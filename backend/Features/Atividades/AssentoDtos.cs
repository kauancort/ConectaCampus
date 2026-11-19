namespace AprendizagemColaborativa.Api.Features.Atividades;

public sealed record AssentoResponse(Guid Id, string Codigo, string Fileira, int Coluna, string Status);

public sealed record AssentoReservadoResponse(Guid Id, string Codigo, string Fileira, int Coluna);

public sealed record MapaAssentosResponse(
    Guid AtividadeId,
    string Tema,
    DateOnly Data,
    DateTimeOffset Inicio,
    DateTimeOffset? Fim,
    SalaAtividadeResponse Sala,
    int CapacidadeTotal,
    int LugaresDisponiveis,
    IReadOnlyList<AssentoResponse> Assentos);
