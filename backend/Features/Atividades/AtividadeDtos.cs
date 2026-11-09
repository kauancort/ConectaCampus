namespace AprendizagemColaborativa.Api.Features.Atividades;

public sealed record AtividadeRequest(
    string? Tipo,
    string? Tema,
    string? Descricao,
    string? Nivel,
    string? DataHora,
    string? Local,
    int? NumeroDeVagas);

public sealed record AtividadeListaResponse(
    Guid Id,
    string Tipo,
    string Tema,
    string Descricao,
    string Nivel,
    DateTimeOffset DataHora,
    string Local,
    int NumeroDeVagas,
    string Status);

public sealed record AtividadeDetalheResponse(
    Guid Id,
    string Tipo,
    string Tema,
    string Descricao,
    string Nivel,
    DateTimeOffset DataHora,
    string Local,
    int NumeroDeVagas,
    string Status,
    string NomeOrganizador);
