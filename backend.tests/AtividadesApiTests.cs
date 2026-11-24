using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using AprendizagemColaborativa.Api.Data;
using AprendizagemColaborativa.Api.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace AprendizagemColaborativa.Api.Tests;

[Collection(ApiIntegrationCollection.Name)]
public sealed class AtividadesApiTests(WebApplicationFactory<Program> factory)
{
    [Fact]
    public async Task Criar_atividade_retorna_sala_e_aparece_nas_listagens()
    {
        var authenticated = await CreateAuthenticatedClientAsync();
        try
        {
            var response = await authenticated.Client.PostAsJsonAsync("/api/atividades", CreateRequest("DESKTOP"));
            Assert.Equal(HttpStatusCode.Created, response.StatusCode);

            using var created = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
            Assert.Equal("AULA", created.RootElement.GetProperty("modalidade").GetString());
            Assert.Equal("DESKTOP", created.RootElement.GetProperty("sala").GetProperty("tipo").GetString());
            Assert.Equal(24, created.RootElement.GetProperty("sala").GetProperty("capacidade").GetInt32());
            Assert.True(created.RootElement.TryGetProperty("data", out _));
            Assert.True(created.RootElement.TryGetProperty("inicio", out _));
            Assert.True(created.RootElement.TryGetProperty("fim", out _));

            using var filtered = await authenticated.Client.GetAsync("/api/atividades?tema=Teste%20de%20hor%C3%A1rio");
            Assert.Equal(HttpStatusCode.OK, filtered.StatusCode);
            using var filteredJson = JsonDocument.Parse(await filtered.Content.ReadAsStringAsync());
            Assert.NotEmpty(filteredJson.RootElement.EnumerateArray());

            using var mine = await authenticated.Client.GetAsync("/api/atividades/minhas");
            Assert.Equal(HttpStatusCode.OK, mine.StatusCode);
            using var mineJson = JsonDocument.Parse(await mine.Content.ReadAsStringAsync());
            Assert.NotEmpty(mineJson.RootElement.EnumerateArray());
        }
        finally
        {
            await DeleteStudentAsync(authenticated.Email);
            authenticated.Client.Dispose();
        }
    }

    [Fact]
    public async Task Criar_atividade_com_termino_igual_ao_inicio_retorna_bad_request()
    {
        var authenticated = await CreateAuthenticatedClientAsync();
        try
        {
            var inicio = DateTimeOffset.UtcNow.AddDays(45).ToString("O");
            var request = new
            {
                tema = "Teste de horário inválido",
                descricao = "Validação do intervalo de reserva.",
                modalidade = "AULA",
                tipoSala = "DESKTOP",
                inicio,
                fim = inicio
            };

            using var response = await authenticated.Client.PostAsJsonAsync("/api/atividades", request);
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }
        finally
        {
            await DeleteStudentAsync(authenticated.Email);
            authenticated.Client.Dispose();
        }
    }

    [Fact]
    public async Task Criar_atividades_no_mesmo_horario_retorna_conflito()
    {
        var authenticated = await CreateAuthenticatedClientAsync();
        try
        {
            var request = CreateRequest("NOTEBOOK");
            using var first = await authenticated.Client.PostAsJsonAsync("/api/atividades", request);
            using var second = await authenticated.Client.PostAsJsonAsync("/api/atividades", request);

            Assert.Equal(HttpStatusCode.Created, first.StatusCode);
            Assert.Equal(HttpStatusCode.Conflict, second.StatusCode);
        }
        finally
        {
            await DeleteStudentAsync(authenticated.Email);
            authenticated.Client.Dispose();
        }
    }

