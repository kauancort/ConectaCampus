namespace AprendizagemColaborativa.Api.Features.Atividades;

public static class AssentosEndpoints
{
    public static void MapAssentosEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet("/api/atividades/{id}/assentos", async (
            string id, AssentosService service, CancellationToken cancellationToken) =>
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

            return Results.Ok(await service.ConsultarAsync(atividade, cancellationToken));
        }).WithTags("Assentos");
    }
}
