using Microsoft.AspNetCore.Identity;

namespace AprendizagemColaborativa.Api.Models;

public sealed class Aluno : IdentityUser
{
    public string Nome { get; set; } = string.Empty;
    public string Curso { get; set; } = string.Empty;
    public bool Professor { get; set; }
    public bool PodeMinistrar { get; set; }
}
