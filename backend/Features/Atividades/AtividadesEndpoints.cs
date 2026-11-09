using System.Globalization;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text.RegularExpressions;
using AprendizagemColaborativa.Api.Data;
using AprendizagemColaborativa.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace AprendizagemColaborativa.Api.Features.Atividades;

public static partial class AtividadesEndpoints
{
    private static readonly string[] TiposAceitos = ["aula", "palestra", "grupo de estudo"];
    private const int MaxTemaLength = 200;
    private const int MaxDescricaoLength = 5000;
    private const int MaxNivelLength = 80;
    private const int MaxLocalLength = 300;

    public static void MapAtividadesEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/atividades").WithTags("Atividades");

        group.MapPost("", async (AtividadeRequest? request, ClaimsPrincipal principal, ApplicationDbContext db) =>
        {
            var errors = Validate(request);
            if (errors.Count > 0)
            {
                return Results.ValidationProblem(errors, title: "Revise os dados informados.");
            }

            var organizerId = principal.FindFirstValue(JwtRegisteredClaimNames.Sub);
            if (string.IsNullOrWhiteSpace(organizerId))
            {
                return Results.Unauthorized();
            }

            var organizerName = await db.Users.Where(aluno => aluno.Id == organizerId)
                .Select(aluno => aluno.Nome).SingleOrDefaultAsync();
            if (organizerName is null)
            {
                return Results.Unauthorized();
            }

            var activity = new Atividade
            {
                Id = Guid.NewGuid(),
                OrganizadorId = organizerId,
                Tipo = NormalizeType(request!.Tipo!),
                Tema = request.Tema!.Trim(),
                Descricao = request.Descricao!.Trim(),
                Nivel = request.Nivel!.Trim(),
                DataHoraUtc = ParseUtc(request.DataHora!),
                Local = request.Local!.Trim(),
                NumeroDeVagas = request.NumeroDeVagas!.Value,
                Status = "publicada"
            };

            db.Atividades.Add(activity);
            await db.SaveChangesAsync();
            return Results.Created($"/api/atividades/{activity.Id}", ToDetails(activity, organizerName));
        }).RequireAuthorization();

        group.MapGet("", async (
            string? tema,
            string? tipo,
            string? nivel,
            DateOnly? data,
            ApplicationDbContext db) =>
        {
            var errors = ValidateFilters(tema, tipo, nivel);
            if (errors.Count > 0)
            {
                return Results.ValidationProblem(errors, title: "Revise os filtros informados.");
            }

            var query = db.Atividades.AsNoTracking().AsQueryable();
            if (!string.IsNullOrWhiteSpace(tema))
            {
                var themeFilter = tema.Trim();
                query = query.Where(activity => activity.Tema.Contains(themeFilter));
            }

            if (!string.IsNullOrWhiteSpace(tipo))
            {
                var typeFilter = NormalizeType(tipo);
                query = query.Where(activity => activity.Tipo == typeFilter);
            }

            if (!string.IsNullOrWhiteSpace(nivel))
            {
                var levelFilter = nivel.Trim();
                query = query.Where(activity => activity.Nivel == levelFilter);
            }

            if (data.HasValue)
            {
                var startUtc = DateTime.SpecifyKind(data.Value.ToDateTime(TimeOnly.MinValue), DateTimeKind.Utc);
                var endUtc = startUtc.AddDays(1);
                query = query.Where(activity => activity.DataHoraUtc >= startUtc && activity.DataHoraUtc < endUtc);
            }

            var activities = (await query.OrderBy(activity => activity.DataHoraUtc).ToListAsync())
                .Select(ToListItem)
                .ToList();
            return Results.Ok(activities);
        });

        group.MapGet("/minhas", async (ClaimsPrincipal principal, ApplicationDbContext db) =>
        {
            var organizerId = principal.FindFirstValue(JwtRegisteredClaimNames.Sub);
            if (string.IsNullOrWhiteSpace(organizerId))
            {
                return Results.Unauthorized();
            }

            var activities = (await db.Atividades.AsNoTracking()
                    .Where(activity => activity.OrganizadorId == organizerId)
                    .OrderBy(activity => activity.DataHoraUtc)
                    .ToListAsync())
                .Select(ToListItem)
                .ToList();
            return Results.Ok(activities);
        }).RequireAuthorization();

