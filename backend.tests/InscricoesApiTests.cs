using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using AprendizagemColaborativa.Api.Data;
using AprendizagemColaborativa.Api.Features.Atividades;
using AprendizagemColaborativa.Api.Features.Auth;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace AprendizagemColaborativa.Api.Tests;

[Collection(ApiIntegrationCollection.Name)]
public sealed class InscricoesApiTests(WebApplicationFactory<Program> factory) : IAsyncLifetime
{
    private readonly HttpClient _public = factory.CreateClient();
    private readonly List<string> _alunos = [];
    private readonly List<Guid> _atividades = [];
    private HttpClient _primeiro = null!;
    private HttpClient _segundo = null!;
    private AtividadeResponse _atividade = null!;
    private static int _dia;
    private static readonly Guid AssentoA1 = SalaSeed.Assentos.Single(item =>
        item.SalaId == SalaSeed.SalaDesktopId && item.Codigo == "A1").Id;
    private static readonly Guid AssentoA2 = SalaSeed.Assentos.Single(item =>
        item.SalaId == SalaSeed.SalaDesktopId && item.Codigo == "A2").Id;

    public async Task InitializeAsync()
    {
        _primeiro = await CriarAlunoAsync("professor");
        _segundo = await CriarAlunoAsync();
        _atividade = await CriarAtividadeAsync();
    }

    [Theory]
    [InlineData("DESKTOP", 24, 6)]
    [InlineData("NOTEBOOK", 20, 5)]
    public async Task Mapa_autenticado_retorna_grade_completa_sem_dados_pessoais(string tipo, int capacidade, int colunas)
    {
        var atividade = tipo == "DESKTOP" ? _atividade : await CriarAtividadeAsync(tipo);
        using var response = await _primeiro.GetAsync($"/api/atividades/{atividade.Id}/assentos");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var mapa = (await response.Content.ReadFromJsonAsync<MapaAssentosResponse>())!;
        Assert.Equal(atividade.Id, mapa.AtividadeId);
        Assert.Equal(atividade.Tema, mapa.Tema);
        Assert.Equal(atividade.Data, mapa.Data);
        Assert.Equal(atividade.Inicio, mapa.Inicio);
        Assert.Equal(atividade.Fim, mapa.Fim);
        Assert.Equal(atividade.Sala, mapa.Sala);
        Assert.Equal(capacidade, mapa.CapacidadeTotal);
        Assert.Equal(capacidade, mapa.LugaresDisponiveis);
        Assert.Equal(capacidade, mapa.Assentos.Count);
        Assert.Equal(new[] { "A", "B", "C", "D" }, mapa.Assentos.Select(item => item.Fileira).Distinct());
        Assert.All(mapa.Assentos, item =>
        {
            Assert.InRange(item.Coluna, 1, colunas);
            Assert.Equal($"{item.Fileira}{item.Coluna}", item.Codigo);
            Assert.Equal("DISPONIVEL", item.Status);
        });
        var body = await response.Content.ReadAsStringAsync();
        foreach (var campo in new[] { "aluno", "email", "password", "inscricoes", "organizador", "responsavel" })
        {
            Assert.DoesNotContain(campo, body, StringComparison.OrdinalIgnoreCase);
        }
    }

    [Fact]
    public async Task Inscricao_bem_sucedida_usa_aluno_do_token_e_atualiza_mapa()
    {
        using var response = await _primeiro.PostAsJsonAsync(UrlInscricao(), new
        {
            assentoId = AssentoA1, alunoId = _alunos[1]
        });
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var inscricao = (await response.Content.ReadFromJsonAsync<InscricaoResponse>())!;
        Assert.Equal(_atividade.Id, inscricao.AtividadeId);
        Assert.Equal(AssentoA1, inscricao.Assento.Id);
        Assert.Equal("A1", inscricao.Assento.Codigo);
        await ComBancoAsync(async db =>
        {
            var salva = await db.Inscricoes.AsNoTracking().SingleAsync(item => item.AtividadeId == _atividade.Id);
            Assert.Equal(_alunos[0], salva.AlunoId);
            Assert.Equal(AssentoA1, salva.AssentoId);
            Assert.NotNull(salva.Data);
            Assert.Equal("ativa", salva.Status);
        });
        var mapa = await MapaAsync(_atividade.Id);
        Assert.Equal(23, mapa.LugaresDisponiveis);
        Assert.Single(mapa.Assentos, item => item.Status == "OCUPADO" && item.Id == AssentoA1);
        Assert.True(mapa.JaInscrito);
        Assert.Equal(AssentoA1, mapa.MeuAssentoId);
        var mapaOutro = (await _segundo.GetFromJsonAsync<MapaAssentosResponse>($"/api/atividades/{_atividade.Id}/assentos"))!;
        Assert.False(mapaOutro.JaInscrito);
        Assert.Null(mapaOutro.MeuAssentoId);
        Assert.Equal("OCUPADO", mapaOutro.Assentos.Single(item => item.Id == AssentoA1).Status);
        Assert.Equal(HttpStatusCode.NoContent, (await _primeiro.DeleteAsync($"/api/atividades/{_atividade.Id}/inscricoes/me")).StatusCode);
        var aposCancelar = await MapaAsync(_atividade.Id);
        Assert.False(aposCancelar.JaInscrito);
        Assert.Null(aposCancelar.MeuAssentoId);
        Assert.Equal("DISPONIVEL", aposCancelar.Assentos.Single(item => item.Id == AssentoA1).Status);
    }

