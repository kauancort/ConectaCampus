namespace AprendizagemColaborativa.Api.Models;

public sealed class Inscricao
{
    public Guid Id { get; set; }
    public Guid AtividadeId { get; set; }
    public Atividade Atividade { get; set; } = null!;
    public string AlunoId { get; set; } = string.Empty;
    public Aluno Aluno { get; set; } = null!;
    public string Status { get; set; } = "ativa";
    public DateTime CriadaEmUtc { get; set; }
    public DateTime AtualizadaEmUtc { get; set; }
}