    [Fact]
    public async Task Criar_atividades_simultaneas_no_mesmo_horario_cria_apenas_uma()
    {
        var authenticated = await CreateAuthenticatedClientAsync();
        try
        {
            var request = CreateRequest("DESKTOP");
            var responses = await Task.WhenAll(
                authenticated.Client.PostAsJsonAsync("/api/atividades", request),
                authenticated.Client.PostAsJsonAsync("/api/atividades", request));
            try
            {
                Assert.Equal(1, responses.Count(response => response.StatusCode == HttpStatusCode.Created));
                Assert.Equal(1, responses.Count(response => response.StatusCode == HttpStatusCode.Conflict));
            }
            finally
            {
                foreach (var response in responses)
                {
                    response.Dispose();
                }
            }
        }
        finally
        {
            await DeleteStudentAsync(authenticated.Email);
            authenticated.Client.Dispose();
        }
    }

    [Theory]
    [InlineData("teste de horário")]
    [InlineData("%_\\")]
    public async Task Busca_por_tema_ignora_caixa_e_trata_curingas_como_texto(string filtro)
    {
        var authenticated = await CreateAuthenticatedClientAsync();
        try
        {
            using var response = await authenticated.Client.PostAsJsonAsync("/api/atividades",
                CreateRequest("DESKTOP", "Teste de horário %_\\"));
            Assert.Equal(HttpStatusCode.Created, response.StatusCode);
            using var created = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
            var id = created.RootElement.GetProperty("id").GetGuid();

            using var filtered = await authenticated.Client.GetAsync($"/api/atividades?tema={Uri.EscapeDataString(filtro)}");
            Assert.Equal(HttpStatusCode.OK, filtered.StatusCode);
            using var result = JsonDocument.Parse(await filtered.Content.ReadAsStringAsync());
            Assert.Contains(result.RootElement.EnumerateArray(), item => item.GetProperty("id").GetGuid() == id);
            Assert.All(result.RootElement.EnumerateArray(), item =>
                Assert.Contains(filtro, item.GetProperty("tema").GetString()!, StringComparison.OrdinalIgnoreCase));
        }
        finally
        {
            await DeleteStudentAsync(authenticated.Email);
            authenticated.Client.Dispose();
        }
    }

    private static object CreateRequest(string tipoSala, string tema = "Teste de horário")
    {
        var inicio = DateTimeOffset.UtcNow.AddDays(60).AddMinutes(Random.Shared.Next(1, 10_000));
        return new
        {
            tema,
            descricao = "Atividade usada na validação de reserva de sala.",
            modalidade = "AULA",
            tipoSala,
            inicio = inicio.ToString("O"),
            fim = inicio.AddHours(2).ToString("O")
        };
    }

    private async Task<(HttpClient Client, string Email)> CreateAuthenticatedClientAsync()
    {
        var client = factory.CreateClient();
        var email = $"activity-{Guid.NewGuid():N}@example.test";
        var registration = new
        {
            name = "Leonardo Responsável",
            email,
            course = "Desenvolvimento de Software Multiplataforma",
            accountType = "professor",
            password = "Fatec!Teste2026"
        };

        using var register = await client.PostAsJsonAsync("/api/auth/register", registration);
        Assert.Equal(HttpStatusCode.Created, register.StatusCode);
        using var login = await client.PostAsJsonAsync("/api/auth/login", new { email, password = registration.password });
        Assert.Equal(HttpStatusCode.OK, login.StatusCode);
        using var json = JsonDocument.Parse(await login.Content.ReadAsStringAsync());
        var token = json.RootElement.GetProperty("accessToken").GetString();
        Assert.False(string.IsNullOrWhiteSpace(token));
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return (client, email);
    }

    private async Task DeleteStudentAsync(string email)
    {
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var users = scope.ServiceProvider.GetRequiredService<UserManager<Aluno>>();
        var student = await users.FindByEmailAsync(email);
        if (student is null)
        {
            return;
        }

        var activities = await db.Atividades
            .Where(atividade => atividade.ResponsavelId == student.Id || atividade.OrganizadorId == student.Id)
            .ToListAsync();
        db.Atividades.RemoveRange(activities);
        await db.SaveChangesAsync();
        var result = await users.DeleteAsync(student);
        Assert.True(result.Succeeded, string.Join("; ", result.Errors.Select(error => error.Code)));
    }
}
