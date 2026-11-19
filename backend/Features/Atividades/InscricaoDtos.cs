namespace AprendizagemColaborativa.Api.Features.Atividades;

public sealed record CriarInscricaoRequest(Guid? AssentoId);

public sealed record InscricaoResponse(
    Guid Id,
    Guid AtividadeId,
    string Status,
    DateTimeOffset CriadaEmUtc,
    DateTimeOffset AtualizadaEmUtc,
    AssentoReservadoResponse Assento);

public sealed record InscricaoParticipanteResponse(
    string AlunoId,
    string NomeAluno,
    string Status,
    DateTimeOffset CriadaEmUtc,
    DateTimeOffset AtualizadaEmUtc);

public sealed record MinhaInscricaoResponse(
    Guid Id,
    string Status,
    DateTimeOffset CriadaEmUtc,
    DateTimeOffset AtualizadaEmUtc,
    Guid AtividadeId,
    string Tipo,
    string Tema,
    DateTimeOffset DataHora,
    string Local,
    string StatusAtividade,
    SalaAtividadeResponse? Sala,
    AssentoReservadoResponse? Assento,
    DateTimeOffset Inicio,
    DateTimeOffset? Fim);
