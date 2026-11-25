using System.Security.Claims;
using AprendizagemColaborativa.Api.Data;
using Microsoft.EntityFrameworkCore;

namespace AprendizagemColaborativa.Api.Features.Auth;

public static class AlunosEndpoints
{
    public static void MapAlunosEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/professor/alunos").RequireAuthorization();
        group.MapGet("", async (ClaimsPrincipal user, ApplicationDbContext db) =>
        {
            if (!await AcessoEndpoints.ProfessorAsync(db, user)) return Results.Forbid();
            var alunos = await db.Users.AsNoTracking().Where(a => !a.Professor).OrderBy(a => a.Nome)
                .Select(a => new { a.Id, a.Nome, a.Curso, a.Email, a.PodeMinistrar }).ToListAsync();
            return Results.Ok(alunos);
        });
        group.MapGet("/{id}", async (string id, ClaimsPrincipal user, ApplicationDbContext db) =>
        {
            if (!await AcessoEndpoints.ProfessorAsync(db, user)) return Results.Forbid();
            var aluno = await db.Users.AsNoTracking().Where(a => a.Id == id && !a.Professor)
                .Select(a => new { a.Id, a.Nome, a.Curso, a.Email, a.PodeMinistrar }).SingleOrDefaultAsync();
            if (aluno is null) return Results.NotFound();
            var inscricoes = await db.Inscricoes.AsNoTracking().Where(i => i.AlunoId == id && i.Status == "ativa")
                .OrderByDescending(i => i.Atividade.DataHoraUtc)
                .Select(i => new { i.Atividade.Id, i.Atividade.Titulo, i.Atividade.Tema, i.Atividade.DataHoraUtc, i.Atividade.Status }).ToListAsync();
            var ministradas = await db.Atividades.AsNoTracking().Where(a => a.ResponsavelId == id)
                .OrderByDescending(a => a.DataHoraUtc)
                .Select(a => new { a.Id, a.Titulo, a.Tema, a.DataHoraUtc, a.Status }).ToListAsync();
            return Results.Ok(new { aluno, inscricoes, ministradas });
        });
    }
}
