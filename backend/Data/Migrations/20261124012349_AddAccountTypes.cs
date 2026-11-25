using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AprendizagemColaborativa.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddAccountTypes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "TipoConta",
                schema: "conectacampus",
                table: "AspNetUsers",
                type: "character varying(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "aluno");

            // Preserva o acesso dos responsáveis que já publicavam atividades.
            migrationBuilder.Sql("""
                UPDATE conectacampus."AspNetUsers" AS usuario
                SET "TipoConta" = 'professor'
                WHERE EXISTS (
                    SELECT 1 FROM conectacampus."Atividades" AS atividade
                    WHERE atividade."OrganizadorId" = usuario."Id"
                       OR atividade."ResponsavelId" = usuario."Id"
                );
                """);

            migrationBuilder.AddCheckConstraint(
                name: "CK_AspNetUsers_TipoConta",
                schema: "conectacampus",
                table: "AspNetUsers",
                sql: "\"TipoConta\" IN ('aluno', 'professor')");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "CK_AspNetUsers_TipoConta",
                schema: "conectacampus",
                table: "AspNetUsers");

            migrationBuilder.DropColumn(
                name: "TipoConta",
                schema: "conectacampus",
                table: "AspNetUsers");
        }
    }
}
