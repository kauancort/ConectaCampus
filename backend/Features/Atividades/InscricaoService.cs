using AprendizagemColaborativa.Api.Data;
using AprendizagemColaborativa.Api.Models;
using Microsoft.EntityFrameworkCore;
using MySql.Data.MySqlClient;

namespace AprendizagemColaborativa.Api.Features.Atividades;

public sealed class InscricaoService(ApplicationDbContext db)
{
    public async Task<ReservarInscricaoResultado> ReservarAsync(
        Guid atividadeId, Guid assentoId, string alunoId, CancellationToken cancellationToken)
    {
        if (!await db.Users.AnyAsync(aluno => aluno.Id == alunoId, cancellationToken))
        {
            return new(SituacaoInscricao.AlunoInexistente);
        }

        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
        // Todas as reservas/cancelamentos da atividade usam o mesmo bloqueio de linha.
        // Os índices únicos (atividade, aluno) e (atividade, assento) são a garantia final do banco.
        var atividade = (await db.Atividades
            .FromSqlInterpolated($"SELECT * FROM `Atividades` WHERE `Id` = {atividadeId} FOR UPDATE")
            .AsTracking().ToListAsync(cancellationToken)).SingleOrDefault();
        if (atividade is null)
        {
            return new(SituacaoInscricao.AtividadeInexistente);
        }
        if (atividade.Status != "publicada")
        {
            return new(SituacaoInscricao.AtividadeCancelada);
        }

        var assento = await db.Assentos.AsNoTracking()
            .SingleOrDefaultAsync(item => item.Id == assentoId && item.SalaId == atividade.SalaId, cancellationToken);
        if (assento is null)
        {
            return new(SituacaoInscricao.AssentoInvalido);
        }

        var inscricao = await db.Inscricoes.SingleOrDefaultAsync(item =>
            item.AtividadeId == atividadeId && item.AlunoId == alunoId, cancellationToken);
        if (inscricao?.Status == "ativa")
        {
            return new(SituacaoInscricao.AlunoJaInscrito);
        }
        if (await db.Inscricoes.AnyAsync(item => item.AtividadeId == atividadeId &&
            item.AssentoId == assentoId && item.Status == "ativa", cancellationToken))
        {
            return new(SituacaoInscricao.AssentoOcupado);
        }
        if (await db.Inscricoes.CountAsync(item => item.AtividadeId == atividadeId &&
            item.Status == "ativa", cancellationToken) >= atividade.NumeroDeVagas)
        {
            return new(SituacaoInscricao.SemVagas);
        }

        var now = DateTime.UtcNow;
        if (inscricao is null)
        {
            inscricao = new Inscricao
            {
                Id = Guid.NewGuid(), AtividadeId = atividadeId, AlunoId = alunoId, CriadaEmUtc = now
            };
            db.Inscricoes.Add(inscricao);
        }
        inscricao.Status = "ativa";
        inscricao.AssentoId = assentoId;
        inscricao.Data = now;
        inscricao.AtualizadaEmUtc = now;

        try
        {
            await db.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
        }
        catch (DbUpdateException exception) when (IsDuplicateKey(exception))
        {
            await transaction.RollbackAsync(cancellationToken);
            return new(SituacaoInscricao.ConflitoConcorrencia);
        }

        return new(SituacaoInscricao.Criada, new(inscricao.Id, inscricao.AtividadeId, inscricao.Status,
            ToUtcOffset(inscricao.CriadaEmUtc), ToUtcOffset(inscricao.AtualizadaEmUtc),
            new(assento.Id, assento.Codigo, assento.Fileira, assento.Coluna)));
    }

    public async Task<List<MinhaInscricaoResponse>> ListarDoAlunoAsync(string alunoId, CancellationToken cancellationToken)
    {
        var inscricoes = await db.Inscricoes.AsNoTracking()
            .Include(item => item.Atividade).ThenInclude(item => item.Sala)
            .Include(item => item.Assento)
            .Where(item => item.AlunoId == alunoId)
            .OrderByDescending(item => item.AtualizadaEmUtc)
            .ToListAsync(cancellationToken);

        return inscricoes.Select(item => new MinhaInscricaoResponse(
            item.Id, item.Status, ToUtcOffset(item.CriadaEmUtc), ToUtcOffset(item.AtualizadaEmUtc),
            item.AtividadeId, item.Atividade.Tipo, item.Atividade.Tema, ToUtcOffset(item.Atividade.DataHoraUtc),
            item.Atividade.Local, item.Atividade.Status,
            item.Atividade.Sala is { } sala ? new(sala.Id, sala.Nome, sala.Tipo, sala.Capacidade) : null,
            item.Assento is { } assento ? new(assento.Id, assento.Codigo, assento.Fileira, assento.Coluna) : null,
            ToUtcOffset(item.Atividade.Inicio ?? item.Atividade.DataHoraUtc),
            item.Atividade.Fim is { } fim ? ToUtcOffset(fim) : null)).ToList();
    }

    private static DateTimeOffset ToUtcOffset(DateTime value) =>
        new(DateTime.SpecifyKind(value, DateTimeKind.Utc));

    private static bool IsDuplicateKey(DbUpdateException exception)
    {
        for (Exception? current = exception; current is not null; current = current.InnerException)
        {
            if (current is MySqlException { Number: 1062 })
            {
                return true;
            }
        }
        return false;
    }
}

public enum SituacaoInscricao
{
    Criada, AlunoInexistente, AtividadeInexistente, AssentoInvalido, AtividadeCancelada,
    AlunoJaInscrito, AssentoOcupado, SemVagas, ConflitoConcorrencia
}

public sealed record ReservarInscricaoResultado(SituacaoInscricao Situacao, InscricaoResponse? Inscricao = null);