    [Fact]
    public async Task Assento_reservado_em_outra_atividade_permanece_disponivel()
    {
        using var primeira = await ReservarAsync(_primeiro, AssentoA1);
        Assert.Equal(HttpStatusCode.Created, primeira.StatusCode);
        var outra = await CriarAtividadeAsync();
        var mapa = await MapaAsync(outra.Id);
        Assert.Equal(24, mapa.LugaresDisponiveis);
        Assert.Equal("DISPONIVEL", mapa.Assentos.Single(item => item.Id == AssentoA1).Status);
        using var segunda = await _primeiro.PostAsJsonAsync($"/api/atividades/{outra.Id}/inscricoes", new { assentoId = AssentoA1 });
        Assert.Equal(HttpStatusCode.Created, segunda.StatusCode);
    }

    [Fact]
    public async Task Mesmo_aluno_nao_pode_se_inscrever_duas_vezes()
    {
        using var primeira = await ReservarAsync(_primeiro, AssentoA1);
        using var duplicada = await ReservarAsync(_primeiro, AssentoA2);
        Assert.Equal(HttpStatusCode.Created, primeira.StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, duplicada.StatusCode);
        await AssertQuantidadeAsync(1);
        Assert.Equal("DISPONIVEL", (await MapaAsync(_atividade.Id)).Assentos.Single(item => item.Id == AssentoA2).Status);
    }

