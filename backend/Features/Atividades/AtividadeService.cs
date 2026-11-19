using System.Globalization;
using System.Text.RegularExpressions;
using AprendizagemColaborativa.Api.Data;
using AprendizagemColaborativa.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace AprendizagemColaborativa.Api.Features.Atividades;

public sealed partial class AtividadeService(ApplicationDbContext db, AtividadeRepository repository)
{
    private const int MaxTemaLength = 200;
    private const int MaxDescricaoLength = 5000;
    private static readonly string[] ModalidadesAceitas = ["AULA", "PALESTRA"];
    private static readonly string[] TiposSalaAceitos = ["DESKTOP", "NOTEBOOK"];

    public Dictionary<string, string[]> Validar(CriarAtividadeRequest? request)
    {
        var errors = new Dictionary<string, string[]>();
        if (request is null)
        {
            return new() { ["body"] = ["O corpo da requisição é obrigatório."] };
        }

        AddRequired(errors, "tema", request.Tema);
        AddRequired(errors, "descricao", request.Descricao);
        AddRequired(errors, "modalidade", request.Modalidade);
        AddRequired(errors, "tipoSala", request.TipoSala);
        AddRequired(errors, "inicio", request.Inicio);
        AddRequired(errors, "fim", request.Fim);
        AddLengthError(errors, "tema", request.Tema, MaxTemaLength);
        AddLengthError(errors, "descricao", request.Descricao, MaxDescricaoLength);

        if (!string.IsNullOrWhiteSpace(request.Modalidade) && !ModalidadesAceitas.Contains(Normalize(request.Modalidade)))
        {
            errors["modalidade"] = ["A modalidade deve ser AULA ou PALESTRA."];
        }

        if (!string.IsNullOrWhiteSpace(request.TipoSala) && !TiposSalaAceitos.Contains(Normalize(request.TipoSala)))
        {
            errors["tipoSala"] = ["O tipo de sala deve ser DESKTOP ou NOTEBOOK."];
        }

        var inicioValido = TryParseUtc(request.Inicio, out var inicio);
        var fimValido = TryParseUtc(request.Fim, out var fim);
        if (!string.IsNullOrWhiteSpace(request.Inicio) && !inicioValido)
        {
            errors["inicio"] = ["Use data e horário ISO 8601 com fuso explícito."];
        }

        if (!string.IsNullOrWhiteSpace(request.Fim) && !fimValido)
        {
            errors["fim"] = ["Use data e horário ISO 8601 com fuso explícito."];
        }

        if (inicioValido && fimValido && fim <= inicio)
        {
            errors["fim"] = ["O horário de término deve ser posterior ao início."];
        }

        return errors;
    }

    public async Task<CriarAtividadeResultado> CriarAsync(
        CriarAtividadeRequest request,
        string responsavelId,
        CancellationToken cancellationToken)
    {
        TryParseUtc(request.Inicio, out var inicio);
        TryParseUtc(request.Fim, out var fim);
        var modalidade = Normalize(request.Modalidade!);
        var tipoSala = Normalize(request.TipoSala!);

        await using var transaction = await db.Database.BeginTransactionAsync(
            System.Data.IsolationLevel.Serializable,
            cancellationToken);

        var responsavel = await repository.ObterResponsavelAsync(responsavelId, cancellationToken);
        if (responsavel is null)
        {
            await transaction.RollbackAsync(cancellationToken);
            return CriarAtividadeResultado.ResponsavelAusente;
        }

        var salas = await repository.ObterSalasDisponiveisComBloqueioAsync(tipoSala, cancellationToken);
        foreach (var sala in salas)
        {
            var possuiConflito = await repository.ExisteConflitoDeHorarioAsync(
                sala.Id,
                inicio.UtcDateTime,
                fim.UtcDateTime,
                cancellationToken);
            if (possuiConflito)
            {
                continue;
            }

            var atividade = new Atividade
            {
                Id = Guid.NewGuid(),
                Tema = request.Tema!.Trim(),
                Descricao = request.Descricao!.Trim(),
                Modalidade = modalidade,
                SalaId = sala.Id,
                Sala = sala,
                ResponsavelId = responsavel.Id,
                Responsavel = responsavel,
                Inicio = inicio.UtcDateTime,
                Fim = fim.UtcDateTime,
                OrganizadorId = responsavel.Id,
                Organizador = responsavel,
                Tipo = modalidade.ToLowerInvariant(),
                Nivel = "não informado",
                DataHoraUtc = inicio.UtcDateTime,
                Local = sala.Nome,
                NumeroDeVagas = sala.Capacidade,
                Status = "publicada"
            };

            repository.Adicionar(atividade);
            await repository.SalvarAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
            return new(ToResponse(atividade));
        }

        await transaction.RollbackAsync(cancellationToken);
        return CriarAtividadeResultado.SemSalaDisponivel;
    }

    public AtividadeResponse ToResponse(Atividade atividade)
    {
        if (atividade.Sala is null || atividade.Inicio is null || atividade.Fim is null || string.IsNullOrWhiteSpace(atividade.Modalidade))
        {
            throw new InvalidOperationException("A atividade não possui os dados de sala e horário necessários.");
        }

        var inicio = ToUtcOffset(atividade.Inicio.Value);
        return new(
            atividade.Id,
            atividade.Tema,
            atividade.Descricao,
            atividade.Modalidade,
            new(atividade.Sala.Id, atividade.Sala.Nome, atividade.Sala.Tipo, atividade.Sala.Capacidade),
            DateOnly.FromDateTime(inicio.DateTime),
            inicio,
            ToUtcOffset(atividade.Fim.Value),
            atividade.Status);
    }

    private static string Normalize(string value) => value.Trim().ToUpperInvariant();

    private static bool TryParseUtc(string? value, out DateTimeOffset parsed)
    {
        parsed = default;
        return !string.IsNullOrWhiteSpace(value) &&
            IsoDateTimeOffsetRegex().IsMatch(value) &&
            DateTimeOffset.TryParse(value, CultureInfo.InvariantCulture, DateTimeStyles.None, out parsed);
    }

    private static DateTimeOffset ToUtcOffset(DateTime value) =>
        new(DateTime.SpecifyKind(value, DateTimeKind.Utc));

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

public sealed record CriarAtividadeResultado(AtividadeResponse? Atividade, bool SalaIndisponivel, bool ResponsavelInvalido)
{
    public CriarAtividadeResultado(AtividadeResponse atividade) : this(atividade, false, false)
    {
    }

    public static CriarAtividadeResultado SemSalaDisponivel { get; } = new(null, true, false);
    public static CriarAtividadeResultado ResponsavelAusente { get; } = new(null, false, true);
}
