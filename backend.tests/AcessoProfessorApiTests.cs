using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using AprendizagemColaborativa.Api.Data;
using AprendizagemColaborativa.Api.Features.Auth;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace AprendizagemColaborativa.Api.Tests;

public sealed class AcessoProfessorApiTests(WebApplicationFactory<Program> factory) : IClassFixture<WebApplicationFactory<Program>>
{
    [Fact]
    public async Task Solicitacao_exige_professor_e_aprovacao_atualiza_permissao_e_notifica()
    {
        using var publicClient = factory.CreateClient();
        var ids = new List<string>();
        async Task<HttpClient> Conta()
        {
            var email = $"acesso-{Guid.NewGuid():N}@example.test";
            const string senha = "Fatec!Teste2026";
            using var registro = await publicClient.PostAsJsonAsync("/api/auth/register", new { name = "Pessoa Teste", email, course = "DSM", password = senha });
            Assert.Equal(HttpStatusCode.Created, registro.StatusCode);
            ids.Add((await registro.Content.ReadFromJsonAsync<StudentResponse>())!.Id);
            using var login = await publicClient.PostAsJsonAsync("/api/auth/login", new { email, password = senha });
            var token = (await login.Content.ReadFromJsonAsync<LoginResponse>())!.AccessToken;
            var client = factory.CreateClient(); client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token); return client;
        }
        using var aluno = await Conta();
        using var professor = await Conta();
        using var recusado = await Conta();
        try
        {
            await using (var scope = factory.Services.CreateAsyncScope())
            {
                var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                var user = (await db.Users.FindAsync(ids[1]))!; user.Professor = true; await db.SaveChangesAsync();
            }
            Assert.Equal(HttpStatusCode.Forbidden, (await aluno.GetAsync("/api/acesso-professor")).StatusCode);
            Assert.Equal(HttpStatusCode.Forbidden, (await aluno.GetAsync("/api/atividades/minhas")).StatusCode);
            Assert.Equal(HttpStatusCode.OK, (await professor.GetAsync("/api/atividades/minhas")).StatusCode);
            Assert.Equal(HttpStatusCode.Unauthorized, (await publicClient.GetAsync("/api/professor/alunos")).StatusCode);
            Assert.Equal(HttpStatusCode.Forbidden, (await aluno.GetAsync("/api/professor/alunos")).StatusCode);
            Assert.Equal(HttpStatusCode.Forbidden, (await aluno.GetAsync($"/api/professor/alunos/{ids[1]}")).StatusCode);
            Assert.Equal(HttpStatusCode.NotFound, (await professor.GetAsync("/api/professor/alunos/inexistente")).StatusCode);
            Assert.Equal(HttpStatusCode.Forbidden, (await aluno.PostAsJsonAsync("/api/atividades", new { })).StatusCode);
            Assert.Equal(HttpStatusCode.Unauthorized, (await publicClient.GetAsync("/api/notificacoes")).StatusCode);
            Assert.Equal(HttpStatusCode.BadRequest, (await aluno.PostAsJsonAsync("/api/acesso-professor", new { justificativa = " " })).StatusCode);
            using var request = await aluno.PostAsJsonAsync("/api/acesso-professor", new { justificativa = "Compartilhar conhecimento" });
            Assert.Equal(HttpStatusCode.Created, request.StatusCode);
            using var document = JsonDocument.Parse(await request.Content.ReadAsStringAsync());
            var id = document.RootElement.GetProperty("id").GetGuid();
            using var professorNotices = JsonDocument.Parse(await professor.GetStringAsync("/api/notificacoes"));
            var notification = Assert.Single(professorNotices.RootElement.EnumerateArray());
            var notificationId = notification.GetProperty("id").GetGuid();
            Assert.False(notification.GetProperty("lida").GetBoolean());
            Assert.Equal(HttpStatusCode.NotFound, (await aluno.PostAsync($"/api/notificacoes/{notificationId}/lida", null)).StatusCode);
            Assert.Equal(HttpStatusCode.NoContent, (await professor.PostAsync($"/api/notificacoes/{notificationId}/lida", null)).StatusCode);
            using var readNotices = JsonDocument.Parse(await professor.GetStringAsync("/api/notificacoes"));
            Assert.True(Assert.Single(readNotices.RootElement.EnumerateArray()).GetProperty("lida").GetBoolean());
            Assert.Equal(HttpStatusCode.Conflict, (await aluno.PostAsJsonAsync("/api/acesso-professor", new { justificativa = "Outro pedido" })).StatusCode);
            Assert.Equal(HttpStatusCode.Forbidden, (await aluno.PostAsJsonAsync($"/api/acesso-professor/{id}/avaliar", new { decisao = "aprovada" })).StatusCode);
            Assert.Equal(HttpStatusCode.BadRequest, (await professor.PostAsJsonAsync($"/api/acesso-professor/{id}/avaliar", new { decisao = "invalida" })).StatusCode);
            Assert.Equal(HttpStatusCode.OK, (await professor.PostAsJsonAsync($"/api/acesso-professor/{id}/avaliar", new { decisao = "aprovada" })).StatusCode);
            Assert.Equal(HttpStatusCode.Conflict, (await professor.PostAsJsonAsync($"/api/acesso-professor/{id}/avaliar", new { decisao = "aprovada" })).StatusCode);
            Assert.Equal(HttpStatusCode.Forbidden, (await aluno.GetAsync("/api/acesso-professor")).StatusCode);
            var profile = await aluno.GetFromJsonAsync<StudentResponse>("/api/auth/me");
            Assert.False(profile!.Professor);
            Assert.True(profile.PodeMinistrar);
            using var alunos = JsonDocument.Parse(await professor.GetStringAsync("/api/professor/alunos"));
            Assert.True(alunos.RootElement.EnumerateArray().Single(x => x.GetProperty("id").GetString() == ids[0]).GetProperty("podeMinistrar").GetBoolean());
            using var perfilAluno = JsonDocument.Parse(await professor.GetStringAsync($"/api/professor/alunos/{ids[0]}"));
            Assert.True(perfilAluno.RootElement.GetProperty("aluno").GetProperty("podeMinistrar").GetBoolean());
            Assert.Equal(JsonValueKind.Array, perfilAluno.RootElement.GetProperty("inscricoes").ValueKind);
            Assert.Equal(JsonValueKind.Array, perfilAluno.RootElement.GetProperty("ministradas").ValueKind);
            using var notices = JsonDocument.Parse(await aluno.GetStringAsync("/api/notificacoes"));
            Assert.Single(notices.RootElement.EnumerateArray());

            using var secondRequest = await recusado.PostAsJsonAsync("/api/acesso-professor", new { justificativa = "Gostaria de ministrar." });
            Assert.Equal(HttpStatusCode.Created, secondRequest.StatusCode);
            using var secondDocument = JsonDocument.Parse(await secondRequest.Content.ReadAsStringAsync());
            var secondId = secondDocument.RootElement.GetProperty("id").GetGuid();
            Assert.Equal(HttpStatusCode.OK, (await professor.PostAsJsonAsync($"/api/acesso-professor/{secondId}/avaliar", new { decisao = "recusada" })).StatusCode);
            Assert.False((await recusado.GetFromJsonAsync<StudentResponse>("/api/auth/me"))!.Professor);
            Assert.Equal(HttpStatusCode.Forbidden, (await recusado.GetAsync("/api/acesso-professor")).StatusCode);
            using var rejectionNotices = JsonDocument.Parse(await recusado.GetStringAsync("/api/notificacoes"));
            Assert.Contains("recusada", Assert.Single(rejectionNotices.RootElement.EnumerateArray()).GetProperty("mensagem").GetString());
            Assert.Equal(HttpStatusCode.Created, (await recusado.PostAsJsonAsync("/api/acesso-professor", new { justificativa = "Nova solicitação." })).StatusCode);
        }
        finally
        {
            await using var scope = factory.Services.CreateAsyncScope();
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            db.SolicitacoesProfessor.RemoveRange(await db.SolicitacoesProfessor.Where(x => ids.Contains(x.AlunoId)).ToListAsync());
            db.Notificacoes.RemoveRange(await db.Notificacoes.Where(x => ids.Contains(x.UsuarioId)).ToListAsync());
            await db.SaveChangesAsync();
            db.Users.RemoveRange(await db.Users.Where(x => ids.Contains(x.Id)).ToListAsync()); await db.SaveChangesAsync();
        }
    }
}
