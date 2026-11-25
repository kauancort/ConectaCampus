using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using AprendizagemColaborativa.Api.Data;
using AprendizagemColaborativa.Api.Features.Atividades;
using AprendizagemColaborativa.Api.Features.Auth;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace AprendizagemColaborativa.Api.Tests;

<<<<<<< HEAD
// Reservas são exercitadas no serviço que publica após a aprovação; a política
// HTTP da nova fila é coberta em PropostasApiTests.
public sealed class AtividadesApiTests(WebApplicationFactory<Program> factory) : IClassFixture<WebApplicationFactory<Program>>, IAsyncLifetime
=======
[Collection(ApiIntegrationCollection.Name)]
public sealed class AtividadesApiTests(WebApplicationFactory<Program> factory)
>>>>>>> 1524e16cfcfef7673dca4642fc242c4617009a43
{
    private readonly HttpClient _client = factory.CreateClient();
    private string _userId = null!;

    public async Task InitializeAsync()
    {
        var email = $"activity-{Guid.NewGuid():N}@example.test";
        using var response = await _client.PostAsJsonAsync("/api/auth/register", new { name = "Aluno", email, course = "DSM", password = "Teste@123" });
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        _userId = (await response.Content.ReadFromJsonAsync<StudentResponse>())!.Id;
        using var login = await _client.PostAsJsonAsync("/api/auth/login", new { email, password = "Teste@123" });
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", (await login.Content.ReadFromJsonAsync<LoginResponse>())!.AccessToken);
    }
    private static CriarAtividadeRequest Request(string type = "DESKTOP", string theme = "Teste de horário", DateTimeOffset? date = null)
    {
        var start = date ?? DateTimeOffset.UtcNow.AddDays(120).AddMinutes(Random.Shared.Next(1, 10000));
        return new(theme, "Atividade para teste.", "AULA", type, start.ToString("O"), start.AddHours(2).ToString("O"));
    }
    private async Task<CriarAtividadeResultado> Create(CriarAtividadeRequest request)
    {
        await using var scope = factory.Services.CreateAsyncScope();
        var result = await scope.ServiceProvider.GetRequiredService<AtividadeService>().CriarAsync(request, _userId, CancellationToken.None);
        return result;
    }
    [Fact] public async Task Rotas_exigem_autenticacao()
    {
        using var anon = factory.CreateClient();
        Assert.Equal(HttpStatusCode.Unauthorized, (await anon.PostAsJsonAsync("/api/propostas", new { })).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await anon.GetAsync("/api/propostas/minhas")).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await anon.GetAsync("/api/atividades")).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await anon.GetAsync($"/api/atividades/{Guid.NewGuid()}")).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await anon.GetAsync($"/api/atividades/{Guid.NewGuid()}/assentos")).StatusCode);
    }
    [Fact] public async Task Tipos_de_sala_sao_publicos()
    {
        using var anon = factory.CreateClient();
        using var json = JsonDocument.Parse(await anon.GetStringAsync("/api/salas/tipos"));
        Assert.Contains(json.RootElement.EnumerateArray(), x => x.GetProperty("tipo").GetString() == "DESKTOP" && x.GetProperty("capacidade").GetInt32() == 24);
        Assert.Contains(json.RootElement.EnumerateArray(), x => x.GetProperty("tipo").GetString() == "NOTEBOOK" && x.GetProperty("capacidade").GetInt32() == 20);
    }
    [Fact] public async Task Publicacao_reserva_sala_e_aparece_no_catalogo()
    {
        var activity = (await Create(Request())).Atividade!;
        Assert.Equal("DESKTOP", activity.Sala.Tipo); Assert.Equal(24, activity.Sala.Capacidade);
        using var json = JsonDocument.Parse(await _client.GetStringAsync("/api/atividades?tema=Teste"));
        Assert.Contains(json.RootElement.EnumerateArray(), x => x.GetProperty("id").GetGuid() == activity.Id);
    }
    [Fact] public void Valida_dados_e_intervalos()
    {
        using var scope = factory.Services.CreateScope();
        var service = scope.ServiceProvider.GetRequiredService<AtividadeService>();
        var request = new CriarAtividadeRequest("Tema", "Descrição", "CURSO", "AUDITORIO", "2032-01-15T14:00:00", "2032-01-15T16:00:00");
        var errors = service.Validar(request);
        Assert.Contains("modalidade", errors.Keys); Assert.Contains("tipoSala", errors.Keys); Assert.Contains("inicio", errors.Keys); Assert.Contains("fim", errors.Keys);
        var start = DateTimeOffset.UtcNow.AddDays(2).ToString("O");
        Assert.Contains("fim", service.Validar(Request() with { Inicio = start, Fim = start }).Keys);
    }
    [Fact] public async Task Horarios_iguais_conflitam_e_encostados_sao_permitidos()
    {
        var start = DateTimeOffset.UtcNow.AddDays(500).AddMinutes(Random.Shared.Next(10000));
        var first = Request("NOTEBOOK", date: start);
        Assert.NotNull((await Create(first)).Atividade);
        Assert.True((await Create(first)).SalaIndisponivel);
        Assert.NotNull((await Create(Request("NOTEBOOK", date: start.AddHours(2)))).Atividade);
    }
    [Fact] public async Task Reservas_simultaneas_permitidas_apenas_uma_para_mesma_sala()
    {
        var request = Request();
        var results = await Task.WhenAll(Create(request), Create(request));
        Assert.Single(results, x => x.Atividade is not null); Assert.Single(results, x => x.SalaIndisponivel);
    }
    [Theory]
    [InlineData("teste de horário")]
    [InlineData("%_\\")]
    public async Task Busca_ignora_caixa_e_trata_curingas_como_texto(string filter)
    {
        var activity = (await Create(Request(theme: "Teste de horário %_\\"))).Atividade!;
        using var json = JsonDocument.Parse(await _client.GetStringAsync($"/api/atividades?tema={Uri.EscapeDataString(filter)}"));
        Assert.Contains(json.RootElement.EnumerateArray(), x => x.GetProperty("id").GetGuid() == activity.Id);
        Assert.All(json.RootElement.EnumerateArray(), x => Assert.Contains(filter, x.GetProperty("tema").GetString()!, StringComparison.OrdinalIgnoreCase));
    }
    [Fact] public async Task Cancelamento_remove_do_catalogo_e_libera_sala()
    {
        var request = Request(theme: "Atividade cancelada");
        var activity = (await Create(request)).Atividade!;
        await using (var scope = factory.Services.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            (await db.Atividades.FindAsync(activity.Id))!.Status = "cancelada"; await db.SaveChangesAsync();
        }
        using var json = JsonDocument.Parse(await _client.GetStringAsync("/api/atividades?tema=Atividade%20cancelada"));
        Assert.DoesNotContain(json.RootElement.EnumerateArray(), x => x.GetProperty("id").GetGuid() == activity.Id);
        Assert.NotNull((await Create(request)).Atividade);
    }
    [Fact] public async Task Detalhe_valida_identificador()
    {
        Assert.Equal(HttpStatusCode.BadRequest, (await _client.GetAsync("/api/atividades/invalido")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await _client.GetAsync($"/api/atividades/{Guid.NewGuid()}")).StatusCode);
    }
    public async Task DisposeAsync()
    {
<<<<<<< HEAD
        await using var scope = factory.Services.CreateAsyncScope();
=======
        var client = factory.CreateClient();
        var email = $"activity-{Guid.NewGuid():N}@example.test";
        var registration = new
        {
            name = "Leonardo Responsável",
            email,
            course = "Desenvolvimento de Software Multiplataforma",
            accountType = "professor",
            password = "Fatec!Teste2026"
        };

        using var register = await client.PostAsJsonAsync("/api/auth/register", registration);
        Assert.Equal(HttpStatusCode.Created, register.StatusCode);
        using var login = await client.PostAsJsonAsync("/api/auth/login", new { email, password = registration.password });
        Assert.Equal(HttpStatusCode.OK, login.StatusCode);
        using var json = JsonDocument.Parse(await login.Content.ReadAsStringAsync());
        var token = json.RootElement.GetProperty("accessToken").GetString();
        Assert.False(string.IsNullOrWhiteSpace(token));
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return (client, email);
    }

    private async Task DeleteStudentAsync(string email)
    {
        using var scope = factory.Services.CreateScope();
>>>>>>> 1524e16cfcfef7673dca4642fc242c4617009a43
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        db.Atividades.RemoveRange(await db.Atividades.Where(x => x.OrganizadorId == _userId).ToListAsync()); await db.SaveChangesAsync();
        var user = await db.Users.FindAsync(_userId); if (user is not null) { db.Users.Remove(user); await db.SaveChangesAsync(); }
        _client.Dispose();
    }
}
