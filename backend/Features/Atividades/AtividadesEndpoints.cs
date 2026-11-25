using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using AprendizagemColaborativa.Api.Data;
using AprendizagemColaborativa.Api.Features.Auth;
using AprendizagemColaborativa.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace AprendizagemColaborativa.Api.Features.Atividades;

public static class AtividadesEndpoints
{
    public static void MapAtividadesEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet("/api/salas/tipos", async (
            AtividadeRepository repository,
            CancellationToken cancellationToken) =>
        {
            var tipos = await repository.ListarTiposDeSalaAsync(cancellationToken);
            return Results.Ok(tipos);
        }).WithTags("Salas");

        var group = app.MapGroup("/api/atividades").WithTags("Atividades");

        group.MapPost("", () => Results.Forbid()).RequireAuthorization();

        group.MapPut("/{id:guid}/conteudo", async (Guid id, EditarConteudoAula? input, ClaimsPrincipal principal, ApplicationDbContext db) =>
        {
            var userId = principal.FindFirstValue(JwtRegisteredClaimNames.Sub);
            if (string.IsNullOrWhiteSpace(userId)) return Results.Unauthorized();
            if (string.IsNullOrWhiteSpace(input?.Titulo) || input.Titulo.Trim().Length > 200 || string.IsNullOrWhiteSpace(input.Tema) || input.Tema.Trim().Length > 200 || string.IsNullOrWhiteSpace(input.Descricao) || input.Descricao.Trim().Length > 5000)
                return Results.BadRequest(new { message = "Informe título e tema de até 200 caracteres e descrição de até 5000 caracteres." });
            await using var transaction = await db.Database.BeginTransactionAsync();
            var activity = (await db.Atividades.FromSqlInterpolated($"""SELECT * FROM conectacampus."Atividades" WHERE "Id" = {id} FOR UPDATE""").ToListAsync()).SingleOrDefault();
            if (activity is null) return Results.NotFound();
            var proposal = await db.PropostasAula.SingleOrDefaultAsync(p => p.AtividadeId == id && p.AlunoId == userId && p.Status == "aprovada");
            if (proposal is null || activity.ResponsavelId != userId) return Results.Forbid();
            if (activity.Status != "publicada" || activity.Inicio <= DateTime.UtcNow) return Results.Conflict(new { message = "Apenas aulas publicadas que ainda não começaram podem ser editadas." });
            var title = input.Titulo.Trim(); var theme = input.Tema.Trim(); var description = input.Descricao.Trim();
            if (activity.Titulo != title || activity.Tema != theme || activity.Descricao != description)
            {
                activity.Titulo = proposal.Titulo = title;
                activity.Tema = proposal.Tema = theme;
                activity.Descricao = proposal.Descricao = description;
                var recipients = await db.Inscricoes.Where(i => i.AtividadeId == id && i.Status == "ativa").Select(i => i.AlunoId).ToListAsync();
                if (proposal.ProfessorId is not null) recipients.Add(proposal.ProfessorId);
                db.Notificacoes.AddRange(recipients.Where(recipient => recipient != userId).Distinct().Select(recipient => new Notificacao { UsuarioId = recipient, Mensagem = $"As informações da aula ‘{title}’ foram atualizadas." }));
                await db.SaveChangesAsync();
            }
<<<<<<< HEAD
            await transaction.CommitAsync();
            return Results.Ok(new { titulo = title, tema = theme, descricao = description });
        }).RequireAuthorization();
=======

            var responsavelId = principal.FindFirstValue(JwtRegisteredClaimNames.Sub);
            if (string.IsNullOrWhiteSpace(responsavelId))
            {
                return Results.Unauthorized();
            }

            var result = await service.CriarAsync(request!, responsavelId, cancellationToken);
            if (result.ResponsavelInvalido)
            {
                return Results.Unauthorized();
            }

            if (result.SalaIndisponivel)
            {
                return Results.Conflict(new { message = "Não há sala disponível no período solicitado." });
            }

            return Results.Created($"/api/atividades/{result.Atividade!.Id}", result.Atividade);
        }).RequireAuthorization("Professor");
