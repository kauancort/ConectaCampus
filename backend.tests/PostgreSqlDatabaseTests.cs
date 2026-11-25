using AprendizagemColaborativa.Api.Data;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace AprendizagemColaborativa.Api.Tests;

public sealed class PostgreSqlDatabaseTests(WebApplicationFactory<Program> factory)
    : IClassFixture<WebApplicationFactory<Program>>
{
    [Fact]
    public async Task Inicializacao_aplica_migration_e_reexecucao_preserva_seed()
    {
        using var client = factory.CreateClient();
        await using var scope = factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        Assert.True((await client.GetAsync("/health")).IsSuccessStatusCode);
        var migrations = await db.Database.GetAppliedMigrationsAsync();
        Assert.Contains(migrations, migration => migration.EndsWith("_InitialPostgreSql"));
        Assert.Contains(migrations, migration => migration.EndsWith("_AcessoProfessor"));
        Assert.Empty(await db.Database.GetPendingMigrationsAsync());

        await db.Database.MigrateAsync();
        Assert.Equal(2, await db.Salas.CountAsync());
        Assert.Equal(44, await db.Assentos.CountAsync());
    }

    [Fact]
    public async Task Tabelas_e_identity_ficam_no_schema_privado_sem_acesso_publico()
    {
        using var client = factory.CreateClient();
        await using var scope = factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        var publicGrants = await db.Database.SqlQueryRaw<int>("""
            SELECT count(*)::integer AS "Value"
            FROM pg_namespace n,
                LATERAL aclexplode(COALESCE(n.nspacl, acldefault('n', n.nspowner))) acl
            WHERE n.nspname = 'conectacampus' AND acl.grantee = 0
            """).SingleAsync();
        Assert.Equal(0, publicGrants);

        var exposedTables = await db.Database.SqlQueryRaw<int>("""
            SELECT count(*)::integer AS "Value" FROM information_schema.tables
            WHERE table_schema = 'public' AND table_name IN
                ('AspNetUsers', 'AspNetUserClaims', 'AspNetUserLogins', 'AspNetUserTokens',
                 'Atividades', 'Inscricoes', 'Salas', 'Assentos', '__EFMigrationsHistory')
            """).SingleAsync();
        Assert.Equal(0, exposedTables);

        var apiRolesWithAccess = await db.Database.SqlQueryRaw<int>("""
            SELECT count(*)::integer AS "Value" FROM pg_roles
            WHERE rolname IN ('anon', 'authenticated', 'service_role')
                AND has_schema_privilege(oid, 'conectacampus', 'USAGE')
            """).SingleAsync();
        Assert.Equal(0, apiRolesWithAccess);
    }
}