        group.MapGet("/{id}", async (string id, ApplicationDbContext db) =>
        {
            if (!Guid.TryParse(id, out var activityId))
            {
                return Results.BadRequest(new { message = "O identificador da atividade deve ser um UUID válido." });
            }

            var activity = await db.Atividades.AsNoTracking()
                .Include(item => item.Organizador)
                .SingleOrDefaultAsync(item => item.Id == activityId);
            return activity is null
                ? Results.NotFound()
                : Results.Ok(ToDetails(activity, activity.Organizador.Nome));
        });

        group.MapPut("/{id}", async (
            string id,
            AtividadeRequest? request,
            ClaimsPrincipal principal,
            ApplicationDbContext db) =>
        {
            if (!Guid.TryParse(id, out var activityId))
            {
                return Results.BadRequest(new { message = "O identificador da atividade deve ser um UUID válido." });
            }

            await using var transaction = await db.Database.BeginTransactionAsync();
            var lockedActivities = await db.Atividades
                .FromSqlInterpolated($"SELECT * FROM `Atividades` WHERE `Id` = {activityId} FOR UPDATE")
                .AsTracking()
                .ToListAsync();
            var activity = lockedActivities.SingleOrDefault();
            if (activity is null)
            {
                return Results.NotFound();
            }

            var organizerId = principal.FindFirstValue(JwtRegisteredClaimNames.Sub);
            if (!string.Equals(activity.OrganizadorId, organizerId, StringComparison.Ordinal))
            {
                return Results.Forbid();
            }

            var errors = Validate(request);
            if (errors.Count > 0)
            {
                return Results.ValidationProblem(errors, title: "Revise os dados informados.");
            }

            var activeEnrollments = await db.Inscricoes.CountAsync(enrollment =>
                enrollment.AtividadeId == activityId && enrollment.Status == "ativa");
            if (request!.NumeroDeVagas!.Value < activeEnrollments)
            {
                    return Results.Conflict(new { message = "O número de vagas não pode ser menor que as inscrições ativas." });
            }

            ApplyRequest(activity, request!);
            await db.SaveChangesAsync();
            await transaction.CommitAsync();
            var organizerName = await db.Users.Where(aluno => aluno.Id == organizerId)
                .Select(aluno => aluno.Nome).SingleAsync();
            return Results.Ok(ToDetails(activity, organizerName));
        }).RequireAuthorization();

