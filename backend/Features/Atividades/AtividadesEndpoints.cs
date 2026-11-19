using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

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

        group.MapPost("", async (
            CriarAtividadeRequest? request,
            ClaimsPrincipal principal,
            AtividadeService service,
            CancellationToken cancellationToken) =>
        {
            var errors = service.Validar(request);
            if (errors.Count > 0)
            {
                return Results.ValidationProblem(errors, title: "Revise os dados informados.");
            }

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
        }).RequireAuthorization();

        group.MapGet("", async (
            string? tema,
            AtividadeRepository repository,
            AtividadeService service,
            CancellationToken cancellationToken) =>
        {
            var atividades = await repository.ListarAsync(tema, cancellationToken);
            return Results.Ok(atividades.Select(service.ToResponse));
        });

        group.MapGet("/minhas", async (
            ClaimsPrincipal principal,
            AtividadeRepository repository,
            AtividadeService service,
            CancellationToken cancellationToken) =>
        {
            var responsavelId = principal.FindFirstValue(JwtRegisteredClaimNames.Sub);
            if (string.IsNullOrWhiteSpace(responsavelId))
            {
                return Results.Unauthorized();
            }

            var atividades = await repository.ListarDoResponsavelAsync(responsavelId, cancellationToken);
            return Results.Ok(atividades.Select(service.ToResponse));
        }).RequireAuthorization();

        group.MapGet("/{id}", async (
            string id,
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
                : Results.Ok(service.ToResponse(atividade));
        });
    }
}
