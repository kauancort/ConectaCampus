namespace AprendizagemColaborativa.Api.Features.Atividades;

public sealed record CriarAtividadeRequest(
    string? Tema,
    string? Descricao,
    string? Modalidade,
    string? TipoSala,
    string? Inicio,
    string? Fim,
    string? Titulo = null);

public sealed record TipoSalaResponse(string Tipo, int Capacidade);

public sealed record SalaAtividadeResponse(Guid Id, string Nome, string Tipo, int Capacidade);

public sealed record AtividadeResponse(
    Guid Id,
    string Tema,
    string Descricao,
    string Modalidade,
    SalaAtividadeResponse Sala,
    DateOnly Data,
    DateTimeOffset Inicio,
    DateTimeOffset Fim,
    string Status,
    string? NomeProfessor,
    int LugaresDisponiveis,
    string Titulo,
    string? NomeMinistrante);

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
