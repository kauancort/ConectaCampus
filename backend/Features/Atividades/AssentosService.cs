using AprendizagemColaborativa.Api.Data;
using AprendizagemColaborativa.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace AprendizagemColaborativa.Api.Features.Atividades;

public sealed class AssentosService(ApplicationDbContext db)
{
    public Task<Atividade?> ObterAtividadeAsync(Guid atividadeId, CancellationToken cancellationToken) =>
        db.Atividades.AsNoTracking()
            .Include(atividade => atividade.Sala).ThenInclude(sala => sala!.Assentos)
            .SingleOrDefaultAsync(atividade => atividade.Id == atividadeId, cancellationToken);

    public async Task<MapaAssentosResponse> ConsultarAsync(Atividade atividade, string alunoId, CancellationToken cancellationToken)
    {
        var ocupados = await db.Inscricoes.AsNoTracking()
            .Where(inscricao => inscricao.AtividadeId == atividade.Id &&
                inscricao.Status == "ativa" && inscricao.AssentoId != null)
            .Select(inscricao => inscricao.AssentoId!.Value)
            .ToListAsync(cancellationToken);
        var minhaInscricao = await db.Inscricoes.AsNoTracking()
            .Where(i => i.AtividadeId == atividade.Id && i.AlunoId == alunoId && i.Status == "ativa")
            .Select(i => new { i.AssentoId }).SingleOrDefaultAsync(cancellationToken);
        return CriarMapa(atividade, ocupados) with { JaInscrito = minhaInscricao is not null, MeuAssentoId = minhaInscricao?.AssentoId };
    }

    public static MapaAssentosResponse CriarMapa(Atividade atividade, IEnumerable<Guid> assentosOcupados)
    {
        var sala = atividade.Sala ?? throw new InvalidOperationException("A atividade não possui sala.");
        var ocupados = assentosOcupados.ToHashSet();
        var assentos = sala.Assentos.OrderBy(assento => assento.Fileira).ThenBy(assento => assento.Coluna)
            .Select(assento => new AssentoResponse(assento.Id, assento.Codigo, assento.Fileira, assento.Coluna,
                ocupados.Contains(assento.Id) ? "OCUPADO" : "DISPONIVEL"))
            .ToList();
        var inicio = ToUtcOffset(atividade.Inicio ?? atividade.DataHoraUtc);
        return new(atividade.Id, string.IsNullOrWhiteSpace(atividade.Titulo) ? atividade.Tema : atividade.Titulo, DateOnly.FromDateTime(inicio.DateTime), inicio,
            atividade.Fim is { } fim ? ToUtcOffset(fim) : null,
            new(sala.Id, sala.Nome, sala.Tipo, sala.Capacidade), sala.Capacidade,
            assentos.Count(assento => assento.Status == "DISPONIVEL"), assentos);
    }

    private static DateTimeOffset ToUtcOffset(DateTime value) =>
        new(DateTime.SpecifyKind(value, DateTimeKind.Utc));
}
