namespace AprendizagemColaborativa.Api.Models;

public sealed class Inscricao
{
    public Guid Id { get; set; }
    public Guid AtividadeId { get; set; }
    public Atividade Atividade { get; set; } = null!;
    public string AlunoId { get; set; } = string.Empty;
    public Aluno Aluno { get; set; } = null!;
    public Guid? AssentoId { get; set; }
    public Assento? Assento { get; set; }
    public DateTime? Data { get; set; }
    public string Status { get; set; } = "ativa";
    public bool? Presente { get; set; }
    public DateTime CriadaEmUtc { get; set; }
    public DateTime AtualizadaEmUtc { get; set; }
}
