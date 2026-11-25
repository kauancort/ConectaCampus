using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using AprendizagemColaborativa.Api.Data;
using AprendizagemColaborativa.Api.Features.Auth;
using AprendizagemColaborativa.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace AprendizagemColaborativa.Api.Features.Atividades;

public static class InscricoesEndpoints
{
    public static void MapInscricoesEndpoints(this IEndpointRouteBuilder app)
    {
        var activities = app.MapGroup("/api/atividades").WithTags("Inscricoes");

        activities.MapPost("/{id}/inscricoes", async (
            string id,
            CriarInscricaoRequest? request,
            ClaimsPrincipal principal,
            InscricaoService service,
            CancellationToken cancellationToken) =>
        {
            if (!Guid.TryParse(id, out var activityId))
            {
                return Results.BadRequest(new { message = "O identificador da atividade deve ser um UUID válido." });
            }

            var studentId = principal.FindFirstValue(JwtRegisteredClaimNames.Sub);
            if (string.IsNullOrWhiteSpace(studentId))
            {
                return Results.Unauthorized();
            }

            if (request?.AssentoId is not { } assentoId || assentoId == Guid.Empty)
            {
                return Results.ValidationProblem(new Dictionary<string, string[]>
                {
                    ["assentoId"] = ["Informe um identificador de assento válido."]
                });
            }

            var result = await service.ReservarAsync(activityId, assentoId, studentId, cancellationToken);
            return result.Situacao switch
            {
                SituacaoInscricao.Criada => Results.Json(result.Inscricao, statusCode: StatusCodes.Status201Created),
                SituacaoInscricao.AlunoInexistente => Results.Unauthorized(),
                SituacaoInscricao.AtividadeInexistente => Results.NotFound(),
                SituacaoInscricao.AssentoInvalido => Results.BadRequest(new { message = "O assento não pertence à sala da atividade." }),
                SituacaoInscricao.AtividadeCancelada => Results.Conflict(new { message = "Atividades canceladas não aceitam inscrições." }),
                SituacaoInscricao.AtividadeEncerrada => Results.Conflict(new { message = "A atividade já começou." }),
                SituacaoInscricao.AlunoJaInscrito => Results.Conflict(new { message = "Você já está inscrito nesta atividade." }),
                SituacaoInscricao.AssentoOcupado => Results.Conflict(new { message = "Este assento já está ocupado nesta atividade." }),
                SituacaoInscricao.SemVagas => Results.Conflict(new { message = "Não há vagas disponíveis nesta atividade." }),
                _ => Results.Conflict(new { message = "A inscrição ou o assento já foi reservado nesta atividade." })
            };
        }).RequireAuthorization();

        activities.MapDelete("/{id}/inscricoes/me", async (
            string id,
            ClaimsPrincipal principal,
            ApplicationDbContext db) =>
        {
            if (!Guid.TryParse(id, out var activityId))
            {
                return Results.BadRequest(new { message = "O identificador da atividade deve ser um UUID válido." });
            }

            var studentId = principal.FindFirstValue(JwtRegisteredClaimNames.Sub);
            if (string.IsNullOrWhiteSpace(studentId))
            {
                return Results.Unauthorized();
            }

            await using var transaction = await db.Database.BeginTransactionAsync();
            var activity = (await LockActivityAsync(db, activityId)).SingleOrDefault();
            if (activity is null)
            {
                return Results.NotFound();
            }

            var enrollment = await db.Inscricoes.SingleOrDefaultAsync(item =>
                item.AtividadeId == activityId && item.AlunoId == studentId);
            if (enrollment is not null && enrollment.Status == "ativa")
            {
                enrollment.Status = "cancelada";
                enrollment.Presente = null;
                enrollment.AssentoId = null;
                enrollment.AtualizadaEmUtc = DateTime.UtcNow;
                await db.SaveChangesAsync();
            }

            await transaction.CommitAsync();
            return Results.NoContent();
        }).RequireAuthorization();

        activities.MapGet("/{id}/inscricoes", async (
            string id,
            ClaimsPrincipal principal,
            ApplicationDbContext db) =>
        {
            if (!Guid.TryParse(id, out var activityId))
            {
                return Results.BadRequest(new { message = "O identificador da atividade deve ser um UUID válido." });
            }

            var activity = await db.Atividades.AsNoTracking()
                .SingleOrDefaultAsync(item => item.Id == activityId);
            if (activity is null)
            {
                return Results.NotFound();
            }

            var studentId = principal.FindFirstValue(JwtRegisteredClaimNames.Sub);
            var approver = await AcessoEndpoints.ProfessorAsync(db, principal) && await db.PropostasAula.AnyAsync(p => p.AtividadeId == activityId && p.ProfessorId == studentId && p.Status == "aprovada");
            if (!string.Equals(activity.OrganizadorId, studentId, StringComparison.Ordinal) && !string.Equals(activity.ResponsavelId, studentId, StringComparison.Ordinal) && !approver)
            {
                return Results.Forbid();
            }

            var enrollments = (await db.Inscricoes.AsNoTracking()
                .Include(item => item.Aluno)
                .Include(item => item.Assento)
                .Where(item => item.AtividadeId == activityId)
                .OrderBy(item => item.CriadaEmUtc)
                .ToListAsync())
                .Select(item => new InscricaoParticipanteResponse(
                    item.AlunoId,
                    item.Aluno.Nome,
                    item.Status,
                    ToUtcOffset(item.CriadaEmUtc),
                    ToUtcOffset(item.AtualizadaEmUtc), item.Presente,
                    item.Assento is { } assento ? new(assento.Id, assento.Codigo, assento.Fileira, assento.Coluna) : null))
                .ToList();
            return Results.Ok(enrollments);
        }).RequireAuthorization();

        activities.MapPut("/{id:guid}/inscricoes/{alunoId}/presenca", async (Guid id, string alunoId, PresencaRequest? request, ClaimsPrincipal principal, ApplicationDbContext db) =>
        {
            var userId = principal.FindFirstValue(JwtRegisteredClaimNames.Sub);
            if (string.IsNullOrWhiteSpace(userId)) return Results.Unauthorized();
            var ministrante = await db.Atividades.AnyAsync(a => a.Id == id && a.ResponsavelId == userId && db.PropostasAula.Any(p => p.AtividadeId == id && p.AlunoId == userId && p.Status == "aprovada"));
            var professor = await AcessoEndpoints.ProfessorAsync(db, principal) && await db.PropostasAula.AnyAsync(p => p.AtividadeId == id && p.ProfessorId == userId && p.Status == "aprovada");
            if (!ministrante && !professor) return Results.Forbid();
            if (request is null) return Results.BadRequest();
            var enrollment = await db.Inscricoes.SingleOrDefaultAsync(x => x.AtividadeId == id && x.AlunoId == alunoId && x.Status == "ativa");
            if (enrollment is null) return Results.NotFound();
            enrollment.Presente = request.Presente;
            enrollment.AtualizadaEmUtc = DateTime.UtcNow;
            await db.SaveChangesAsync();
            return Results.NoContent();
        }).RequireAuthorization();

        app.MapGet("/api/inscricoes/minhas", ListarMinhasAsync).RequireAuthorization().WithTags("Inscricoes");
        app.MapGet("/api/inscricoes/me", ListarMinhasAsync).RequireAuthorization().WithTags("Inscricoes");
    }

    private static async Task<IResult> ListarMinhasAsync(
        ClaimsPrincipal principal, InscricaoService service, CancellationToken cancellationToken)
    {
        var studentId = principal.FindFirstValue(JwtRegisteredClaimNames.Sub);
        if (string.IsNullOrWhiteSpace(studentId))
        {
            return Results.Unauthorized();
        }

        return Results.Ok(await service.ListarDoAlunoAsync(studentId, cancellationToken));
    }

    private static Task<List<Atividade>> LockActivityAsync(ApplicationDbContext db, Guid activityId) =>
        db.Atividades
            .FromSqlInterpolated($"""SELECT * FROM conectacampus."Atividades" WHERE "Id" = {activityId} FOR UPDATE""")
            .AsTracking()
            .ToListAsync();

    private static DateTimeOffset ToUtcOffset(DateTime value) =>
        new(DateTime.SpecifyKind(value, DateTimeKind.Utc));

}