        group.MapPatch("/{id}/cancelar", async (
            string id,
            ClaimsPrincipal principal,
            ApplicationDbContext db) =>
        {
            if (!Guid.TryParse(id, out var activityId))
            {
                return Results.BadRequest(new { message = "O identificador da atividade deve ser um UUID válido." });
            }

            var activity = await db.Atividades.SingleOrDefaultAsync(item => item.Id == activityId);
            if (activity is null)
            {
                return Results.NotFound();
            }

            var organizerId = principal.FindFirstValue(JwtRegisteredClaimNames.Sub);
            if (!string.Equals(activity.OrganizadorId, organizerId, StringComparison.Ordinal))
            {
                return Results.Forbid();
            }

            activity.Status = "cancelada";
            await db.SaveChangesAsync();
            return Results.NoContent();
        }).RequireAuthorization();
    }

    private static Dictionary<string, string[]> Validate(AtividadeRequest? request)
    {
        var errors = new Dictionary<string, string[]>();
        if (request is null)
        {
            return new() { ["body"] = ["O corpo da requisição é obrigatório."] };
        }

        AddRequired(errors, "tipo", request.Tipo);
        AddRequired(errors, "tema", request.Tema);
        AddRequired(errors, "descricao", request.Descricao);
        AddRequired(errors, "nivel", request.Nivel);
        AddRequired(errors, "dataHora", request.DataHora);
        AddRequired(errors, "local", request.Local);
        if (request.NumeroDeVagas is null || request.NumeroDeVagas <= 0)
        {
            errors["numeroDeVagas"] = ["Informe um número de vagas inteiro e positivo."];
        }

        if (!string.IsNullOrWhiteSpace(request.Tipo) && !IsSupportedType(request.Tipo))
        {
            errors["tipo"] = ["O tipo deve ser aula, palestra ou grupo de estudo."];
        }

        AddLengthError(errors, "tema", request.Tema, MaxTemaLength);
        AddLengthError(errors, "descricao", request.Descricao, MaxDescricaoLength);
        AddLengthError(errors, "nivel", request.Nivel, MaxNivelLength);
        AddLengthError(errors, "local", request.Local, MaxLocalLength);

        if (!string.IsNullOrWhiteSpace(request.DataHora) && !TryParseOffsetDateTime(request.DataHora, out _))
        {
            errors["dataHora"] = ["Use uma data e horário ISO 8601 com fuso explícito, por exemplo 2027-03-20T14:00:00Z."];
        }

        return errors;
    }

    private static Dictionary<string, string[]> ValidateFilters(string? tema, string? tipo, string? nivel)
    {
        var errors = new Dictionary<string, string[]>();
        if (!string.IsNullOrWhiteSpace(tipo) && !IsSupportedType(tipo))
        {
            errors["tipo"] = ["O tipo deve ser aula, palestra ou grupo de estudo."];
        }
        AddLengthError(errors, "tema", tema, MaxTemaLength);
        AddLengthError(errors, "nivel", nivel, MaxNivelLength);
        return errors;
    }

    private static void ApplyRequest(Atividade activity, AtividadeRequest request)
    {
        activity.Tipo = NormalizeType(request.Tipo!);
        activity.Tema = request.Tema!.Trim();
        activity.Descricao = request.Descricao!.Trim();
        activity.Nivel = request.Nivel!.Trim();
        activity.DataHoraUtc = ParseUtc(request.DataHora!);
        activity.Local = request.Local!.Trim();
        activity.NumeroDeVagas = request.NumeroDeVagas!.Value;
    }

    private static AtividadeListaResponse ToListItem(Atividade activity) => new(
        activity.Id,
        activity.Tipo,
        activity.Tema,
        activity.Descricao,
        activity.Nivel,
        new DateTimeOffset(DateTime.SpecifyKind(activity.DataHoraUtc, DateTimeKind.Utc)),
        activity.Local,
        activity.NumeroDeVagas,
        activity.Status);

    private static AtividadeDetalheResponse ToDetails(Atividade activity, string organizerName) => new(
        activity.Id,
        activity.Tipo,
        activity.Tema,
        activity.Descricao,
        activity.Nivel,
        new DateTimeOffset(DateTime.SpecifyKind(activity.DataHoraUtc, DateTimeKind.Utc)),
        activity.Local,
        activity.NumeroDeVagas,
        activity.Status,
        organizerName);

    private static string NormalizeType(string value) => value.Trim().ToLowerInvariant();
    private static bool IsSupportedType(string value) => TiposAceitos.Contains(NormalizeType(value), StringComparer.Ordinal);

    private static DateTime ParseUtc(string value)
    {
        TryParseOffsetDateTime(value, out var parsed);
        return parsed.UtcDateTime;
    }

    private static bool TryParseOffsetDateTime(string value, out DateTimeOffset parsed)
    {
        parsed = default;
        return IsoDateTimeOffsetRegex().IsMatch(value) && DateTimeOffset.TryParse(
            value,
            CultureInfo.InvariantCulture,
            DateTimeStyles.None,
            out parsed);
    }

    private static void AddRequired(IDictionary<string, string[]> errors, string field, string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            errors[field] = ["Este campo é obrigatório."];
        }
    }

    private static void AddLengthError(IDictionary<string, string[]> errors, string field, string? value, int maxLength)
    {
        if (!string.IsNullOrWhiteSpace(value) && value.Trim().Length > maxLength)
        {
            errors[field] = [$"Este campo deve ter no máximo {maxLength} caracteres."];
        }
    }

    [GeneratedRegex(@"^\d{4}-\d{2}-\d{2}T\d{2}:\d{2}:\d{2}(?:\.\d+)?(?:[Zz]|[+-]\d{2}:\d{2})$", RegexOptions.CultureInvariant)]
    private static partial Regex IsoDateTimeOffsetRegex();
}
