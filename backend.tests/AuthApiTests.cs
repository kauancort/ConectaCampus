using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.IdentityModel.Tokens.Jwt;
using System.Text.Json;
using System.Text.Json.Nodes;
using AprendizagemColaborativa.Api.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using Xunit;

namespace AprendizagemColaborativa.Api.Tests;

[Collection(ApiIntegrationCollection.Name)]
public sealed class AuthApiTests(WebApplicationFactory<Program> factory)
{
    private readonly HttpClient _client = factory.CreateClient();

    [Fact]
    public async Task Register_valid_student_returns_public_profile_only()
    {
        var email = NewEmail();
        try
        {
            var response = await _client.PostAsJsonAsync("/api/auth/register", ValidRegistration(email));
            Assert.Equal(HttpStatusCode.Created, response.StatusCode);
            var body = await response.Content.ReadAsStringAsync();
            Assert.DoesNotContain("password", body, StringComparison.OrdinalIgnoreCase);
            using var json = JsonDocument.Parse(body);
            Assert.Equal(email, json.RootElement.GetProperty("email").GetString());
            Assert.Equal("Ada Aluna", json.RootElement.GetProperty("name").GetString());
            Assert.Equal("aluno", json.RootElement.GetProperty("accountType").GetString());
        }
        finally
        {
            await DeleteStudentAsync(email);
        }
    }

    [Fact]
    public async Task Register_missing_fields_returns_bad_request()
    {
        using var response = await _client.PostAsJsonAsync("/api/auth/register", new { });
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        using var invalidEmail = await _client.PostAsJsonAsync("/api/auth/register", new
        {
            name = "Ada Aluna",
            email = "not-an-email",
            course = "DSM",
            password = TestPassword
        });
        Assert.Equal(HttpStatusCode.BadRequest, invalidEmail.StatusCode);
    }

    [Fact]
    public async Task Register_duplicate_email_returns_conflict()
    {
        var email = NewEmail();
        try
        {
            var first = await _client.PostAsJsonAsync("/api/auth/register", ValidRegistration(email));
            Assert.Equal(HttpStatusCode.Created, first.StatusCode);

            using var duplicate = await _client.PostAsJsonAsync("/api/auth/register", ValidRegistration(email));
            Assert.Equal(HttpStatusCode.Conflict, duplicate.StatusCode);
        }
        finally
        {
            await DeleteStudentAsync(email);
        }
    }

    [Fact]
    public async Task Login_valid_credentials_issue_token_and_invalid_credentials_are_unauthorized()
    {
        var email = NewEmail();
        try
        {
            var registration = await _client.PostAsJsonAsync("/api/auth/register", ValidRegistration(email));
            Assert.Equal(HttpStatusCode.Created, registration.StatusCode);

            using var invalid = await _client.PostAsJsonAsync("/api/auth/login", new { email, password = "Wrong!Password2026" });
            Assert.Equal(HttpStatusCode.Unauthorized, invalid.StatusCode);

            using var login = await _client.PostAsJsonAsync("/api/auth/login", new { email, password = TestPassword });
            Assert.Equal(HttpStatusCode.OK, login.StatusCode);
            using var json = JsonDocument.Parse(await login.Content.ReadAsStringAsync());
            var token = json.RootElement.GetProperty("accessToken").GetString();
            Assert.False(string.IsNullOrWhiteSpace(token));
            Assert.Equal("Bearer", json.RootElement.GetProperty("tokenType").GetString());

            _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
            using var me = await _client.GetAsync("/api/auth/me");
            Assert.Equal(HttpStatusCode.OK, me.StatusCode);
            using var profile = JsonDocument.Parse(await me.Content.ReadAsStringAsync());
            Assert.Equal(email, profile.RootElement.GetProperty("email").GetString());
        }
        finally
        {
            _client.DefaultRequestHeaders.Authorization = null;
            await DeleteStudentAsync(email);
        }
    }

