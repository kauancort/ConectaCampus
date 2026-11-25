namespace AprendizagemColaborativa.Api.Models;

public sealed class PropostaAula
{
    public Guid Id { get; set; }
    public string AlunoId { get; set; } = string.Empty;
    public Aluno Aluno { get; set; } = null!;
    public string Titulo { get; set; } = string.Empty;
    public string Tema { get; set; } = string.Empty;
    public string Descricao { get; set; } = string.Empty;
    public string? NumeroSala { get; set; }
    public string TipoSala { get; set; } = string.Empty;
    public DateTime Inicio { get; set; }
    public DateTime Fim { get; set; }
    public string Status { get; set; } = "pendente";
    public DateTime CriadaEmUtc { get; set; } = DateTime.UtcNow;
    public string? ProfessorId { get; set; }
    public Aluno? Professor { get; set; }
    public DateTime? AvaliadaEmUtc { get; set; }
    public string? MotivoRecusa { get; set; }
    public Guid? AtividadeId { get; set; }
    public Atividade? Atividade { get; set; }
}
