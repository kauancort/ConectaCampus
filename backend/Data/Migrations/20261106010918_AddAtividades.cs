using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AprendizagemColaborativa.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddAtividades : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Atividades",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "char(36)", nullable: false),
                    OrganizadorId = table.Column<string>(type: "varchar(255)", nullable: false),
                    Tipo = table.Column<string>(type: "varchar(30)", maxLength: 30, nullable: false),
                    Tema = table.Column<string>(type: "varchar(200)", maxLength: 200, nullable: false),
                    Descricao = table.Column<string>(type: "varchar(5000)", maxLength: 5000, nullable: false),
                    Nivel = table.Column<string>(type: "varchar(80)", maxLength: 80, nullable: false),
                    DataHoraUtc = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    Local = table.Column<string>(type: "varchar(300)", maxLength: 300, nullable: false),
                    NumeroDeVagas = table.Column<int>(type: "int", nullable: false),
                    Status = table.Column<string>(type: "varchar(20)", maxLength: 20, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Atividades", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Atividades_AspNetUsers_OrganizadorId",
                        column: x => x.OrganizadorId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_Atividades_DataHoraUtc",
                table: "Atividades",
                column: "DataHoraUtc");

            migrationBuilder.CreateIndex(
                name: "IX_Atividades_OrganizadorId",
                table: "Atividades",
                column: "OrganizadorId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Atividades");
        }
    }
}
