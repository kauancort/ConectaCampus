namespace AprendizagemColaborativa.Api.Models;

public sealed class SolicitacaoProfessor
{
    public Guid Id { get; set; }
    public string AlunoId { get; set; } = string.Empty;
    public string Justificativa { get; set; } = string.Empty;
    public string Status { get; set; } = "pendente";
    public DateTime CriadaEmUtc { get; set; } = DateTime.UtcNow;
    public string? AvaliadorId { get; set; }
    public DateTime? AvaliadaEmUtc { get; set; }
}

public sealed class Notificacao
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string UsuarioId { get; set; } = string.Empty;
    public string Mensagem { get; set; } = string.Empty;
    public bool Lida { get; set; }
    public DateTime CriadaEmUtc { get; set; } = DateTime.UtcNow;
}
