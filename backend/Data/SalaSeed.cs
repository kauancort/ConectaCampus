using AprendizagemColaborativa.Api.Models;

namespace AprendizagemColaborativa.Api.Data;

public static class SalaSeed
{
    public static readonly Guid SalaDesktopId = new("0e9abb4a-5aa5-4e6d-b94a-774a5fad4f10");
    public static readonly Guid SalaNotebookId = new("70cb6f07-08e2-4664-8d45-334564cdd768");

    public static readonly Sala[] Salas =
    [
        new() { Id = SalaDesktopId, Nome = "Laboratório Desktop", Tipo = "DESKTOP", Capacidade = 24, Status = "disponivel" },
        new() { Id = SalaNotebookId, Nome = "Laboratório Notebook", Tipo = "NOTEBOOK", Capacidade = 20, Status = "disponivel" }
    ];

    public static readonly Assento[] Assentos =
    [
        .. CreateAssentos(SalaDesktopId, 6),
        .. CreateAssentos(SalaNotebookId, 5)
    ];

    private static IEnumerable<Assento> CreateAssentos(Guid salaId, int colunas)
    {
        for (var fileira = 'A'; fileira <= 'D'; fileira++)
        {
            for (var coluna = 1; coluna <= colunas; coluna++)
            {
                var codigo = $"{fileira}{coluna}";
                yield return new Assento
                {
                    Id = DeterministicGuid($"{salaId:N}:{codigo}"),
                    SalaId = salaId,
                    Codigo = codigo,
                    Fileira = fileira.ToString(),
                    Coluna = coluna
                };
            }
        }
    }

    private static Guid DeterministicGuid(string value)
    {
        var bytes = System.Security.Cryptography.MD5.HashData(System.Text.Encoding.UTF8.GetBytes(value));
        return new Guid(bytes);
    }
}
