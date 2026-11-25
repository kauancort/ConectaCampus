using Microsoft.AspNetCore.Identity;

namespace AprendizagemColaborativa.Api.Models;

public sealed class Aluno : IdentityUser
{
    public string Nome { get; set; } = string.Empty;
    public string Curso { get; set; } = string.Empty;
<<<<<<< HEAD
    public bool Professor { get; set; }
    public bool PodeMinistrar { get; set; }
=======
    public string TipoConta { get; set; } = TiposConta.Aluno;
}

public static class TiposConta
{
    public const string Aluno = "aluno";
    public const string Professor = "professor";
>>>>>>> 1524e16cfcfef7673dca4642fc242c4617009a43
}
