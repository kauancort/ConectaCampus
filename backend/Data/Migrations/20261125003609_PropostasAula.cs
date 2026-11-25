using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AprendizagemColaborativa.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class PropostasAula : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Titulo",
                schema: "conectacampus",
                table: "Atividades",
                type: "character varying(200)",
                maxLength: 200,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<bool>(
                name: "PodeMinistrar",
                schema: "conectacampus",
                table: "AspNetUsers",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            // Antes, a aprovação do aluno concedia indevidamente o perfil de professor.
            migrationBuilder.Sql("""
                UPDATE conectacampus."AspNetUsers" AS u
                SET "PodeMinistrar" = true, "Professor" = false
                WHERE EXISTS (SELECT 1 FROM conectacampus."SolicitacoesProfessor" s
                              WHERE s."AlunoId" = u."Id" AND s."Status" = 'aprovada');
                UPDATE conectacampus."Atividades" SET "Titulo" = "Tema" WHERE "Titulo" = '';
                """);

            migrationBuilder.CreateTable(
                name: "PropostasAula",
                schema: "conectacampus",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    AlunoId = table.Column<string>(type: "text", nullable: false),
                    Titulo = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Tema = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Descricao = table.Column<string>(type: "character varying(5000)", maxLength: 5000, nullable: false),
                    TipoSala = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    Inicio = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Fim = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    CriadaEmUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ProfessorId = table.Column<string>(type: "text", nullable: true),
                    AvaliadaEmUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    MotivoRecusa = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    AtividadeId = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PropostasAula", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PropostasAula_AspNetUsers_AlunoId",
                        column: x => x.AlunoId,
                        principalSchema: "conectacampus",
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PropostasAula_AspNetUsers_ProfessorId",
                        column: x => x.ProfessorId,
                        principalSchema: "conectacampus",
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PropostasAula_Atividades_AtividadeId",
                        column: x => x.AtividadeId,
                        principalSchema: "conectacampus",
                        principalTable: "Atividades",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_PropostasAula_AlunoId",
                schema: "conectacampus",
                table: "PropostasAula",
                column: "AlunoId");

            migrationBuilder.CreateIndex(
                name: "IX_PropostasAula_AtividadeId",
                schema: "conectacampus",
                table: "PropostasAula",
                column: "AtividadeId");

            migrationBuilder.CreateIndex(
                name: "IX_PropostasAula_ProfessorId",
                schema: "conectacampus",
                table: "PropostasAula",
                column: "ProfessorId");

            migrationBuilder.CreateIndex(
                name: "IX_PropostasAula_Status",
                schema: "conectacampus",
                table: "PropostasAula",
                column: "Status");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PropostasAula",
                schema: "conectacampus");

            migrationBuilder.DropColumn(
                name: "Titulo",
                schema: "conectacampus",
                table: "Atividades");

            migrationBuilder.DropColumn(
                name: "PodeMinistrar",
                schema: "conectacampus",
                table: "AspNetUsers");
        }
    }
}
