using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AprendizagemColaborativa.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddAlunoProfileAndUniqueEmail : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "EmailIndex",
                table: "AspNetUsers");

            migrationBuilder.AddColumn<string>(
                name: "Curso",
                table: "AspNetUsers",
                type: "varchar(120)",
                maxLength: 120,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Nome",
                table: "AspNetUsers",
                type: "varchar(200)",
                maxLength: 200,
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateIndex(
                name: "EmailIndex",
                table: "AspNetUsers",
                column: "NormalizedEmail",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "EmailIndex",
                table: "AspNetUsers");

            migrationBuilder.DropColumn(
                name: "Curso",
                table: "AspNetUsers");

            migrationBuilder.DropColumn(
                name: "Nome",
                table: "AspNetUsers");

            migrationBuilder.CreateIndex(
                name: "EmailIndex",
                table: "AspNetUsers",
                column: "NormalizedEmail");
        }
    }
}
