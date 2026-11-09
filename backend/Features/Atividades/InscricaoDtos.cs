namespace AprendizagemColaborativa.Api.Features.Atividades;

public sealed record InscricaoResponse(
    Guid Id,
    Guid AtividadeId,
    string Status,
    DateTimeOffset CriadaEmUtc,
    DateTimeOffset AtualizadaEmUtc);

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
    string StatusAtividade);
