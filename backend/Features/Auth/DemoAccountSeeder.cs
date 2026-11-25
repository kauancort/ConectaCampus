using AprendizagemColaborativa.Api.Models;
using Microsoft.AspNetCore.Identity;

namespace AprendizagemColaborativa.Api.Features.Auth;

/// <summary>Contas opcionais para demonstração em ambientes locais.</summary>
public static class DemoAccountSeeder
{
    public const string Password = "Campus@123";
    public const string StudentEmail = "aluno@conectacampus.local";
    public const string TeacherEmail = "professor@conectacampus.local";

    public static async Task SeedAsync(UserManager<Aluno> users, ILogger logger)
    {
        await CreateIfMissingAsync(users, logger, StudentEmail, "Mariana Costa", false);
        await CreateIfMissingAsync(users, logger, TeacherEmail, "Ricardo Almeida", true);
    }

    private static async Task CreateIfMissingAsync(UserManager<Aluno> users, ILogger logger, string email, string name, bool professor)
    {
        if (await users.FindByEmailAsync(email) is not null)
        {
            logger.LogInformation("Conta de demonstração {Email} já existe; nenhum dado foi alterado.", email);
            return;
        }

        var account = new Aluno { UserName = email, Email = email, Nome = name, Curso = "Desenvolvimento de Software Multiplataforma", Professor = professor };
        var result = await users.CreateAsync(account, Password);
        if (!result.Succeeded)
        {
            throw new InvalidOperationException($"Não foi possível criar a conta de demonstração {email}: {string.Join(", ", result.Errors.Select(error => error.Code))}.");
        }
        logger.LogInformation("Conta de demonstração {Email} criada.", email);
    }
}