    [Fact]
    public async Task Atualizar_perfil_exige_sessao_valida_campos_e_preserva_permissoes()
    {
        var email = NewEmail();
        try
        {
            Assert.Equal(HttpStatusCode.Unauthorized, (await _client.PutAsJsonAsync("/api/auth/me", new { name = "Novo", course = "DSM" })).StatusCode);
            Assert.Equal(HttpStatusCode.Created, (await _client.PostAsJsonAsync("/api/auth/register", ValidRegistration(email))).StatusCode);
            using var login = await _client.PostAsJsonAsync("/api/auth/login", new { email, password = TestPassword });
            using var json = JsonDocument.Parse(await login.Content.ReadAsStringAsync());
            _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", json.RootElement.GetProperty("accessToken").GetString());
            Assert.Equal(HttpStatusCode.BadRequest, (await _client.PutAsJsonAsync("/api/auth/me", new { name = " ", course = "DSM" })).StatusCode);
            var response = await _client.PutAsJsonAsync("/api/auth/me", new { name = "  Ada Souza  ", course = " Gestão ", email = "outra@example.test", professor = true, podeMinistrar = true });
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            using var updated = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
            Assert.Equal("Ada Souza", updated.RootElement.GetProperty("name").GetString());
            Assert.Equal("Gestão", updated.RootElement.GetProperty("course").GetString());
            Assert.Equal(email, updated.RootElement.GetProperty("email").GetString());
            Assert.False(updated.RootElement.GetProperty("professor").GetBoolean());
            Assert.False(updated.RootElement.GetProperty("podeMinistrar").GetBoolean());
            using var me = await _client.GetFromJsonAsync<JsonDocument>("/api/auth/me");
            Assert.Equal("Ada Souza", me!.RootElement.GetProperty("name").GetString());
        }
        finally { _client.DefaultRequestHeaders.Authorization = null; await DeleteStudentAsync(email); }
    }

    [Fact]
    public async Task Me_without_token_returns_unauthorized()
    {
        _client.DefaultRequestHeaders.Authorization = null;
        using var response = await _client.GetAsync("/api/auth/me");
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Theory]
    [InlineData("aluno")]
    [InlineData("professor")]
    public async Task Cadastro_e_login_preservam_tipo_de_conta_no_perfil_e_token(string accountType)
    {
        var email = NewEmail();
        try
        {
            using var register = await _client.PostAsJsonAsync("/api/auth/register", new
            {
                name = "Conta de teste", email, course = "DSM", password = TestPassword, accountType
            });
            Assert.Equal(HttpStatusCode.Created, register.StatusCode);
            using var registration = JsonDocument.Parse(await register.Content.ReadAsStringAsync());
            Assert.Equal(accountType, registration.RootElement.GetProperty("accountType").GetString());

            using var login = await _client.PostAsJsonAsync("/api/auth/login", new { email, password = TestPassword });
            Assert.Equal(HttpStatusCode.OK, login.StatusCode);
            using var body = JsonDocument.Parse(await login.Content.ReadAsStringAsync());
            Assert.Equal(accountType, body.RootElement.GetProperty("student").GetProperty("accountType").GetString());
            var token = body.RootElement.GetProperty("accessToken").GetString()!;
            Assert.Equal(accountType, new JwtSecurityTokenHandler().ReadJwtToken(token).Claims.Single(claim => claim.Type == "account_type").Value);

            _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
            using var me = await _client.GetAsync("/api/auth/me");
            Assert.Equal(HttpStatusCode.OK, me.StatusCode);
            using var profile = JsonDocument.Parse(await me.Content.ReadAsStringAsync());
            Assert.Equal(accountType, profile.RootElement.GetProperty("accountType").GetString());

            using var create = await _client.PostAsJsonAsync("/api/atividades", new { });
            Assert.Equal(accountType == "professor" ? HttpStatusCode.BadRequest : HttpStatusCode.Forbidden, create.StatusCode);
            using var activities = await _client.GetAsync("/api/atividades/minhas");
            Assert.Equal(accountType == "professor" ? HttpStatusCode.OK : HttpStatusCode.Forbidden, activities.StatusCode);
        }
        finally
        {
            _client.DefaultRequestHeaders.Authorization = null;
            await DeleteStudentAsync(email);
        }
    }

    [Theory]
    [InlineData("admin")]
    [InlineData("")]
    [InlineData("responsavel")]
    public async Task Cadastro_rejeita_tipo_de_conta_invalido(string accountType)
    {
        var email = NewEmail();
        try
        {
            using var response = await _client.PostAsJsonAsync("/api/auth/register", new
            {
                name = "Conta de teste", email, course = "DSM", password = TestPassword, accountType
            });
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
            using var scope = factory.Services.CreateScope();
            Assert.Null(await scope.ServiceProvider.GetRequiredService<UserManager<Aluno>>().FindByEmailAsync(email));
        }
        finally
        {
            await DeleteStudentAsync(email);
        }
    }

