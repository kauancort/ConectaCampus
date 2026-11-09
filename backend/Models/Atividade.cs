namespace AprendizagemColaborativa.Api.Models;

public sealed class Atividade
{
    public Guid Id { get; set; }
    public string OrganizadorId { get; set; } = string.Empty;
    public Aluno Organizador { get; set; } = null!;
    public string Tipo { get; set; } = string.Empty;
    public string Tema { get; set; } = string.Empty;
    public string Descricao { get; set; } = string.Empty;
    public string Nivel { get; set; } = string.Empty;
    public DateTime DataHoraUtc { get; set; }
    public string Local { get; set; } = string.Empty;
    public int NumeroDeVagas { get; set; }
    public string Status { get; set; } = "publicada";
}
