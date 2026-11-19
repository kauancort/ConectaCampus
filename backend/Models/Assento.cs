namespace AprendizagemColaborativa.Api.Models;

public sealed class Assento
{
    public Guid Id { get; set; }
    public Guid SalaId { get; set; }
    public Sala Sala { get; set; } = null!;
    public string Codigo { get; set; } = string.Empty;
    public string Fileira { get; set; } = string.Empty;
    public int Coluna { get; set; }
    public ICollection<Inscricao> Inscricoes { get; set; } = new List<Inscricao>();
}
