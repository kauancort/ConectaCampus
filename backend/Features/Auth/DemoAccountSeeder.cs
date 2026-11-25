using AprendizagemColaborativa.Api.Models;
using Microsoft.AspNetCore.Identity;

namespace AprendizagemColaborativa.Api.Features.Auth;

/// <summary>Contas iniciais de aluno e professor, sem dados fictícios de atividades.</summary>
public static class DemoAccountSeeder
{
    public const string Password = "Campus123@";
    private const string LegacyPassword = "Campus@123";
    public const string StudentEmail = "aluno@conectacampus.local";
    public const string TeacherEmail = "professor@conectacampus.local";

    public static async Task SeedAsync(UserManager<Aluno> users, ILogger logger)
    {
        await CreateIfMissingAsync(users, logger, StudentEmail, "Mariana Costa", false);
        await CreateIfMissingAsync(users, logger, TeacherEmail, "Ricardo Almeida", true);
    }

    private static async Task CreateIfMissingAsync(UserManager<Aluno> users, ILogger logger, string email, string name, bool professor)
    {
        if (await users.FindByEmailAsync(email) is { } existing)
        {
            // Atualiza somente a senha padrão antiga de uma conta inicial intacta.
            // Senhas e permissões alteradas pelo usuário são preservadas.
            if (existing.Nome == name && existing.Professor == professor &&
                existing.Curso == "Desenvolvimento de Software Multiplataforma" &&
                await users.CheckPasswordAsync(existing, LegacyPassword))
            {
                var resetToken = await users.GeneratePasswordResetTokenAsync(existing);
                var reset = await users.ResetPasswordAsync(existing, resetToken, Password);
                if (!reset.Succeeded)
                    throw new InvalidOperationException($"Não foi possível atualizar a senha inicial de {email}: {string.Join(", ", reset.Errors.Select(error => error.Code))}.");
                existing.TipoConta = professor ? TiposConta.Professor : TiposConta.Aluno;
                var update = await users.UpdateAsync(existing);
                if (!update.Succeeded)
                    throw new InvalidOperationException($"Não foi possível atualizar o perfil inicial de {email}: {string.Join(", ", update.Errors.Select(error => error.Code))}.");
                logger.LogInformation("Senha padrão antiga da conta inicial {Email} atualizada.", email);
                return;
            }
            logger.LogInformation("Conta de demonstração {Email} já existe; nenhum dado foi alterado.", email);
            return;
        }

        var account = new Aluno { UserName = email, Email = email, Nome = name, Curso = "Desenvolvimento de Software Multiplataforma", Professor = professor, TipoConta = professor ? TiposConta.Professor : TiposConta.Aluno };
        var result = await users.CreateAsync(account, Password);
        if (!result.Succeeded)
        {
            throw new InvalidOperationException($"Não foi possível criar a conta de demonstração {email}: {string.Join(", ", result.Errors.Select(error => error.Code))}.");
        }
        logger.LogInformation("Conta de demonstração {Email} criada.", email);
    }
}
