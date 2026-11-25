using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using AprendizagemColaborativa.Api.Data;
using AprendizagemColaborativa.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace AprendizagemColaborativa.Api.Features.Auth;

public static class AcessoEndpoints
{
    private static string? UserId(ClaimsPrincipal user) => user.FindFirstValue(JwtRegisteredClaimNames.Sub);
    public static Task<bool> ProfessorAsync(ApplicationDbContext db, ClaimsPrincipal user) => db.Users.AnyAsync(x => x.Id == UserId(user) && x.Professor);

    public static void MapAcessoEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/acesso-professor").RequireAuthorization();
        group.MapGet("/minha", async (ClaimsPrincipal user, ApplicationDbContext db) => Results.Ok(await db.SolicitacoesProfessor.AsNoTracking().Where(x => x.AlunoId == UserId(user)).OrderByDescending(x => x.CriadaEmUtc).FirstOrDefaultAsync()));
        group.MapPost("", async (SolicitacaoRequest? request, ClaimsPrincipal user, ApplicationDbContext db) =>
        {
            var id = UserId(user);
            if (id is null) return Results.Unauthorized();
            if (string.IsNullOrWhiteSpace(request?.Justificativa) || request.Justificativa.Trim().Length > 2000) return Results.BadRequest(new { message = "Informe uma justificativa de até 2000 caracteres." });
            await using var transaction = await db.Database.BeginTransactionAsync();
            var aluno = (await db.Users.FromSqlInterpolated($"""SELECT * FROM conectacampus."AspNetUsers" WHERE "Id" = {id} FOR UPDATE""").ToListAsync()).SingleOrDefault();
            if (aluno is null) return Results.Unauthorized();
            if (aluno.Professor || aluno.PodeMinistrar || await db.SolicitacoesProfessor.AnyAsync(x => x.AlunoId == id && x.Status == "pendente")) return Results.Conflict(new { message = "Você já possui acesso ou uma solicitação pendente." });
            var solicitacao = new SolicitacaoProfessor { Id = Guid.NewGuid(), AlunoId = id, Justificativa = request.Justificativa.Trim() };
            db.SolicitacoesProfessor.Add(solicitacao);
            var professores = await db.Users.Where(x => x.Professor).Select(x => x.Id).ToListAsync();
            db.Notificacoes.AddRange(professores.Select(x => new Notificacao { UsuarioId = x, Mensagem = $"{aluno.Nome} solicitou acesso para ministrar atividades." }));
            await db.SaveChangesAsync(); await transaction.CommitAsync();
            return Results.Json(solicitacao, statusCode: 201);
        });
        group.MapGet("", async (ClaimsPrincipal user, ApplicationDbContext db) =>
        {
            if (!await ProfessorAsync(db, user)) return Results.Forbid();
            var items = await (from s in db.SolicitacoesProfessor.AsNoTracking() join a in db.Users on s.AlunoId equals a.Id where s.Status == "pendente" orderby s.CriadaEmUtc select new { s.Id, s.Justificativa, s.Status, s.CriadaEmUtc, Nome = a.Nome, a.Email, Curso = a.Curso }).ToListAsync();
            return Results.Ok(items);
        });
        group.MapPost("/{id:guid}/avaliar", async (Guid id, AvaliacaoRequest? request, ClaimsPrincipal user, ApplicationDbContext db) =>
        {
            if (!await ProfessorAsync(db, user)) return Results.Forbid();
            if (request?.Decisao is not ("aprovada" or "recusada")) return Results.BadRequest(new { message = "Decisão inválida." });
            await using var transaction = await db.Database.BeginTransactionAsync();
            var s = (await db.SolicitacoesProfessor.FromSqlInterpolated($"""SELECT * FROM conectacampus."SolicitacoesProfessor" WHERE "Id" = {id} FOR UPDATE""").ToListAsync()).SingleOrDefault();
            if (s is null) return Results.NotFound();
            if (s.Status != "pendente") return Results.Conflict(new { message = "Esta solicitação já foi avaliada." });
            var aluno = await db.Users.FindAsync(s.AlunoId);
            if (aluno is null) return Results.NotFound();
            s.Status = request.Decisao; s.AvaliadorId = UserId(user); s.AvaliadaEmUtc = DateTime.UtcNow;
            if (s.Status == "aprovada") aluno.PodeMinistrar = true;
            db.Notificacoes.Add(new Notificacao { UsuarioId = aluno.Id, Mensagem = $"Sua solicitação para ministrar atividades foi {s.Status}." });
            await db.SaveChangesAsync(); await transaction.CommitAsync();
            return Results.Ok(new { s.Id, s.Status });
        });
        var notifications = app.MapGroup("/api/notificacoes").RequireAuthorization();
        notifications.MapGet("", async (ClaimsPrincipal user, ApplicationDbContext db) => Results.Ok(await db.Notificacoes.AsNoTracking().Where(x => x.UsuarioId == UserId(user)).OrderByDescending(x => x.CriadaEmUtc).ToListAsync()));
        notifications.MapPost("/{id:guid}/lida", async (Guid id, ClaimsPrincipal user, ApplicationDbContext db) =>
        {
            var item = await db.Notificacoes.SingleOrDefaultAsync(x => x.Id == id && x.UsuarioId == UserId(user));
            if (item is null) return Results.NotFound();
            item.Lida = true; await db.SaveChangesAsync(); return Results.NoContent();
        });
    }
}
public sealed record SolicitacaoRequest(string? Justificativa);
public sealed record AvaliacaoRequest(string? Decisao);