    [Fact]
    public async Task Outro_aluno_nao_pode_reservar_assento_ocupado()
    {
        using var primeira = await ReservarAsync(_primeiro, AssentoA1);
        using var segunda = await ReservarAsync(_segundo, AssentoA1);
        Assert.Equal(HttpStatusCode.Created, primeira.StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, segunda.StatusCode);
        await AssertQuantidadeAsync(1);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Assento_inexistente_ou_de_outra_sala_retorna_400_sem_inscricao(bool outraSala)
    {
        var assento = outraSala ? SalaSeed.Assentos.First(item => item.SalaId == SalaSeed.SalaNotebookId).Id : Guid.NewGuid();
        using var response = await ReservarAsync(_primeiro, assento);
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        await AssertQuantidadeAsync(0);
    }

    [Theory]
    [InlineData("{}")]
    [InlineData("null")]
    [InlineData("{\"assentoId\":\"invalido\"}")]
    [InlineData("{\"assentoId\":\"00000000-0000-0000-0000-000000000000\"}")]
    public async Task Corpo_invalido_retorna_400(string json)
    {
        using var content = new StringContent(json, Encoding.UTF8, "application/json");
        using var response = await _primeiro.PostAsync(UrlInscricao(), content);
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        await AssertQuantidadeAsync(0);
    }

    [Fact]
    public async Task Atividade_inexistente_retorna_404_no_mapa_e_na_reserva()
    {
        var id = Guid.NewGuid();
        using var mapa = await _primeiro.GetAsync($"/api/atividades/{id}/assentos");
        using var reserva = await _primeiro.PostAsJsonAsync($"/api/atividades/{id}/inscricoes", new { assentoId = AssentoA1 });
        Assert.Equal(HttpStatusCode.NotFound, mapa.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, reserva.StatusCode);
    }

    [Fact]
    public async Task Sem_token_retorna_401_nas_inscricoes()
    {
        using var reserva = await ReservarAsync(_public, AssentoA1);
        using var minhas = await _public.GetAsync("/api/inscricoes/minhas");
        using var alias = await _public.GetAsync("/api/inscricoes/me");
        Assert.Equal(HttpStatusCode.Unauthorized, reserva.StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, minhas.StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, alias.StatusCode);
        await AssertQuantidadeAsync(0);
    }

    [Fact]
    public async Task Minhas_inscricoes_filtra_aluno_e_inclui_atividade_sala_horario_assento()
    {
        using var primeira = await ReservarAsync(_primeiro, AssentoA1);
        using var segunda = await ReservarAsync(_segundo, AssentoA2);
        Assert.Equal(HttpStatusCode.Created, primeira.StatusCode);
        Assert.Equal(HttpStatusCode.Created, segunda.StatusCode);
        foreach (var rota in new[] { "/api/inscricoes/minhas", "/api/inscricoes/me" })
        {
            using var response = await _primeiro.GetAsync(rota);
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var minha = Assert.Single((await response.Content.ReadFromJsonAsync<List<MinhaInscricaoResponse>>())!);
            Assert.Equal(_atividade.Id, minha.AtividadeId);
            Assert.Equal(_atividade.Tema, minha.Tema);
            Assert.Equal(_atividade.Sala, minha.Sala);
            Assert.Equal(_atividade.Inicio, minha.Inicio);
            Assert.Equal(_atividade.Fim, minha.Fim);
            Assert.Equal(AssentoA1, minha.Assento!.Id);
        }
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Concorrencia_cria_uma_inscricao_e_retorna_409_para_a_outra(bool mesmoAluno)
    {
        var responses = await Task.WhenAll(
            ReservarAsync(_primeiro, AssentoA1),
            ReservarAsync(mesmoAluno ? _primeiro : _segundo, mesmoAluno ? AssentoA2 : AssentoA1));
        try
        {
            Assert.Single(responses, item => item.StatusCode == HttpStatusCode.Created);
            Assert.Single(responses, item => item.StatusCode == HttpStatusCode.Conflict);
            await AssertQuantidadeAsync(1);
            Assert.Equal(23, (await MapaAsync(_atividade.Id)).LugaresDisponiveis);
        }
        finally
        {
            foreach (var response in responses) response.Dispose();
        }
    }

    [Fact]
    public async Task Concorrencia_com_alunos_e_assentos_distintos_permite_ambas_as_reservas()
    {
        var responses = await Task.WhenAll(ReservarAsync(_primeiro, AssentoA1), ReservarAsync(_segundo, AssentoA2));
        try
        {
            Assert.All(responses, response => Assert.Equal(HttpStatusCode.Created, response.StatusCode));
            await AssertQuantidadeAsync(2);
            Assert.Equal(22, (await MapaAsync(_atividade.Id)).LugaresDisponiveis);
        }
        finally
        {
            foreach (var response in responses) response.Dispose();
        }
    }

    [Fact]
    public async Task Cancelamento_libera_assento_e_reinscricao_respeita_reserva_atual()
    {
        using var primeira = await ReservarAsync(_primeiro, AssentoA1);
        Assert.Equal(HttpStatusCode.Created, primeira.StatusCode);
        using var cancelamento = await _primeiro.DeleteAsync($"{UrlInscricao()}/me");
        Assert.Equal(HttpStatusCode.NoContent, cancelamento.StatusCode);
        Assert.Equal(24, (await MapaAsync(_atividade.Id)).LugaresDisponiveis);
        using var segunda = await ReservarAsync(_segundo, AssentoA1);
        using var ocupada = await ReservarAsync(_primeiro, AssentoA1);
        using var reinscricao = await ReservarAsync(_primeiro, AssentoA2);
        Assert.Equal(HttpStatusCode.Created, segunda.StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, ocupada.StatusCode);
        Assert.Equal(HttpStatusCode.Created, reinscricao.StatusCode);
        await AssertQuantidadeAsync(2);
        Assert.Equal(22, (await MapaAsync(_atividade.Id)).LugaresDisponiveis);
    }

    [Fact]
    public async Task Atividade_cancelada_nao_aceita_inscricao()
    {
        await ComBancoAsync(async db =>
        {
            var atividade = await db.Atividades.FindAsync(_atividade.Id);
            atividade!.Status = "cancelada";
            await db.SaveChangesAsync();
        });
        using var response = await ReservarAsync(_primeiro, AssentoA1);
        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        await AssertQuantidadeAsync(0);
    }

    [Fact]
    public async Task Conflito_do_indice_unico_retorna_409_e_reverte_transacao()
    {
        using var primeira = await ReservarAsync(_primeiro, AssentoA1);
        Assert.Equal(HttpStatusCode.Created, primeira.StatusCode);
        // Simula registro legado que ainda ocupa a chave única no banco, mas não a consulta de ativos.
        await ComBancoAsync(async db =>
        {
            var inscricao = await db.Inscricoes.SingleAsync(item => item.AtividadeId == _atividade.Id);
            inscricao.Status = "cancelada";
            await db.SaveChangesAsync();
        });
        using var conflito = await ReservarAsync(_segundo, AssentoA1);
        Assert.Equal(HttpStatusCode.Conflict, conflito.StatusCode);
        await AssertQuantidadeAsync(1);
        await ComBancoAsync(async db => Assert.False(await db.Inscricoes.AnyAsync(item =>
            item.AtividadeId == _atividade.Id && item.AlunoId == _alunos[1])));
    }

    private string UrlInscricao() => $"/api/atividades/{_atividade.Id}/inscricoes";

    private Task<HttpResponseMessage> ReservarAsync(HttpClient client, Guid assentoId) =>
        client.PostAsJsonAsync(UrlInscricao(), new { assentoId });

    private async Task<MapaAssentosResponse> MapaAsync(Guid atividadeId) =>
        (await _primeiro.GetFromJsonAsync<MapaAssentosResponse>($"/api/atividades/{atividadeId}/assentos"))!;

    private Task AssertQuantidadeAsync(int quantidade) => ComBancoAsync(async db =>
        Assert.Equal(quantidade, await db.Inscricoes.CountAsync(item => item.AtividadeId == _atividade.Id)));

    private async Task ComBancoAsync(Func<ApplicationDbContext, Task> action)
    {
        await using var scope = factory.Services.CreateAsyncScope();
        await action(scope.ServiceProvider.GetRequiredService<ApplicationDbContext>());
    }

    private async Task<HttpClient> CriarAlunoAsync(string accountType = "aluno")
    {
        var email = $"inscricoes-{Guid.NewGuid():N}@example.test";
        const string password = "Fatec!Teste2026";
        using var register = await _public.PostAsJsonAsync("/api/auth/register", new
        {
            name = "Aluno de teste", email, course = "DSM", password, accountType
        });
        Assert.Equal(HttpStatusCode.Created, register.StatusCode);
        var aluno = (await register.Content.ReadFromJsonAsync<StudentResponse>())!;
        _alunos.Add(aluno.Id);
        using var login = await _public.PostAsJsonAsync("/api/auth/login", new { email, password });
        Assert.Equal(HttpStatusCode.OK, login.StatusCode);
        var token = (await login.Content.ReadFromJsonAsync<LoginResponse>())!.AccessToken;
        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return client;
    }

    private async Task<AtividadeResponse> CriarAtividadeAsync(string tipo = "DESKTOP")
    {
        var inicio = new DateTimeOffset(2032, 1, 1, 14, 0, 0, TimeSpan.Zero).AddDays(Interlocked.Increment(ref _dia));
        await using var scope = factory.Services.CreateAsyncScope();
        var service = scope.ServiceProvider.GetRequiredService<AtividadeService>();
        var request = new CriarAtividadeRequest("Reserva de assento", "Teste de integração isolado", "AULA", tipo, inicio.ToString("O"), inicio.AddHours(2).ToString("O"));
        var result = await service.CriarAsync(request, _alunos[0], CancellationToken.None);
        var atividade = Assert.IsType<AtividadeResponse>(result.Atividade);
        _atividades.Add(atividade.Id);
        return atividade;
    }

    public async Task DisposeAsync()
    {
        await ComBancoAsync(async db =>
        {
            db.Inscricoes.RemoveRange(await db.Inscricoes.Where(item => _atividades.Contains(item.AtividadeId)).ToListAsync());
            await db.SaveChangesAsync();
            db.Atividades.RemoveRange(await db.Atividades.Where(item => _atividades.Contains(item.Id)).ToListAsync());
            await db.SaveChangesAsync();
            db.Users.RemoveRange(await db.Users.Where(item => _alunos.Contains(item.Id)).ToListAsync());
            await db.SaveChangesAsync();
        });
        _primeiro?.Dispose();
        _segundo?.Dispose();
        _public.Dispose();
    }
}
