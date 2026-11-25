using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AprendizagemColaborativa.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class NumeroSala : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Numero",
                schema: "conectacampus",
                table: "Salas",
                type: "character varying(6)",
                maxLength: 6,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "NumeroSala",
                schema: "conectacampus",
                table: "PropostasAula",
                type: "character varying(6)",
                maxLength: 6,
                nullable: true);

            migrationBuilder.UpdateData(
                schema: "conectacampus",
                table: "Salas",
                keyColumn: "Id",
                keyValue: new Guid("0e9abb4a-5aa5-4e6d-b94a-774a5fad4f10"),
                column: "Numero",
                value: null);

            migrationBuilder.UpdateData(
                schema: "conectacampus",
                table: "Salas",
                keyColumn: "Id",
                keyValue: new Guid("70cb6f07-08e2-4664-8d45-334564cdd768"),
                column: "Numero",
                value: null);

            migrationBuilder.CreateIndex(
                name: "IX_Salas_Numero",
                schema: "conectacampus",
                table: "Salas",
                column: "Numero",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Salas_Numero",
                schema: "conectacampus",
                table: "Salas");

            migrationBuilder.DropColumn(
                name: "Numero",
                schema: "conectacampus",
                table: "Salas");

            migrationBuilder.DropColumn(
                name: "NumeroSala",
                schema: "conectacampus",
                table: "PropostasAula");
        }
    }
}
