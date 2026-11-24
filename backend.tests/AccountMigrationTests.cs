using AprendizagemColaborativa.Api.Data;
using AprendizagemColaborativa.Api.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;
using Xunit;

namespace AprendizagemColaborativa.Api.Tests;

[Collection(ApiIntegrationCollection.Name)]
public sealed class AccountMigrationTests(WebApplicationFactory<Program> factory)
{
    [Fact]
    public async Task Atualizacao_preserva_usuarios_senhas_e_atividade_e_reconhece_ambos_os_responsaveis()
    {
        var connectionString = factory.Services.GetRequiredService<IConfiguration>()
            .GetConnectionString("DefaultConnection");
        await using var admin = new NpgsqlConnection(connectionString);
        await admin.OpenAsync();

        // Nome gerado pelo teste; o banco principal não é rebaixado nem apagado.
        var databaseName = "conectacampus_migration_" + Guid.NewGuid().ToString("N");
        await using (var create = new NpgsqlCommand($"CREATE DATABASE \"{databaseName}\"", admin))
        {
            await create.ExecuteNonQueryAsync();
        }

        try
        {
            var settings = new NpgsqlConnectionStringBuilder(connectionString)
            {
                Database = databaseName,
                Pooling = false
            };
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseNpgsql(settings.ConnectionString, postgres =>
                    postgres.MigrationsHistoryTable("__EFMigrationsHistory", ApplicationDbContext.Schema))
                .Options;
            await using var db = new ApplicationDbContext(options);
            var initialMigration = db.Database.GetMigrations().Single(id => id.EndsWith("_InitialPostgreSql", StringComparison.Ordinal));
            await db.GetService<IMigrator>().MigrateAsync(initialMigration);

            var hasher = new PasswordHasher<Aluno>();
            var originalHashes = new Dictionary<string, string>();
            var password = "Tests!" + Guid.NewGuid().ToString("N");
            foreach (var id in new[] { "aluno-legado", "organizador-legado", "responsavel-legado" })
            {
                var hash = hasher.HashPassword(new Aluno(), password);
                originalHashes[id] = hash;
                await db.Database.ExecuteSqlInterpolatedAsync($"""
                    INSERT INTO conectacampus."AspNetUsers"
                        ("Id", "Nome", "Curso", "PasswordHash", "EmailConfirmed", "PhoneNumberConfirmed",
                         "TwoFactorEnabled", "LockoutEnabled", "AccessFailedCount")
                    VALUES ({id}, {id}, 'DSM', {hash}, false, false, false, false, 0)
                    """);
            }

            var activityId = Guid.NewGuid();
            await db.Database.ExecuteSqlInterpolatedAsync($"""
                INSERT INTO conectacampus."Atividades"
                    ("Id", "OrganizadorId", "ResponsavelId", "Tipo", "Tema", "Descricao", "Nivel",
                     "DataHoraUtc", "Local", "NumeroDeVagas", "Status")
                VALUES ({activityId}, 'organizador-legado', 'responsavel-legado', 'aula',
                    'Aula existente', 'Atividade anterior aos perfis', 'iniciante',
                    {new DateTime(2035, 1, 1, 12, 0, 0, DateTimeKind.Utc)}, 'Campus', 10, 'publicada')
                """);

            await db.Database.MigrateAsync();

            var users = await db.Users.AsNoTracking().OrderBy(user => user.Id).ToListAsync();
            Assert.Equal(3, users.Count);
            Assert.Equal(TiposConta.Aluno, users.Single(user => user.Id == "aluno-legado").TipoConta);
            Assert.Equal(TiposConta.Professor, users.Single(user => user.Id == "organizador-legado").TipoConta);
            Assert.Equal(TiposConta.Professor, users.Single(user => user.Id == "responsavel-legado").TipoConta);
            Assert.All(users, user =>
            {
                Assert.Equal(user.Id, user.Nome);
                Assert.Equal("DSM", user.Curso);
                Assert.Equal(originalHashes[user.Id], user.PasswordHash);
                Assert.Equal(PasswordVerificationResult.Success, hasher.VerifyHashedPassword(user, user.PasswordHash!, password));
            });
            var activity = Assert.Single(await db.Atividades.AsNoTracking().ToListAsync());
            Assert.Equal(activityId, activity.Id);
            Assert.Equal("Aula existente", activity.Tema);
            Assert.Equal("organizador-legado", activity.OrganizadorId);
            Assert.Equal("responsavel-legado", activity.ResponsavelId);
            Assert.Empty(await db.Database.GetPendingMigrationsAsync());
        }
        finally
        {
            // Remove exclusivamente o banco auxiliar criado acima, inclusive em falhas.
            await using var drop = new NpgsqlCommand($"DROP DATABASE IF EXISTS \"{databaseName}\" WITH (FORCE)", admin);
            await drop.ExecuteNonQueryAsync();
        }
    }
}
