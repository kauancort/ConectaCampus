using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

namespace AprendizagemColaborativa.Api.Features.Atividades;

public static class AssentosEndpoints
{
    public static void MapAssentosEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet("/api/atividades/{id}/assentos", async (
            string id, ClaimsPrincipal principal, AssentosService service, CancellationToken cancellationToken) =>
        {
            if (!Guid.TryParse(id, out var atividadeId))
            {
                return Results.BadRequest(new { message = "O identificador da atividade deve ser um UUID válido." });
            }

            var atividade = await service.ObterAtividadeAsync(atividadeId, cancellationToken);
            if (atividade is null)
            {
                return Results.NotFound();
            }

            if (atividade.Sala is null)
            {
                return Results.Conflict(new { message = "A atividade não possui sala com assentos definidos." });
            }

            var alunoId = principal.FindFirstValue(JwtRegisteredClaimNames.Sub);
            if (string.IsNullOrWhiteSpace(alunoId)) return Results.Unauthorized();
            return Results.Ok(await service.ConsultarAsync(atividade, alunoId, cancellationToken));
        }).RequireAuthorization().WithTags("Assentos");
    }
}
