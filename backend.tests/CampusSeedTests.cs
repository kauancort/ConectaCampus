using AprendizagemColaborativa.Api.Data;
using AprendizagemColaborativa.Api.Features.Auth;
using AprendizagemColaborativa.Api.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace AprendizagemColaborativa.Api.Tests;

public sealed class CampusSeedTests(WebApplicationFactory<Program> factory) : IClassFixture<WebApplicationFactory<Program>>
{
    [Fact]
    public async Task Aulas_antigas_recebem_inscritos_sem_duplicar_nem_reverter_cancelamentos()
    {
        using var client = factory.CreateClient();
        await using var scope = factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var users = scope.ServiceProvider.GetRequiredService<UserManager<Aluno>>();
        Assert.Null(await users.FindByEmailAsync(DemoAccountSeeder.StudentEmail));
        await DemoAccountSeeder.SeedAsync(users, NullLogger.Instance);
        var student = (await users.FindByEmailAsync(DemoAccountSeeder.StudentEmail))!;
        var teacher = (await users.FindByEmailAsync(DemoAccountSeeder.TeacherEmail))!;
        var id = Guid.NewGuid();
        var now = DateTime.UtcNow;
        try
        {
            db.Atividades.Add(new Atividade { Id = id, Titulo = "JavaScript na prática", Tema = "Programação", Descricao = "Explore variáveis, condições e eventos ao criar pequenas interações no navegador.", Modalidade = "AULA", Tipo = "aula", Nivel = "não informado", SalaId = SalaSeed.SalaDesktopId, Inicio = now.AddDays(500), Fim = now.AddDays(500).AddHours(1), DataHoraUtc = now.AddDays(500), Local = "Laboratório Desktop", NumeroDeVagas = 24, ResponsavelId = student.Id, OrganizadorId = student.Id });
            db.PropostasAula.Add(new PropostaAula { Id = Guid.NewGuid(), AlunoId = student.Id, ProfessorId = teacher.Id, AtividadeId = id, Titulo = "JavaScript na prática", Tema = "Programação", Descricao = "Explore variáveis, condições e eventos ao criar pequenas interações no navegador.", TipoSala = "DESKTOP", Inicio = now.AddDays(500), Fim = now.AddDays(500).AddHours(1), Status = "aprovada" });
            await db.SaveChangesAsync();
            await CampusSeed.SeedAsync(db, users, NullLogger.Instance);
            var inscritos = await db.Inscricoes.Where(i => i.AtividadeId == id).ToListAsync();
            Assert.Equal(8, inscritos.Count);
            Assert.Equal(8, inscritos.Select(i => i.AssentoId).Distinct().Count());
            var canceled = inscritos[0]; canceled.Status = "cancelada"; canceled.AssentoId = null;
            await db.SaveChangesAsync();
            await CampusSeed.SeedAsync(db, users, NullLogger.Instance);
            var after = await db.Inscricoes.AsNoTracking().Where(i => i.AtividadeId == id).ToListAsync();
            Assert.Equal(8, after.Count);
            Assert.Equal("cancelada", after.Single(i => i.Id == canceled.Id).Status);
            Assert.Equal(7, after.Count(i => i.Status == "ativa"));
        }
        finally
        {
            var ids = await db.Users.Where(u => u.Email!.EndsWith("@exemplo.conectacampus.local") || u.Id == student.Id || u.Id == teacher.Id).Select(u => u.Id).ToListAsync();
            var activityIds = await db.Atividades.Where(a => ids.Contains(a.ResponsavelId!) || a.Id == id).Select(a => a.Id).ToListAsync();
            db.Notificacoes.RemoveRange(await db.Notificacoes.Where(n => ids.Contains(n.UsuarioId)).ToListAsync());
            db.SolicitacoesProfessor.RemoveRange(await db.SolicitacoesProfessor.Where(s => ids.Contains(s.AlunoId)).ToListAsync());
            db.Inscricoes.RemoveRange(await db.Inscricoes.Where(i => activityIds.Contains(i.AtividadeId) || ids.Contains(i.AlunoId)).ToListAsync());
            db.PropostasAula.RemoveRange(await db.PropostasAula.Where(p => ids.Contains(p.AlunoId) || activityIds.Contains(p.AtividadeId ?? Guid.Empty)).ToListAsync());
            await db.SaveChangesAsync();
            db.Atividades.RemoveRange(await db.Atividades.Where(a => activityIds.Contains(a.Id)).ToListAsync());
            await db.SaveChangesAsync();
            db.Users.RemoveRange(await db.Users.Where(u => ids.Contains(u.Id)).ToListAsync());
            await db.SaveChangesAsync();
        }
    }
}
