using AprendizagemColaborativa.Api.Data;
using AprendizagemColaborativa.Api.Features.Atividades;
using AprendizagemColaborativa.Api.Models;
using Xunit;

namespace AprendizagemColaborativa.Api.Tests;

public sealed class AssentosServiceTests
{
    [Theory]
    [InlineData("DESKTOP", 6, 24)]
    [InlineData("NOTEBOOK", 5, 20)]
    public void Mapa_retorna_grade_completa_ordenada_e_disponivel(string tipo, int colunas, int capacidade)
    {
        var atividade = CriarAtividade(tipo);
        // A ordem de armazenamento não deve alterar a posição retornada ao front-end.
        atividade.Sala!.Assentos = atividade.Sala.Assentos.Reverse().ToList();
        var mapa = AssentosService.CriarMapa(atividade, []);

        Assert.Equal(capacidade, mapa.CapacidadeTotal);
        Assert.Equal(capacidade, mapa.LugaresDisponiveis);
        Assert.Equal(capacidade, mapa.Assentos.Count);
        Assert.Equal(capacidade, mapa.Assentos.Select(item => item.Id).Distinct().Count());
        var posicoes = from fileira in new[] { "A", "B", "C", "D" }
                       from coluna in Enumerable.Range(1, colunas)
                       select (Fileira: fileira, Coluna: coluna);
        Assert.Equal(posicoes, mapa.Assentos.Select(item => (item.Fileira, item.Coluna)));
        Assert.All(mapa.Assentos, item =>
        {
            Assert.Equal($"{item.Fileira}{item.Coluna}", item.Codigo);
            Assert.Equal("DISPONIVEL", item.Status);
        });
    }

    [Theory]
    [InlineData("DESKTOP", 1)]
    [InlineData("DESKTOP", 24)]
    [InlineData("NOTEBOOK", 3)]
    [InlineData("NOTEBOOK", 20)]
    public void Mapa_conta_apenas_assentos_ocupados_da_sala(string tipo, int quantidade)
    {
        var atividade = CriarAtividade(tipo);
        var ocupados = atividade.Sala!.Assentos.Take(quantidade).Select(item => item.Id).ToArray();
        // IDs repetidos ou de outra sala não podem reduzir a disponibilidade.
        var mapa = AssentosService.CriarMapa(atividade, ocupados.Concat(ocupados).Append(Guid.NewGuid()));

        Assert.Equal(mapa.CapacidadeTotal - quantidade, mapa.LugaresDisponiveis);
        Assert.Equal(quantidade, mapa.Assentos.Count(item => item.Status == "OCUPADO"));
        Assert.All(mapa.Assentos, item =>
            Assert.Equal(ocupados.Contains(item.Id) ? "OCUPADO" : "DISPONIVEL", item.Status));
    }

    [Fact]
    public void Mapa_preserva_identificacao_sala_e_horario_utc()
    {
        var atividade = CriarAtividade("DESKTOP");
        var mapa = AssentosService.CriarMapa(atividade, []);

        Assert.Equal(atividade.Id, mapa.AtividadeId);
        Assert.Equal(atividade.Tema, mapa.Tema);
        Assert.Equal(new DateOnly(2030, 1, 15), mapa.Data);
        Assert.Equal(new DateTimeOffset(2030, 1, 15, 14, 0, 0, TimeSpan.Zero), mapa.Inicio);
        Assert.Equal(mapa.Inicio.AddHours(2), mapa.Fim);
        Assert.Equal(atividade.Sala!.Id, mapa.Sala.Id);
        Assert.Equal(atividade.Sala.Nome, mapa.Sala.Nome);
        Assert.Equal("DESKTOP", mapa.Sala.Tipo);
    }

    private static Atividade CriarAtividade(string tipo)
    {
        var original = SalaSeed.Salas.Single(sala => sala.Tipo == tipo);
        var sala = new Sala
        {
            Id = original.Id, Nome = original.Nome, Tipo = original.Tipo, Capacidade = original.Capacidade,
            Assentos = SalaSeed.Assentos.Where(assento => assento.SalaId == original.Id).ToList()
        };
        return new()
        {
            Id = Guid.NewGuid(), Tema = "Mapa de assentos", SalaId = sala.Id, Sala = sala,
            Inicio = new DateTime(2030, 1, 15, 14, 0, 0), Fim = new DateTime(2030, 1, 15, 16, 0, 0)
        };
    }
}
