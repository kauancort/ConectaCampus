using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AprendizagemColaborativa.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddInscricoes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Inscricoes",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "char(36)", nullable: false),
                    AtividadeId = table.Column<Guid>(type: "char(36)", nullable: false),
                    AlunoId = table.Column<string>(type: "varchar(255)", nullable: false),
                    Status = table.Column<string>(type: "varchar(20)", maxLength: 20, nullable: false),
                    CriadaEmUtc = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    AtualizadaEmUtc = table.Column<DateTime>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Inscricoes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Inscricoes_AspNetUsers_AlunoId",
                        column: x => x.AlunoId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Inscricoes_Atividades_AtividadeId",
                        column: x => x.AtividadeId,
                        principalTable: "Atividades",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_Inscricoes_AlunoId",
                table: "Inscricoes",
                column: "AlunoId");

            migrationBuilder.CreateIndex(
                name: "IX_Inscricoes_AtividadeId_AlunoId",
                table: "Inscricoes",
                columns: new[] { "AtividadeId", "AlunoId" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Inscricoes");
        }
    }
}
