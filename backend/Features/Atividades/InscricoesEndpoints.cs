using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using AprendizagemColaborativa.Api.Data;
using AprendizagemColaborativa.Api.Models;
using Microsoft.EntityFrameworkCore;
using MySql.Data.MySqlClient;

namespace AprendizagemColaborativa.Api.Features.Atividades;

public static class InscricoesEndpoints
{
    public static void MapInscricoesEndpoints(this IEndpointRouteBuilder app)
    {
        var activities = app.MapGroup("/api/atividades").WithTags("Inscricoes");

        activities.MapPost("/{id}/inscricoes", async (
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
            var lockedActivities = await LockActivityAsync(db, activityId);
            var activity = lockedActivities.SingleOrDefault();
            if (activity is null)
            {
                return Results.NotFound();
            }

            if (activity.Status != "publicada")
            {
                return Results.Conflict(new { message = "Atividades canceladas não aceitam inscrições." });
            }

            var existing = await db.Inscricoes.SingleOrDefaultAsync(enrollment =>
                enrollment.AtividadeId == activityId && enrollment.AlunoId == studentId);
            if (existing?.Status == "ativa")
            {
                return Results.Conflict(new { message = "Você já está inscrito nesta atividade." });
            }

            var activeCount = await db.Inscricoes.CountAsync(enrollment =>
                enrollment.AtividadeId == activityId && enrollment.Status == "ativa");
            if (activeCount >= activity.NumeroDeVagas)
            {
                return Results.Conflict(new { message = "Não há vagas disponíveis nesta atividade." });
            }

            var now = DateTime.UtcNow;
            var reactivated = existing is not null;
            if (existing is null)
            {
                existing = new Inscricao
                {
                    Id = Guid.NewGuid(),
                    AtividadeId = activityId,
                    AlunoId = studentId,
                    Status = "ativa",
                    CriadaEmUtc = now,
                    AtualizadaEmUtc = now
                };
                db.Inscricoes.Add(existing);
            }
            else
            {
                existing.Status = "ativa";
                existing.AtualizadaEmUtc = now;
            }

            try
            {
                await db.SaveChangesAsync();
                await transaction.CommitAsync();
            }
            catch (DbUpdateException exception) when (IsDuplicateKey(exception))
            {
                return Results.Conflict(new { message = "Você já está inscrito nesta atividade." });
            }

            var response = ToResponse(existing);
            return reactivated
                ? Results.Ok(response)
                : Results.Json(response, statusCode: StatusCodes.Status201Created);
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
            if (!string.Equals(activity.OrganizadorId, studentId, StringComparison.Ordinal))
            {
                return Results.Forbid();
            }

            var enrollments = (await db.Inscricoes.AsNoTracking()
                .Include(item => item.Aluno)
                .Where(item => item.AtividadeId == activityId)
                .OrderBy(item => item.CriadaEmUtc)
                .ToListAsync())
                .Select(item => new InscricaoParticipanteResponse(
                    item.AlunoId,
                    item.Aluno.Nome,
                    item.Status,
                    ToUtcOffset(item.CriadaEmUtc),
                    ToUtcOffset(item.AtualizadaEmUtc)))
                .ToList();
            return Results.Ok(enrollments);
        }).RequireAuthorization();

        app.MapGet("/api/inscricoes/me", async (ClaimsPrincipal principal, ApplicationDbContext db) =>
        {
            var studentId = principal.FindFirstValue(JwtRegisteredClaimNames.Sub);
            if (string.IsNullOrWhiteSpace(studentId))
            {
                return Results.Unauthorized();
            }

            var enrollments = (await db.Inscricoes.AsNoTracking()
                .Include(item => item.Atividade)
                .Where(item => item.AlunoId == studentId)
                .OrderByDescending(item => item.AtualizadaEmUtc)
                .ToListAsync())
                .Select(item => new MinhaInscricaoResponse(
                    item.Id,
                    item.Status,
                    ToUtcOffset(item.CriadaEmUtc),
                    ToUtcOffset(item.AtualizadaEmUtc),
                    item.AtividadeId,
                    item.Atividade.Tipo,
                    item.Atividade.Tema,
                    ToUtcOffset(item.Atividade.DataHoraUtc),
                    item.Atividade.Local,
                    item.Atividade.Status))
                .ToList();
            return Results.Ok(enrollments);
        }).RequireAuthorization().WithTags("Inscricoes");
    }

    private static Task<List<Atividade>> LockActivityAsync(ApplicationDbContext db, Guid activityId) =>
        db.Atividades
            .FromSqlInterpolated($"SELECT * FROM `Atividades` WHERE `Id` = {activityId} FOR UPDATE")
            .AsTracking()
            .ToListAsync();

    private static InscricaoResponse ToResponse(Inscricao enrollment) => new(
        enrollment.Id,
        enrollment.AtividadeId,
        enrollment.Status,
        ToUtcOffset(enrollment.CriadaEmUtc),
        ToUtcOffset(enrollment.AtualizadaEmUtc));

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
