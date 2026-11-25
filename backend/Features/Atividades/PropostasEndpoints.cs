using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using AprendizagemColaborativa.Api.Data;
using AprendizagemColaborativa.Api.Features.Auth;
using AprendizagemColaborativa.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace AprendizagemColaborativa.Api.Features.Atividades;

public static class PropostasEndpoints
{
    private static string? UserId(ClaimsPrincipal user) => user.FindFirstValue(JwtRegisteredClaimNames.Sub);

    public static void MapPropostasEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/propostas").RequireAuthorization();
        group.MapPost("", async (PropostaRequest? input, ClaimsPrincipal principal, ApplicationDbContext db, AtividadeService service, CancellationToken ct) =>
        {
            var user = await db.Users.SingleOrDefaultAsync(x => x.Id == UserId(principal), ct);
            if (user is null) return Results.Unauthorized();
            if (!user.PodeMinistrar && !user.Professor) return Results.Forbid();
            var request = new CriarAtividadeRequest(input?.Tema, input?.Descricao, "AULA", input?.TipoSala, input?.Inicio, input?.Fim, input?.Titulo);
            var errors = service.Validar(request);
            if (string.IsNullOrWhiteSpace(input?.Titulo)) errors["titulo"] = ["Informe o título da aula."];
            else if (input.Titulo.Trim().Length > 200) errors["titulo"] = ["O título deve ter no máximo 200 caracteres."];
            var numero = input?.NumeroSala?.Trim();
            if (string.IsNullOrEmpty(numero) || numero.Length > 6 || !numero.All(c => c >= '0' && c <= '9'))
                errors["numeroSala"] = ["Informe o número da sala usando até 6 dígitos."];
            if (errors.Count > 0) return Results.ValidationProblem(errors);
            numero = numero!.TrimStart('0');
            if (numero.Length == 0) numero = "0";
            var proposal = new PropostaAula { Id = Guid.NewGuid(), AlunoId = user.Id, Titulo = input!.Titulo!.Trim(), Tema = input.Tema!.Trim(), Descricao = input.Descricao!.Trim(), TipoSala = input.TipoSala!.Trim().ToUpperInvariant(), NumeroSala = numero, Inicio = DateTimeOffset.Parse(input.Inicio!).UtcDateTime, Fim = DateTimeOffset.Parse(input.Fim!).UtcDateTime };
            await using var transaction = await db.Database.BeginTransactionAsync(System.Data.IsolationLevel.ReadCommitted, ct);
            if (user.Professor)
            {
                var publicationError = await PublishAsync(proposal, db, service, ct);
                if (publicationError is not null) return publicationError;
                proposal.Status = "aprovada";
                proposal.ProfessorId = user.Id;
                proposal.AvaliadaEmUtc = DateTime.UtcNow;
            }
            db.PropostasAula.Add(proposal);
            if (!user.Professor)
            {
                var teachers = await db.Users.Where(x => x.Professor).Select(x => x.Id).ToListAsync(ct);
                db.Notificacoes.AddRange(teachers.Select(id => new Notificacao { UsuarioId = id, Mensagem = $"{user.Nome} enviou uma proposta de aula: {proposal.Titulo}." }));
            }
            await db.SaveChangesAsync(ct);
            await transaction.CommitAsync(ct);
            return Results.Json(new { proposal.Id, proposal.Status, proposal.AtividadeId }, statusCode: 201);
        });
        group.MapGet("/minhas", async (ClaimsPrincipal principal, ApplicationDbContext db, CancellationToken ct) =>
        {
            var id = UserId(principal);
            var items = await db.PropostasAula.AsNoTracking().Include(x => x.Aluno).Include(x => x.Professor).Where(x => x.AlunoId == id).OrderByDescending(x => x.CriadaEmUtc).ToListAsync(ct);
            return Results.Ok(items.Select(ToResponse));
        });
        group.MapGet("/pendentes", async (ClaimsPrincipal principal, ApplicationDbContext db, CancellationToken ct) =>
        {
            if (!await AcessoEndpoints.ProfessorAsync(db, principal)) return Results.Forbid();
            var items = await db.PropostasAula.AsNoTracking().Include(x => x.Aluno).Where(x => x.Status == "pendente").OrderBy(x => x.CriadaEmUtc).ToListAsync(ct);
            return Results.Ok(items.Select(ToResponse));
        });
        group.MapGet("/aprovadas", async (ClaimsPrincipal principal, ApplicationDbContext db, CancellationToken ct) =>
        {
            if (!await AcessoEndpoints.ProfessorAsync(db, principal)) return Results.Forbid();
            var items = await db.PropostasAula.AsNoTracking().Include(x => x.Aluno).Include(x => x.Professor).Include(x => x.Atividade).Where(x => x.ProfessorId == UserId(principal) && x.Status == "aprovada").OrderByDescending(x => x.AvaliadaEmUtc).ToListAsync(ct);
            return Results.Ok(items.Select(ToResponse));
        });
        group.MapPost("/{id:guid}/avaliar", async (Guid id, AvaliarPropostaRequest? input, ClaimsPrincipal principal, ApplicationDbContext db, AtividadeService service, CancellationToken ct) =>
        {
            if (!await AcessoEndpoints.ProfessorAsync(db, principal)) return Results.Forbid();
            if (input?.Decisao is not ("aprovada" or "recusada")) return Results.BadRequest(new { message = "Decisão inválida." });
            if (input.Decisao == "recusada" && (string.IsNullOrWhiteSpace(input.Motivo) || input.Motivo.Trim().Length > 2000)) return Results.BadRequest(new { message = "Informe o motivo da recusa (até 2000 caracteres)." });
            await using var transaction = await db.Database.BeginTransactionAsync(System.Data.IsolationLevel.ReadCommitted, ct);
            var proposal = (await db.PropostasAula.FromSqlInterpolated($"""SELECT * FROM conectacampus."PropostasAula" WHERE "Id" = {id} FOR UPDATE""").ToListAsync(ct)).SingleOrDefault();
            if (proposal is null) return Results.NotFound();
            if (proposal.Status != "pendente") return Results.Conflict(new { message = "Esta proposta já foi avaliada." });
            if (input.Decisao == "aprovada")
            {
                var aluno = await db.Users.SingleOrDefaultAsync(x => x.Id == proposal.AlunoId, ct);
                if (aluno is null || (!aluno.PodeMinistrar && !aluno.Professor)) return Results.Conflict(new { message = "O usuário não está autorizado a ministrar aulas." });
                var publicationError = await PublishAsync(proposal, db, service, ct);
                if (publicationError is not null) return publicationError;
            }
            proposal.Status = input.Decisao; proposal.ProfessorId = UserId(principal); proposal.AvaliadaEmUtc = DateTime.UtcNow; proposal.MotivoRecusa = input.Decisao == "recusada" ? input.Motivo!.Trim() : null;
            db.Notificacoes.Add(new Notificacao { UsuarioId = proposal.AlunoId, Mensagem = $"Sua proposta de aula ‘{proposal.Titulo}’ foi {proposal.Status}." });
            await db.SaveChangesAsync(ct); await transaction.CommitAsync(ct);
            return Results.Ok(new { proposal.Id, proposal.Status, proposal.AtividadeId });
        });
    }

    private static async Task<IResult?> PublishAsync(PropostaAula proposal, ApplicationDbContext db, AtividadeService service, CancellationToken ct)
    {
        if (proposal.Inicio <= DateTime.UtcNow) return Results.Conflict(new { message = "O horário da aula já passou. Escolha um horário futuro." });
        Guid? salaId = null;
        if (proposal.NumeroSala is { } numeroSala)
        {
            // Serializa a criação e a reserva da mesma sala entre publicações concorrentes.
            await db.Database.ExecuteSqlInterpolatedAsync($"SELECT pg_advisory_xact_lock(hashtextextended({"sala:" + numeroSala}, 0))", ct);
            var sala = await db.Salas.SingleOrDefaultAsync(x => x.Numero == numeroSala, ct);
            if (sala is not null && sala.Tipo != proposal.TipoSala)
                return Results.Conflict(new { message = "Esta sala já está cadastrada com outro tipo. Confira o número e o tipo informado." });
            if (sala is null)
            {
                var colunas = proposal.TipoSala == "NOTEBOOK" ? 5 : 6;
                sala = new Sala { Id = Guid.NewGuid(), Numero = numeroSala, Nome = $"Sala {numeroSala}", Tipo = proposal.TipoSala, Capacidade = 4 * colunas, Status = "disponivel" };
                for (var fileira = 'A'; fileira <= 'D'; fileira++)
                    for (var coluna = 1; coluna <= colunas; coluna++)
                        sala.Assentos.Add(new Assento { Id = Guid.NewGuid(), Codigo = $"{fileira}{coluna}", Fileira = fileira.ToString(), Coluna = coluna });
                db.Salas.Add(sala);
                await db.SaveChangesAsync(ct);
            }
            salaId = sala.Id;
        }
        var request = new CriarAtividadeRequest(proposal.Tema, proposal.Descricao, "AULA", proposal.TipoSala, new DateTimeOffset(proposal.Inicio).ToString("O"), new DateTimeOffset(proposal.Fim).ToString("O"), proposal.Titulo);
        var created = await service.CriarAsync(request, proposal.AlunoId, ct, dentroDeTransacao: true, salaEspecificaId: salaId);
        if (created.SalaIndisponivel) return Results.Conflict(new { message = "A sala não está disponível neste horário. Escolha outro horário." });
        if (created.ResponsavelInvalido) return Results.Conflict(new { message = "Ministrante não encontrado." });
        proposal.AtividadeId = created.Atividade!.Id;
        return null;
    }

    private static object ToResponse(PropostaAula p) => new { p.Id, p.Titulo, p.Tema, p.Descricao, p.TipoSala, p.NumeroSala, p.Inicio, p.Fim, p.Status, p.CriadaEmUtc, p.AvaliadaEmUtc, p.MotivoRecusa, p.AtividadeId, Aluno = p.Aluno.Nome, Professor = p.Professor?.Nome, StatusAtividade = p.Atividade?.Status };
}

public sealed record PropostaRequest(string? Titulo, string? Tema, string? Descricao, string? TipoSala, string? Inicio, string? Fim, string? NumeroSala);
public sealed record AvaliarPropostaRequest(string? Decisao, string? Motivo);
