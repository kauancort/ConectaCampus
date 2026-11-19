using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using AprendizagemColaborativa.Api.Models;

namespace AprendizagemColaborativa.Api.Data;

public sealed class ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
    : IdentityUserContext<Aluno>(options)
{
    public DbSet<Sala> Salas => Set<Sala>();
    public DbSet<Assento> Assentos => Set<Assento>();
    public DbSet<Atividade> Atividades => Set<Atividade>();
    public DbSet<Inscricao> Inscricoes => Set<Inscricao>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);
        builder.Entity<Aluno>(entity =>
        {
            entity.Property(aluno => aluno.Nome).HasMaxLength(200).IsRequired();
            entity.Property(aluno => aluno.Curso).HasMaxLength(120).IsRequired();
            entity.HasIndex(aluno => aluno.NormalizedEmail).IsUnique().HasDatabaseName("EmailIndex");
        });
        builder.Entity<Sala>(entity =>
        {
            entity.Property(sala => sala.Nome).HasMaxLength(120).IsRequired();
            entity.Property(sala => sala.Tipo).HasMaxLength(20).IsRequired();
            entity.Property(sala => sala.Capacidade).IsRequired();
            entity.Property(sala => sala.Status).HasMaxLength(20).IsRequired();
        });
        builder.Entity<Assento>(entity =>
        {
            entity.Property(assento => assento.Codigo).HasMaxLength(10).IsRequired();
            entity.Property(assento => assento.Fileira).HasMaxLength(1).IsRequired();
            entity.Property(assento => assento.Coluna).IsRequired();
            entity.HasIndex(assento => new { assento.SalaId, assento.Codigo }).IsUnique();
            entity.HasIndex(assento => new { assento.SalaId, assento.Fileira, assento.Coluna }).IsUnique();
            entity.HasOne(assento => assento.Sala)
                .WithMany(sala => sala.Assentos)
                .HasForeignKey(assento => assento.SalaId)
                .OnDelete(DeleteBehavior.Restrict);
        });
        builder.Entity<Atividade>(entity =>
        {
            entity.Property(atividade => atividade.Modalidade).HasMaxLength(30);
            entity.Property(atividade => atividade.Inicio).HasColumnType("datetime(6)");
            entity.Property(atividade => atividade.Fim).HasColumnType("datetime(6)");
            entity.Property(atividade => atividade.Tipo).HasMaxLength(30).IsRequired();
            entity.Property(atividade => atividade.Tema).HasMaxLength(200).IsRequired();
            entity.Property(atividade => atividade.Descricao).HasMaxLength(5000).IsRequired();
            entity.Property(atividade => atividade.Nivel).HasMaxLength(80).IsRequired();
            entity.Property(atividade => atividade.DataHoraUtc).HasColumnType("datetime(6)").IsRequired();
            entity.Property(atividade => atividade.Local).HasMaxLength(300).IsRequired();
            entity.Property(atividade => atividade.Status).HasMaxLength(20).IsRequired();
            entity.HasIndex(atividade => atividade.OrganizadorId);
            entity.HasIndex(atividade => atividade.ResponsavelId);
            entity.HasIndex(atividade => atividade.SalaId);
            entity.HasIndex(atividade => atividade.DataHoraUtc);
            entity.HasOne(atividade => atividade.Organizador)
                .WithMany()
                .HasForeignKey(atividade => atividade.OrganizadorId)
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(atividade => atividade.Responsavel)
                .WithMany()
                .HasForeignKey(atividade => atividade.ResponsavelId)
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(atividade => atividade.Sala)
                .WithMany(sala => sala.Atividades)
                .HasForeignKey(atividade => atividade.SalaId)
                .OnDelete(DeleteBehavior.Restrict);
        });
        builder.Entity<Inscricao>(entity =>
        {
            entity.Property(inscricao => inscricao.Status).HasMaxLength(20).IsRequired();
            entity.Property(inscricao => inscricao.CriadaEmUtc).HasColumnType("datetime(6)").IsRequired();
            entity.Property(inscricao => inscricao.AtualizadaEmUtc).HasColumnType("datetime(6)").IsRequired();
            entity.HasIndex(inscricao => new { inscricao.AtividadeId, inscricao.AlunoId }).IsUnique();
            entity.HasIndex(inscricao => inscricao.AlunoId);
            entity.HasOne(inscricao => inscricao.Atividade)
                .WithMany()
                .HasForeignKey(inscricao => inscricao.AtividadeId)
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(inscricao => inscricao.Aluno)
                .WithMany()
                .HasForeignKey(inscricao => inscricao.AlunoId)
                .OnDelete(DeleteBehavior.Restrict);
        });
    }
}
