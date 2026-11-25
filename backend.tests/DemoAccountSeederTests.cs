using System.Net;
using System.Net.Http.Json;
using AprendizagemColaborativa.Api.Features.Auth;
using AprendizagemColaborativa.Api.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace AprendizagemColaborativa.Api.Tests;

public sealed class DemoAccountSeederTests(WebApplicationFactory<Program> factory) : IClassFixture<WebApplicationFactory<Program>>
{
    [Fact]
    public async Task Seed_cria_ambos_os_perfis_e_preserva_contas_ao_reexecutar()
    {
        using var client = factory.CreateClient();
        await using var scope = factory.Services.CreateAsyncScope();
        var users = scope.ServiceProvider.GetRequiredService<UserManager<Aluno>>();
        Assert.Null(await users.FindByEmailAsync(DemoAccountSeeder.StudentEmail));
        Assert.Null(await users.FindByEmailAsync(DemoAccountSeeder.TeacherEmail));
        Aluno? student = null;
        try
        {
            await DemoAccountSeeder.SeedAsync(users, NullLogger.Instance);
            student = (await users.FindByEmailAsync(DemoAccountSeeder.StudentEmail))!;
            var teacher = (await users.FindByEmailAsync(DemoAccountSeeder.TeacherEmail))!;
            Assert.True(teacher.Professor);
            Assert.False(student.Professor);
            Assert.True(await users.CheckPasswordAsync(teacher, DemoAccountSeeder.Password));
            Assert.True(await users.CheckPasswordAsync(student, DemoAccountSeeder.Password));
            using var studentLogin = await client.PostAsJsonAsync("/api/auth/login", new { email = DemoAccountSeeder.StudentEmail, password = DemoAccountSeeder.Password });
            Assert.Equal(HttpStatusCode.OK, studentLogin.StatusCode);
            Assert.False((await studentLogin.Content.ReadFromJsonAsync<LoginResponse>())!.Student.Professor);
            using var login = await client.PostAsJsonAsync("/api/auth/login", new { email = DemoAccountSeeder.TeacherEmail, password = DemoAccountSeeder.Password });
            Assert.Equal(HttpStatusCode.OK, login.StatusCode);
            Assert.True((await login.Content.ReadFromJsonAsync<LoginResponse>())!.Student.Professor);
            teacher.Professor = false;
            Assert.True((await users.UpdateAsync(teacher)).Succeeded);
            student.Nome = "Conta existente";
            Assert.True((await users.UpdateAsync(student)).Succeeded);
            Assert.True((await users.ChangePasswordAsync(student, DemoAccountSeeder.Password, "OutraSenha@123")).Succeeded);
            await DemoAccountSeeder.SeedAsync(users, NullLogger.Instance);
            Assert.False((await users.FindByEmailAsync(DemoAccountSeeder.TeacherEmail))!.Professor);
            Assert.Equal("Conta existente", (await users.FindByEmailAsync(DemoAccountSeeder.StudentEmail))!.Nome);
            Assert.True(await users.CheckPasswordAsync(student, "OutraSenha@123"));
            Assert.False(await users.CheckPasswordAsync(student, DemoAccountSeeder.Password));
        }
        finally
        {
            var teacher = await users.FindByEmailAsync(DemoAccountSeeder.TeacherEmail);
            if (teacher is not null) await users.DeleteAsync(teacher);
            if (student is not null) await users.DeleteAsync(student);
        }
    }
}