>>>>>>> 1524e16cfcfef7673dca4642fc242c4617009a43

        group.MapGet("", async (
            string? tema,
            ApplicationDbContext db,
            AtividadeRepository repository,
            AtividadeService service,
            CancellationToken cancellationToken) =>
        {
            var atividades = await repository.ListarAsync(tema, cancellationToken);
            var counts = await db.Inscricoes.Where(x => x.Status == "ativa").GroupBy(x => x.AtividadeId).Select(g => new { Id = g.Key, Total = g.Count() }).ToDictionaryAsync(x => x.Id, x => x.Total, cancellationToken);
            return Results.Ok(atividades.Select(x => service.ToResponse(x) with { LugaresDisponiveis = Math.Max(0, x.NumeroDeVagas - counts.GetValueOrDefault(x.Id)) }));
        }).RequireAuthorization();

        group.MapGet("/minhas", async (
            ClaimsPrincipal principal,
            ApplicationDbContext db,
            AtividadeRepository repository,
            AtividadeService service,
            CancellationToken cancellationToken) =>
        {
            if (!await AcessoEndpoints.ProfessorAsync(db, principal)) return Results.Forbid();
            var responsavelId = principal.FindFirstValue(JwtRegisteredClaimNames.Sub);
            if (string.IsNullOrWhiteSpace(responsavelId))
            {
                return Results.Unauthorized();
            }

            var atividades = await repository.ListarDoResponsavelAsync(responsavelId, cancellationToken);
            return Results.Ok(atividades.Select(service.ToResponse));
        }).RequireAuthorization("Professor");

        group.MapPost("/{id:guid}/cancelar", async (Guid id, ClaimsPrincipal principal, ApplicationDbContext db) =>
        {
            await using var transaction = await db.Database.BeginTransactionAsync();
            var atividade = (await db.Atividades.FromSqlInterpolated($"""SELECT * FROM conectacampus."Atividades" WHERE "Id" = {id} FOR UPDATE""").ToListAsync()).SingleOrDefault();
            if (atividade is null) return Results.NotFound();
            var userId = principal.FindFirstValue(JwtRegisteredClaimNames.Sub);
            var approver = await AcessoEndpoints.ProfessorAsync(db, principal) && await db.PropostasAula.AnyAsync(p => p.AtividadeId == id && p.ProfessorId == userId && p.Status == "aprovada");
            if (atividade.OrganizadorId != userId && !approver) return Results.Forbid();
            if (atividade.Status == "cancelada") return Results.Conflict(new { message = "Atividade já cancelada." });
            atividade.Status = "cancelada";
            var inscritos = await db.Inscricoes.Where(x => x.AtividadeId == id && x.Status == "ativa").Select(x => x.AlunoId).ToListAsync();
            var title = string.IsNullOrWhiteSpace(atividade.Titulo) ? atividade.Tema : atividade.Titulo;
            db.Notificacoes.AddRange(inscritos.Select(x => new Notificacao { UsuarioId = x, Mensagem = $"A atividade ‘{title}’ foi cancelada." }));
            if (!inscritos.Contains(atividade.OrganizadorId)) db.Notificacoes.Add(new Notificacao { UsuarioId = atividade.OrganizadorId, Mensagem = $"A atividade ‘{title}’ foi cancelada." });
            await db.SaveChangesAsync(); await transaction.CommitAsync();
            return Results.NoContent();
        }).RequireAuthorization();

        group.MapGet("/{id}", async (
            string id,
            ApplicationDbContext db,
            AtividadeRepository repository,
            AtividadeService service,
            CancellationToken cancellationToken) =>
        {
            if (!Guid.TryParse(id, out var atividadeId))
            {
                return Results.BadRequest(new { message = "O identificador da atividade deve ser um UUID válido." });
            }

            var atividade = await repository.ObterPorIdAsync(atividadeId, cancellationToken);
            return atividade is null
                ? Results.NotFound()
                : Results.Ok(service.ToResponse(atividade) with { LugaresDisponiveis = Math.Max(0, atividade.NumeroDeVagas - await db.Inscricoes.CountAsync(x => x.AtividadeId == atividadeId && x.Status == "ativa", cancellationToken)) });
        }).RequireAuthorization();
    }
}

public sealed record EditarConteudoAula(string? Titulo, string? Tema, string? Descricao);
