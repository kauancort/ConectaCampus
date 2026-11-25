using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using AprendizagemColaborativa.Api.Data;
using AprendizagemColaborativa.Api.Features.Auth;
using AprendizagemColaborativa.Api.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
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
            Assert.True(teacher.PodeMinistrar);
            Assert.False(student.Professor);
            Assert.False(student.PodeMinistrar);
            Assert.Equal(TiposConta.Professor, teacher.TipoConta);
            Assert.Equal(TiposConta.Aluno, student.TipoConta);
            Assert.True(await users.CheckPasswordAsync(teacher, DemoAccountSeeder.Password));
            Assert.True(await users.CheckPasswordAsync(student, DemoAccountSeeder.Password));
            using var studentLogin = await client.PostAsJsonAsync("/api/auth/login", new { email = DemoAccountSeeder.StudentEmail, password = DemoAccountSeeder.Password });
            Assert.Equal(HttpStatusCode.OK, studentLogin.StatusCode);
            Assert.False((await studentLogin.Content.ReadFromJsonAsync<LoginResponse>())!.Student.Professor);
            using var login = await client.PostAsJsonAsync("/api/auth/login", new { email = DemoAccountSeeder.TeacherEmail, password = DemoAccountSeeder.Password });
            Assert.Equal(HttpStatusCode.OK, login.StatusCode);
            Assert.True((await login.Content.ReadFromJsonAsync<LoginResponse>())!.Student.Professor);
            teacher.PodeMinistrar = false;
            Assert.True((await users.UpdateAsync(teacher)).Succeeded);
            await DemoAccountSeeder.SeedAsync(users, NullLogger.Instance);
            var updatedTeacher = (await users.FindByEmailAsync(DemoAccountSeeder.TeacherEmail))!;
            Assert.Equal(teacher.Id, updatedTeacher.Id);
            Assert.True(updatedTeacher.PodeMinistrar);
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

    [Theory]
    [InlineData(false, DemoAccountSeeder.StudentEmail, "Mariana Costa")]
    [InlineData(true, DemoAccountSeeder.TeacherEmail, "Ricardo Almeida")]
    public async Task Seed_atualiza_senha_padrao_antiga_sem_duplicar_conta(bool professor, string email, string name)
    {
        await using var scope = factory.Services.CreateAsyncScope();
        var users = scope.ServiceProvider.GetRequiredService<UserManager<Aluno>>();
        var account = new Aluno { UserName = email, Email = email, Nome = name, Curso = "Desenvolvimento de Software Multiplataforma", Professor = professor };
        Assert.True((await users.CreateAsync(account, "Campus@123")).Succeeded);
        try
        {
            await DemoAccountSeeder.SeedAsync(users, NullLogger.Instance);
            var updated = (await users.FindByEmailAsync(email))!;
            Assert.Equal(account.Id, updated.Id);
            Assert.Equal(professor, updated.Professor);
            Assert.Equal(professor, updated.PodeMinistrar);
            Assert.Equal(professor ? TiposConta.Professor : TiposConta.Aluno, updated.TipoConta);
            Assert.True(await users.CheckPasswordAsync(updated, "Campus123@"));
            Assert.False(await users.CheckPasswordAsync(updated, "Campus@123"));
        }
        finally
        {
            foreach (var initialEmail in new[] { DemoAccountSeeder.StudentEmail, DemoAccountSeeder.TeacherEmail })
            {
                var initial = await users.FindByEmailAsync(initialEmail);
                if (initial is not null) Assert.True((await users.DeleteAsync(initial)).Succeeded);
            }
        }
    }

    [Fact]
    public async Task Producao_cria_duas_contas_sem_dados_ficticios_e_login_respeita_permissoes()
    {
        await using var scope = factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var users = scope.ServiceProvider.GetRequiredService<UserManager<Aluno>>();
        var userCount = await db.Users.CountAsync();
        var activityCount = await db.Atividades.CountAsync();
        var proposalCount = await db.PropostasAula.CountAsync();
        using var production = factory.WithWebHostBuilder(builder => builder
            .UseEnvironment("Production")
            .ConfigureAppConfiguration((_, config) => config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["DEMO_ACCOUNTS_ENABLED"] = "false"
            })));
        try
        {
            using var client = production.CreateClient();
            Assert.Equal(HttpStatusCode.OK, (await client.GetAsync("/health")).StatusCode);
            Assert.Equal(userCount + 2, await db.Users.CountAsync());
            Assert.Equal(activityCount, await db.Atividades.CountAsync());
            Assert.Equal(proposalCount, await db.PropostasAula.CountAsync());
            foreach (var professor in new[] { false, true })
            {
                var email = professor ? DemoAccountSeeder.TeacherEmail : DemoAccountSeeder.StudentEmail;
                using var login = await client.PostAsJsonAsync("/api/auth/login", new { email, password = "Campus123@" });
                Assert.Equal(HttpStatusCode.OK, login.StatusCode);
                var session = (await login.Content.ReadFromJsonAsync<LoginResponse>())!;
                Assert.Equal(professor, session.Student.Professor);
                Assert.Equal(professor, session.Student.PodeMinistrar);
                Assert.Equal(professor ? TiposConta.Professor : TiposConta.Aluno, session.Student.AccountType);
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", session.AccessToken);
                Assert.Equal(professor ? HttpStatusCode.OK : HttpStatusCode.Forbidden, (await client.GetAsync("/api/atividades/minhas")).StatusCode);
            }
        }
        finally
        {
            foreach (var email in new[] { DemoAccountSeeder.StudentEmail, DemoAccountSeeder.TeacherEmail })
            {
                var account = await users.FindByEmailAsync(email);
                if (account is not null) Assert.True((await users.DeleteAsync(account)).Succeeded);
            }
        }
    }
}
