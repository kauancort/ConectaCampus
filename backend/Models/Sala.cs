namespace AprendizagemColaborativa.Api.Models;

public sealed class Sala
{
    public Guid Id { get; set; }
    public string Nome { get; set; } = string.Empty;
    public string Tipo { get; set; } = string.Empty;
    public int Capacidade { get; set; }
    public string Status { get; set; } = string.Empty;
    public ICollection<Assento> Assentos { get; set; } = new List<Assento>();
    public ICollection<Atividade> Atividades { get; set; } = new List<Atividade>();
}
