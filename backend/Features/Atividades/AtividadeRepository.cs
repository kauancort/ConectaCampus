using AprendizagemColaborativa.Api.Data;
using AprendizagemColaborativa.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace AprendizagemColaborativa.Api.Features.Atividades;

public sealed class AtividadeRepository(ApplicationDbContext db)
{
    private const string SalaDisponivel = "disponivel";

    public Task<List<TipoSalaResponse>> ListarTiposDeSalaAsync(CancellationToken cancellationToken) =>
        db.Salas.AsNoTracking()
            .Where(sala => sala.Status == SalaDisponivel)
            .GroupBy(sala => sala.Tipo)
            .OrderBy(grupo => grupo.Key)
            .Select(grupo => new TipoSalaResponse(grupo.Key, grupo.Max(sala => sala.Capacidade)))
            .ToListAsync(cancellationToken);

    public Task<Aluno?> ObterResponsavelAsync(string responsavelId, CancellationToken cancellationToken) =>
        db.Users.SingleOrDefaultAsync(aluno => aluno.Id == responsavelId, cancellationToken);

    public Task<List<Sala>> ObterSalasDisponiveisComBloqueioAsync(
        string tipoSala,
        CancellationToken cancellationToken) =>
        db.Salas
            .FromSqlInterpolated($"SELECT * FROM `Salas` WHERE `Tipo` = {tipoSala} AND `Status` = {SalaDisponivel} ORDER BY `Id` FOR UPDATE")
            .ToListAsync(cancellationToken);

    public Task<bool> ExisteConflitoDeHorarioAsync(
        Guid salaId,
        DateTime inicio,
        DateTime fim,
        CancellationToken cancellationToken) =>
        db.Atividades.AnyAsync(atividade =>
            atividade.SalaId == salaId &&
            atividade.Inicio < fim &&
            inicio < atividade.Fim,
            cancellationToken);

    public void Adicionar(Atividade atividade) => db.Atividades.Add(atividade);

    public Task SalvarAsync(CancellationToken cancellationToken) => db.SaveChangesAsync(cancellationToken);

    public Task<Atividade?> ObterPorIdAsync(Guid id, CancellationToken cancellationToken) =>
        db.Atividades.AsNoTracking()
            .Include(atividade => atividade.Sala)
            .SingleOrDefaultAsync(atividade => atividade.Id == id, cancellationToken);

    public Task<List<Atividade>> ListarAsync(string? tema, CancellationToken cancellationToken)
    {
        var query = db.Atividades.AsNoTracking()
            .Include(atividade => atividade.Sala)
            .Where(atividade => atividade.SalaId != null && atividade.Inicio != null && atividade.Fim != null);

        if (!string.IsNullOrWhiteSpace(tema))
        {
            var filtro = tema.Trim();
            query = query.Where(atividade => atividade.Tema.Contains(filtro));
        }

        return query.OrderBy(atividade => atividade.Inicio).ToListAsync(cancellationToken);
    }

    public Task<List<Atividade>> ListarDoResponsavelAsync(string responsavelId, CancellationToken cancellationToken) =>
        db.Atividades.AsNoTracking()
            .Include(atividade => atividade.Sala)
            .Where(atividade =>
                atividade.ResponsavelId == responsavelId &&
                atividade.SalaId != null &&
                atividade.Inicio != null &&
                atividade.Fim != null)
            .OrderBy(atividade => atividade.Inicio)
            .ToListAsync(cancellationToken);
}