    [Theory]
    [InlineData("POST", "/api/atividades")]
    [InlineData("GET", "/api/atividades/minhas")]
    public async Task Rotas_de_professor_sem_token_retornam_401(string method, string route)
    {
        using var request = new HttpRequestMessage(new HttpMethod(method), route);
        if (method == "POST") request.Content = JsonContent.Create(new { });
        using var response = await _client.SendAsync(request);
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Login_nao_promove_aluno_com_perfil_enviado_no_corpo()
    {
        var email = NewEmail();
        try
        {
            using var register = await _client.PostAsJsonAsync("/api/auth/register", ValidRegistration(email));
            Assert.Equal(HttpStatusCode.Created, register.StatusCode);
            using var login = await _client.PostAsJsonAsync("/api/auth/login", new
            {
                email, password = TestPassword, accountType = "professor"
            });
            Assert.Equal(HttpStatusCode.OK, login.StatusCode);
            using var body = JsonDocument.Parse(await login.Content.ReadAsStringAsync());
            Assert.Equal("aluno", body.RootElement.GetProperty("student").GetProperty("accountType").GetString());
            var token = body.RootElement.GetProperty("accessToken").GetString();
            _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
            using var create = await _client.PostAsJsonAsync("/api/atividades", new { accountType = "professor" });
            Assert.Equal(HttpStatusCode.Forbidden, create.StatusCode);
        }
        finally
        {
            _client.DefaultRequestHeaders.Authorization = null;
            await DeleteStudentAsync(email);
        }
    }

    [Fact]
    public async Task Cadastro_duplicado_com_outro_perfil_nao_altera_conta_existente()
    {
        var email = NewEmail();
        try
        {
            using var register = await _client.PostAsJsonAsync("/api/auth/register", ValidRegistration(email));
            Assert.Equal(HttpStatusCode.Created, register.StatusCode);
            using var duplicate = await _client.PostAsJsonAsync("/api/auth/register", new
            {
                name = "Outra pessoa", email, course = "Outro curso", password = TestPassword, accountType = "professor"
            });
            Assert.Equal(HttpStatusCode.Conflict, duplicate.StatusCode);
            using var scope = factory.Services.CreateScope();
            var student = await scope.ServiceProvider.GetRequiredService<UserManager<Aluno>>().FindByEmailAsync(email);
            Assert.NotNull(student);
            Assert.Equal("aluno", student.TipoConta);
            Assert.Equal("Ada Aluna", student.Nome);
        }
        finally
        {
            await DeleteStudentAsync(email);
        }
    }

    [Fact]
    public async Task Alterar_claim_de_aluno_para_professor_invalida_token()
    {
        var email = NewEmail();
        try
        {
            using var register = await _client.PostAsJsonAsync("/api/auth/register", ValidRegistration(email));
            Assert.Equal(HttpStatusCode.Created, register.StatusCode);
            using var login = await _client.PostAsJsonAsync("/api/auth/login", new { email, password = TestPassword });
            Assert.Equal(HttpStatusCode.OK, login.StatusCode);
            using var body = JsonDocument.Parse(await login.Content.ReadAsStringAsync());
            var parts = body.RootElement.GetProperty("accessToken").GetString()!.Split('.');
            var payload = JsonNode.Parse(Base64UrlEncoder.Decode(parts[1]))!;
            payload["account_type"] = "professor";
            var forgedToken = $"{parts[0]}.{Base64UrlEncoder.Encode(payload.ToJsonString())}.{parts[2]}";
            _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", forgedToken);
            using var activities = await _client.GetAsync("/api/atividades/minhas");
            Assert.Equal(HttpStatusCode.Unauthorized, activities.StatusCode);
        }
        finally
        {
            _client.DefaultRequestHeaders.Authorization = null;
            await DeleteStudentAsync(email);
        }
    }

    private async Task DeleteStudentAsync(string email)
    {
        using var scope = factory.Services.CreateScope();
        var users = scope.ServiceProvider.GetRequiredService<UserManager<Aluno>>();
        var student = await users.FindByEmailAsync(email);
        if (student is not null)
        {
            var result = await users.DeleteAsync(student);
            Assert.True(result.Succeeded, string.Join("; ", result.Errors.Select(error => error.Code)));
        }
    }

    private static object ValidRegistration(string email) => new
    {
        name = "Ada Aluna",
        email,
        course = "Desenvolvimento de Software Multiplataforma",
        password = TestPassword
    };

    private static string NewEmail() => $"test-{Guid.NewGuid():N}@example.test";
    private const string TestPassword = "Fatec!Teste2026";
}
