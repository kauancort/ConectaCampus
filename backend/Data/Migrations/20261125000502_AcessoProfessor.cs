using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AprendizagemColaborativa.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class AcessoProfessor : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "Professor",
                schema: "conectacampus",
                table: "AspNetUsers",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateTable(
                name: "Notificacoes",
                schema: "conectacampus",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UsuarioId = table.Column<string>(type: "text", nullable: false),
                    Mensagem = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    Lida = table.Column<bool>(type: "boolean", nullable: false),
                    CriadaEmUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Notificacoes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Notificacoes_AspNetUsers_UsuarioId",
                        column: x => x.UsuarioId,
                        principalSchema: "conectacampus",
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "SolicitacoesProfessor",
                schema: "conectacampus",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    AlunoId = table.Column<string>(type: "text", nullable: false),
                    Justificativa = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    Status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    CriadaEmUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    AvaliadorId = table.Column<string>(type: "text", nullable: true),
                    AvaliadaEmUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SolicitacoesProfessor", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SolicitacoesProfessor_AspNetUsers_AlunoId",
                        column: x => x.AlunoId,
                        principalSchema: "conectacampus",
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Notificacoes_UsuarioId_Lida",
                schema: "conectacampus",
                table: "Notificacoes",
                columns: new[] { "UsuarioId", "Lida" });

            migrationBuilder.CreateIndex(
                name: "IX_SolicitacoesProfessor_AlunoId",
                schema: "conectacampus",
                table: "SolicitacoesProfessor",
                column: "AlunoId",
                unique: true,
                filter: "\"Status\" = 'pendente'");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Notificacoes",
                schema: "conectacampus");

            migrationBuilder.DropTable(
                name: "SolicitacoesProfessor",
                schema: "conectacampus");

            migrationBuilder.DropColumn(
                name: "Professor",
                schema: "conectacampus",
                table: "AspNetUsers");
        }
    }
}
