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

public sealed class PropostasApiTests(WebApplicationFactory<Program> factory) : IClassFixture<WebApplicationFactory<Program>>
{
    [Fact]
    public async Task Aluno_propoe_professor_aprova_uma_vez_e_acompanha_participantes()
    {
        using var anon = factory.CreateClient();
        var users = new List<string>();
        async Task<HttpClient> Register(bool professor, bool podeMinistrar)
        {
            var email = $"proposta-{Guid.NewGuid():N}@example.test";
            const string password = "Teste@123";
            using var response = await anon.PostAsJsonAsync("/api/auth/register", new { name = email, email, course = "DSM", password });
            Assert.Equal(HttpStatusCode.Created, response.StatusCode);
            var id = (await response.Content.ReadFromJsonAsync<StudentResponse>())!.Id;
            users.Add(id);
            await using var scope = factory.Services.CreateAsyncScope();
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var user = (await db.Users.FindAsync(id))!; user.Professor = professor; user.PodeMinistrar = podeMinistrar; await db.SaveChangesAsync();
            var login = await anon.PostAsJsonAsync("/api/auth/login", new { email, password });
            var token = (await login.Content.ReadFromJsonAsync<LoginResponse>())!.AccessToken;
            var client = factory.CreateClient(); client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token); return client;
        }
        using var student = await Register(false, true);
        using var teacher = await Register(true, false);
        using var other = await Register(true, false);
        using var unapproved = await Register(false, false);
        try
        {
            var start = DateTimeOffset.UtcNow.AddDays(900 + Random.Shared.Next(100));
            var request = new { titulo = "Oficina de dados", tema = "Dados", descricao = "Exercícios práticos", tipoSala = "DESKTOP", numeroSala = "101", inicio = start.ToString("O"), fim = start.AddHours(1).ToString("O") };
            Assert.Equal(HttpStatusCode.Forbidden, (await teacher.PostAsJsonAsync("/api/propostas", request)).StatusCode);
            Assert.Equal(HttpStatusCode.Forbidden, (await unapproved.PostAsJsonAsync("/api/propostas", request)).StatusCode);
            Assert.Equal(HttpStatusCode.Forbidden, (await student.PostAsJsonAsync("/api/atividades", request)).StatusCode);
            using var invalid = await student.PostAsJsonAsync("/api/propostas", new { titulo = request.titulo, tema = request.tema, descricao = request.descricao, tipoSala = request.tipoSala, numeroSala = "A101", inicio = request.inicio, fim = request.fim });
            Assert.Equal(HttpStatusCode.BadRequest, invalid.StatusCode);
            using var created = await student.PostAsJsonAsync("/api/propostas", request);
            Assert.Equal(HttpStatusCode.Created, created.StatusCode);
            using var json = JsonDocument.Parse(await created.Content.ReadAsStringAsync());
            var id = json.RootElement.GetProperty("id").GetGuid();
            using var catalogBefore = JsonDocument.Parse(await student.GetStringAsync("/api/atividades?tema=Dados"));
            Assert.DoesNotContain(catalogBefore.RootElement.EnumerateArray(), x => x.GetProperty("titulo").GetString() == "Oficina de dados");
            Assert.Equal(HttpStatusCode.Forbidden, (await student.GetAsync("/api/propostas/pendentes")).StatusCode);
            using var queue = JsonDocument.Parse(await teacher.GetStringAsync("/api/propostas/pendentes"));
            Assert.Contains(queue.RootElement.EnumerateArray(), x => x.GetProperty("id").GetGuid() == id && x.GetProperty("numeroSala").GetString() == "101");
            var evaluations = await Task.WhenAll(teacher.PostAsJsonAsync($"/api/propostas/{id}/avaliar", new { decisao = "aprovada" }), other.PostAsJsonAsync($"/api/propostas/{id}/avaliar", new { decisao = "aprovada" }));
            try
            {
                var winner = Assert.Single(evaluations, x => x.StatusCode == HttpStatusCode.OK);
                Assert.Single(evaluations, x => x.StatusCode == HttpStatusCode.Conflict);
                using var result = JsonDocument.Parse(await winner.Content.ReadAsStringAsync());
                var activityId = result.RootElement.GetProperty("atividadeId").GetGuid();
                using var catalog = JsonDocument.Parse(await student.GetStringAsync("/api/atividades?tema=Dados"));
                Assert.Contains(catalog.RootElement.EnumerateArray(), x => x.GetProperty("id").GetGuid() == activityId && x.GetProperty("titulo").GetString() == "Oficina de dados");
                var responsible = ReferenceEquals(winner, evaluations[0]) ? teacher : other;
                var notResponsible = ReferenceEquals(winner, evaluations[0]) ? other : teacher;
                Assert.Equal(HttpStatusCode.OK, (await responsible.GetAsync($"/api/atividades/{activityId}/inscricoes")).StatusCode);
                Assert.Equal(HttpStatusCode.Forbidden, (await notResponsible.GetAsync($"/api/atividades/{activityId}/inscricoes")).StatusCode);
                using var map = JsonDocument.Parse(await student.GetStringAsync($"/api/atividades/{activityId}/assentos"));
                Assert.Equal("Sala 101", map.RootElement.GetProperty("sala").GetProperty("nome").GetString());
                Assert.Equal(24, map.RootElement.GetProperty("assentos").GetArrayLength());
                var seat = map.RootElement.GetProperty("assentos")[0].GetProperty("id").GetGuid();
                Assert.Equal(HttpStatusCode.Created, (await unapproved.PostAsJsonAsync($"/api/atividades/{activityId}/inscricoes", new { assentoId = seat })).StatusCode);
                var edit = new { titulo = "Oficina atualizada", tema = "Dados práticos", descricao = "Nova descrição" };
                Assert.Equal(HttpStatusCode.Forbidden, (await unapproved.PutAsJsonAsync($"/api/atividades/{activityId}/conteudo", edit)).StatusCode);
                Assert.Equal(HttpStatusCode.Forbidden, (await responsible.PutAsJsonAsync($"/api/atividades/{activityId}/conteudo", edit)).StatusCode);
                Assert.Equal(HttpStatusCode.BadRequest, (await student.PutAsJsonAsync($"/api/atividades/{activityId}/conteudo", new { titulo = " ", tema = "Dados", descricao = "Texto" })).StatusCode);
                Assert.Equal(HttpStatusCode.OK, (await student.PutAsJsonAsync($"/api/atividades/{activityId}/conteudo", edit)).StatusCode);
                using var updatedActivity = JsonDocument.Parse(await student.GetStringAsync($"/api/atividades/{activityId}"));
                Assert.Equal("Oficina atualizada", updatedActivity.RootElement.GetProperty("titulo").GetString());
                using var updatedProposal = JsonDocument.Parse(await student.GetStringAsync("/api/propostas/minhas"));
                Assert.Contains(updatedProposal.RootElement.EnumerateArray(), p => p.GetProperty("id").GetGuid() == id && p.GetProperty("titulo").GetString() == "Oficina atualizada");
                using var notices = JsonDocument.Parse(await unapproved.GetStringAsync("/api/notificacoes"));
                Assert.Contains(notices.RootElement.EnumerateArray(), n => n.GetProperty("mensagem").GetString()!.Contains("Oficina atualizada"));
                Assert.Equal(HttpStatusCode.OK, (await student.GetAsync($"/api/atividades/{activityId}/inscricoes")).StatusCode);
                Assert.Equal(HttpStatusCode.Forbidden, (await unapproved.PutAsJsonAsync($"/api/atividades/{activityId}/inscricoes/{users[3]}/presenca", new { presente = true })).StatusCode);
                Assert.Equal(HttpStatusCode.Forbidden, (await notResponsible.PutAsJsonAsync($"/api/atividades/{activityId}/inscricoes/{users[3]}/presenca", new { presente = true })).StatusCode);
                Assert.Equal(HttpStatusCode.NoContent, (await student.PutAsJsonAsync($"/api/atividades/{activityId}/inscricoes/{users[3]}/presenca", new { presente = false })).StatusCode);
                Assert.Equal(HttpStatusCode.NoContent, (await responsible.PutAsJsonAsync($"/api/atividades/{activityId}/inscricoes/{users[3]}/presenca", new { presente = true })).StatusCode);
                using var attendance = JsonDocument.Parse(await responsible.GetStringAsync($"/api/atividades/{activityId}/inscricoes"));
                var participant = Assert.Single(attendance.RootElement.EnumerateArray());
                Assert.True(participant.GetProperty("presente").GetBoolean());
                Assert.Equal(seat, participant.GetProperty("assento").GetProperty("id").GetGuid());
                Assert.Equal(map.RootElement.GetProperty("assentos")[0].GetProperty("codigo").GetString(), participant.GetProperty("assento").GetProperty("codigo").GetString());
                using var history = JsonDocument.Parse(await responsible.GetStringAsync("/api/propostas/aprovadas"));
                Assert.Contains(history.RootElement.EnumerateArray(), x => x.GetProperty("id").GetGuid() == id);
                using var otherHistory = JsonDocument.Parse(await notResponsible.GetStringAsync("/api/propostas/aprovadas"));
                Assert.DoesNotContain(otherHistory.RootElement.EnumerateArray(), x => x.GetProperty("id").GetGuid() == id);
            }
            finally { foreach (var response in evaluations) response.Dispose(); }
            using var second = await student.PostAsJsonAsync("/api/propostas", request);
            Assert.Equal(HttpStatusCode.Created, second.StatusCode);
            using var secondJson = JsonDocument.Parse(await second.Content.ReadAsStringAsync());
            var secondId = secondJson.RootElement.GetProperty("id").GetGuid();
            Assert.Equal(HttpStatusCode.Conflict, (await teacher.PostAsJsonAsync($"/api/propostas/{secondId}/avaliar", new { decisao = "aprovada" })).StatusCode);
            Assert.Equal(HttpStatusCode.BadRequest, (await teacher.PostAsJsonAsync($"/api/propostas/{secondId}/avaliar", new { decisao = "recusada" })).StatusCode);
            Assert.Equal(HttpStatusCode.OK, (await teacher.PostAsJsonAsync($"/api/propostas/{secondId}/avaliar", new { decisao = "recusada", motivo = "Escolha outro horário." })).StatusCode);
            var otherRoomRequest = new { titulo = request.titulo, tema = request.tema, descricao = request.descricao, tipoSala = request.tipoSala, numeroSala = "102", inicio = request.inicio, fim = request.fim };
            using var otherRoom = await student.PostAsJsonAsync("/api/propostas", otherRoomRequest);
            using var otherRoomJson = JsonDocument.Parse(await otherRoom.Content.ReadAsStringAsync());
            Assert.Equal(HttpStatusCode.OK, (await teacher.PostAsJsonAsync($"/api/propostas/{otherRoomJson.RootElement.GetProperty("id").GetGuid()}/avaliar", new { decisao = "aprovada" })).StatusCode);
            var concurrentRequest = new { titulo = request.titulo, tema = request.tema, descricao = request.descricao, tipoSala = request.tipoSala, numeroSala = "00103", inicio = request.inicio, fim = request.fim };
            using var concurrentA = await student.PostAsJsonAsync("/api/propostas", concurrentRequest);
            using var concurrentB = await student.PostAsJsonAsync("/api/propostas", concurrentRequest);
            using var concurrentJsonA = JsonDocument.Parse(await concurrentA.Content.ReadAsStringAsync());
            using var concurrentJsonB = JsonDocument.Parse(await concurrentB.Content.ReadAsStringAsync());
            var a = concurrentJsonA.RootElement.GetProperty("id").GetGuid();
            var b = concurrentJsonB.RootElement.GetProperty("id").GetGuid();
            var roomRace = await Task.WhenAll(teacher.PostAsJsonAsync($"/api/propostas/{a}/avaliar", new { decisao = "aprovada" }), other.PostAsJsonAsync($"/api/propostas/{b}/avaliar", new { decisao = "aprovada" }));
            Assert.Single(roomRace, x => x.StatusCode == HttpStatusCode.OK);
            Assert.Single(roomRace, x => x.StatusCode == HttpStatusCode.Conflict);
            foreach (var response in roomRace) response.Dispose();
            using var mine = JsonDocument.Parse(await student.GetStringAsync("/api/propostas/minhas"));
            Assert.Contains(mine.RootElement.EnumerateArray(), x => x.GetProperty("id").GetGuid() == secondId && x.GetProperty("status").GetString() == "recusada" && x.GetProperty("motivoRecusa").GetString() == "Escolha outro horário.");
        }
        finally
        {
            await using var scope = factory.Services.CreateAsyncScope();
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var proposals = await db.PropostasAula.Where(x => users.Contains(x.AlunoId)).ToListAsync();
            var activities = proposals.Where(x => x.AtividadeId != null).Select(x => x.AtividadeId!.Value).ToArray();
            db.PropostasAula.RemoveRange(proposals); db.Notificacoes.RemoveRange(await db.Notificacoes.Where(x => users.Contains(x.UsuarioId)).ToListAsync()); await db.SaveChangesAsync();
            db.Inscricoes.RemoveRange(await db.Inscricoes.Where(x => activities.Contains(x.AtividadeId)).ToListAsync()); await db.SaveChangesAsync();
            db.Atividades.RemoveRange(await db.Atividades.Where(x => activities.Contains(x.Id)).ToListAsync()); await db.SaveChangesAsync();
            var rooms = await db.Salas.Where(x => x.Numero == "101" || x.Numero == "102" || x.Numero == "103").ToListAsync();
            var roomIds = rooms.Select(x => x.Id).ToArray();
            db.Assentos.RemoveRange(await db.Assentos.Where(x => roomIds.Contains(x.SalaId)).ToListAsync());
            db.Salas.RemoveRange(rooms); await db.SaveChangesAsync();
            db.Users.RemoveRange(await db.Users.Where(x => users.Contains(x.Id)).ToListAsync()); await db.SaveChangesAsync();
        }
    }
}
