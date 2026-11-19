using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace AprendizagemColaborativa.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddSalasAssentos : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "AssentoId",
                table: "Inscricoes",
                type: "char(36)",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "Data",
                table: "Inscricoes",
                type: "datetime(6)",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "Fim",
                table: "Atividades",
                type: "datetime(6)",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "Inicio",
                table: "Atividades",
                type: "datetime(6)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Modalidade",
                table: "Atividades",
                type: "varchar(30)",
                maxLength: 30,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ResponsavelId",
                table: "Atividades",
                type: "varchar(255)",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "SalaId",
                table: "Atividades",
                type: "char(36)",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "Salas",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "char(36)", nullable: false),
                    Nome = table.Column<string>(type: "varchar(120)", maxLength: 120, nullable: false),
                    Tipo = table.Column<string>(type: "varchar(20)", maxLength: 20, nullable: false),
                    Capacidade = table.Column<int>(type: "int", nullable: false),
                    Status = table.Column<string>(type: "varchar(20)", maxLength: 20, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Salas", x => x.Id);
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "Assentos",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "char(36)", nullable: false),
                    SalaId = table.Column<Guid>(type: "char(36)", nullable: false),
                    Codigo = table.Column<string>(type: "varchar(10)", maxLength: 10, nullable: false),
                    Fileira = table.Column<string>(type: "varchar(1)", maxLength: 1, nullable: false),
                    Coluna = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Assentos", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Assentos_Salas_SalaId",
                        column: x => x.SalaId,
                        principalTable: "Salas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.InsertData(
                table: "Salas",
                columns: new[] { "Id", "Capacidade", "Nome", "Status", "Tipo" },
                values: new object[,]
                {
                    { new Guid("0e9abb4a-5aa5-4e6d-b94a-774a5fad4f10"), 24, "Laboratório Desktop", "disponivel", "DESKTOP" },
                    { new Guid("70cb6f07-08e2-4664-8d45-334564cdd768"), 20, "Laboratório Notebook", "disponivel", "NOTEBOOK" }
                });

            migrationBuilder.InsertData(
                table: "Assentos",
                columns: new[] { "Id", "Codigo", "Coluna", "Fileira", "SalaId" },
                values: new object[,]
                {
                    { new Guid("098c2662-93b5-9ce8-4b04-d9eff21b3659"), "B4", 4, "B", new Guid("0e9abb4a-5aa5-4e6d-b94a-774a5fad4f10") },
                    { new Guid("0a70d629-5951-1411-cff8-c0274e194984"), "A5", 5, "A", new Guid("70cb6f07-08e2-4664-8d45-334564cdd768") },
                    { new Guid("10599837-58db-20bb-c6c2-5aa10dc023ed"), "C2", 2, "C", new Guid("0e9abb4a-5aa5-4e6d-b94a-774a5fad4f10") },
                    { new Guid("17efbb91-98d1-0040-1d7c-e6d75501313a"), "C1", 1, "C", new Guid("0e9abb4a-5aa5-4e6d-b94a-774a5fad4f10") },
                    { new Guid("19b48232-cbfc-371d-0821-bc7ed605fadb"), "A1", 1, "A", new Guid("70cb6f07-08e2-4664-8d45-334564cdd768") },
                    { new Guid("21df7fb0-6a7c-1d63-5607-2f9621be0b66"), "B2", 2, "B", new Guid("70cb6f07-08e2-4664-8d45-334564cdd768") },
                    { new Guid("285add86-c7ec-d97b-3d01-6af7e1fd423f"), "C5", 5, "C", new Guid("0e9abb4a-5aa5-4e6d-b94a-774a5fad4f10") },
                    { new Guid("2f58413a-c757-7e96-1022-f5dd4f79fb70"), "C2", 2, "C", new Guid("70cb6f07-08e2-4664-8d45-334564cdd768") },
                    { new Guid("4315347a-b6e2-2398-c4ed-0657ec8dfee2"), "A3", 3, "A", new Guid("70cb6f07-08e2-4664-8d45-334564cdd768") },
                    { new Guid("4caae21c-a204-19e0-586e-e4f81c195cd4"), "B3", 3, "B", new Guid("70cb6f07-08e2-4664-8d45-334564cdd768") },
                    { new Guid("4e4ca461-98ff-5505-d9f1-8886c1d6ef0e"), "B4", 4, "B", new Guid("70cb6f07-08e2-4664-8d45-334564cdd768") },
                    { new Guid("4fc3ea23-2313-54bf-2e4d-1add2da3bf8e"), "A2", 2, "A", new Guid("70cb6f07-08e2-4664-8d45-334564cdd768") },
                    { new Guid("53fc9743-3577-3b65-4229-21b2f939d41a"), "C1", 1, "C", new Guid("70cb6f07-08e2-4664-8d45-334564cdd768") },
                    { new Guid("5522bbbe-a11e-1906-1bf7-9b0058214530"), "C4", 4, "C", new Guid("70cb6f07-08e2-4664-8d45-334564cdd768") },
                    { new Guid("554ed00a-df67-3ea8-4ca6-af7c31549f82"), "A4", 4, "A", new Guid("70cb6f07-08e2-4664-8d45-334564cdd768") },
                    { new Guid("582fffd4-a50b-52f8-3f4d-1d59e9fda3ef"), "A6", 6, "A", new Guid("0e9abb4a-5aa5-4e6d-b94a-774a5fad4f10") },
                    { new Guid("5c49826f-5b48-00a0-0aab-3c7cc3de0f33"), "D5", 5, "D", new Guid("0e9abb4a-5aa5-4e6d-b94a-774a5fad4f10") },
                    { new Guid("5cfe75b0-64a7-9e84-6146-b9260afd2600"), "B5", 5, "B", new Guid("0e9abb4a-5aa5-4e6d-b94a-774a5fad4f10") },
                    { new Guid("5d44f1c9-a571-650c-ab1b-3dbfdc5c8693"), "C3", 3, "C", new Guid("0e9abb4a-5aa5-4e6d-b94a-774a5fad4f10") },
                    { new Guid("6793be2f-032f-9e1c-6382-3c8fab292e3c"), "C4", 4, "C", new Guid("0e9abb4a-5aa5-4e6d-b94a-774a5fad4f10") },
                    { new Guid("6c8ebc23-ac82-bc3e-ce6b-7fc26dfcb6a6"), "B2", 2, "B", new Guid("0e9abb4a-5aa5-4e6d-b94a-774a5fad4f10") },
                    { new Guid("73182fcd-2aa6-bc72-0f86-1ad27e49a012"), "C5", 5, "C", new Guid("70cb6f07-08e2-4664-8d45-334564cdd768") },
                    { new Guid("76b9e1ac-b7e1-5bc2-e3f8-39b8a0fb8120"), "D1", 1, "D", new Guid("70cb6f07-08e2-4664-8d45-334564cdd768") },
                    { new Guid("76eb5964-0c1a-37bc-ae09-2f9f70a06044"), "B5", 5, "B", new Guid("70cb6f07-08e2-4664-8d45-334564cdd768") },
                    { new Guid("7b6b0dbe-4bf7-9ab1-90fe-88225c8410f4"), "B3", 3, "B", new Guid("0e9abb4a-5aa5-4e6d-b94a-774a5fad4f10") },
                    { new Guid("8921d0b7-4093-d1cf-5e05-ec8440b974e8"), "C6", 6, "C", new Guid("0e9abb4a-5aa5-4e6d-b94a-774a5fad4f10") },
                    { new Guid("9230b494-1c2d-70da-a624-789290b26530"), "A5", 5, "A", new Guid("0e9abb4a-5aa5-4e6d-b94a-774a5fad4f10") },
                    { new Guid("9a9c4f4d-5e87-92e2-d193-0ac38f034f76"), "D3", 3, "D", new Guid("70cb6f07-08e2-4664-8d45-334564cdd768") },
                    { new Guid("9ed0e7b0-a409-97c6-6153-465fd78132fd"), "D5", 5, "D", new Guid("70cb6f07-08e2-4664-8d45-334564cdd768") },
                    { new Guid("a1cb9179-a574-7271-1abe-404243d02186"), "D2", 2, "D", new Guid("70cb6f07-08e2-4664-8d45-334564cdd768") },
                    { new Guid("a32192fe-04e7-5297-0fd4-ed638a413005"), "D3", 3, "D", new Guid("0e9abb4a-5aa5-4e6d-b94a-774a5fad4f10") },
                    { new Guid("a62ea0b8-93d0-c426-1e99-238fe61af868"), "C3", 3, "C", new Guid("70cb6f07-08e2-4664-8d45-334564cdd768") },
                    { new Guid("a8bccad6-a20b-a9d5-937a-b606bfeb35c4"), "D4", 4, "D", new Guid("70cb6f07-08e2-4664-8d45-334564cdd768") },
                    { new Guid("a99237fe-df88-cf68-e4d7-c2c90b69fb75"), "D6", 6, "D", new Guid("0e9abb4a-5aa5-4e6d-b94a-774a5fad4f10") },
                    { new Guid("b88da7ae-53a7-533d-7851-60d010b697f7"), "D1", 1, "D", new Guid("0e9abb4a-5aa5-4e6d-b94a-774a5fad4f10") },
                    { new Guid("c6275499-883e-52b8-2ff0-3e81f3703570"), "D2", 2, "D", new Guid("0e9abb4a-5aa5-4e6d-b94a-774a5fad4f10") },
                    { new Guid("d474a9a6-6fbd-91aa-65bf-fc2464478a42"), "B6", 6, "B", new Guid("0e9abb4a-5aa5-4e6d-b94a-774a5fad4f10") },
                    { new Guid("d48da9c7-a5dd-e204-6237-9ae67a8a93e7"), "A3", 3, "A", new Guid("0e9abb4a-5aa5-4e6d-b94a-774a5fad4f10") },
                    { new Guid("d7ad5a1f-69ac-2aef-0e38-6aa8468bf597"), "B1", 1, "B", new Guid("0e9abb4a-5aa5-4e6d-b94a-774a5fad4f10") },
                    { new Guid("d90ec8ef-657f-1cf3-011c-592f25921b83"), "A4", 4, "A", new Guid("0e9abb4a-5aa5-4e6d-b94a-774a5fad4f10") },
                    { new Guid("dc9a1c9b-2e00-4f96-bf5b-94d9546eb76e"), "D4", 4, "D", new Guid("0e9abb4a-5aa5-4e6d-b94a-774a5fad4f10") },
                    { new Guid("e15efd3a-a5b5-e890-ae69-fb024abcc090"), "A2", 2, "A", new Guid("0e9abb4a-5aa5-4e6d-b94a-774a5fad4f10") },
                    { new Guid("eefc9193-e93c-1bfe-e620-45bc3ef83e86"), "B1", 1, "B", new Guid("70cb6f07-08e2-4664-8d45-334564cdd768") },
                    { new Guid("f0339936-3515-2a94-755e-7a865167078e"), "A1", 1, "A", new Guid("0e9abb4a-5aa5-4e6d-b94a-774a5fad4f10") }
                });

            migrationBuilder.CreateIndex(
                name: "IX_Inscricoes_AssentoId",
                table: "Inscricoes",
                column: "AssentoId");

            migrationBuilder.CreateIndex(
                name: "IX_Inscricoes_AtividadeId_AssentoId",
                table: "Inscricoes",
                columns: new[] { "AtividadeId", "AssentoId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Atividades_ResponsavelId",
                table: "Atividades",
                column: "ResponsavelId");

            migrationBuilder.CreateIndex(
                name: "IX_Atividades_SalaId",
                table: "Atividades",
                column: "SalaId");

            migrationBuilder.CreateIndex(
                name: "IX_Assentos_SalaId_Codigo",
                table: "Assentos",
                columns: new[] { "SalaId", "Codigo" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Assentos_SalaId_Fileira_Coluna",
                table: "Assentos",
                columns: new[] { "SalaId", "Fileira", "Coluna" },
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_Atividades_AspNetUsers_ResponsavelId",
                table: "Atividades",
                column: "ResponsavelId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Atividades_Salas_SalaId",
                table: "Atividades",
                column: "SalaId",
                principalTable: "Salas",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Inscricoes_Assentos_AssentoId",
                table: "Inscricoes",
                column: "AssentoId",
                principalTable: "Assentos",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Atividades_AspNetUsers_ResponsavelId",
                table: "Atividades");

            migrationBuilder.DropForeignKey(
                name: "FK_Atividades_Salas_SalaId",
                table: "Atividades");

            migrationBuilder.DropForeignKey(
                name: "FK_Inscricoes_Assentos_AssentoId",
                table: "Inscricoes");

            migrationBuilder.DropTable(
                name: "Assentos");

            migrationBuilder.DropTable(
                name: "Salas");

            migrationBuilder.DropIndex(
                name: "IX_Inscricoes_AssentoId",
                table: "Inscricoes");

            migrationBuilder.DropIndex(
                name: "IX_Inscricoes_AtividadeId_AssentoId",
                table: "Inscricoes");

            migrationBuilder.DropIndex(
                name: "IX_Atividades_ResponsavelId",
                table: "Atividades");

            migrationBuilder.DropIndex(
                name: "IX_Atividades_SalaId",
                table: "Atividades");

            migrationBuilder.DropColumn(
                name: "AssentoId",
                table: "Inscricoes");

            migrationBuilder.DropColumn(
                name: "Data",
                table: "Inscricoes");

            migrationBuilder.DropColumn(
                name: "Fim",
                table: "Atividades");

            migrationBuilder.DropColumn(
                name: "Inicio",
                table: "Atividades");

            migrationBuilder.DropColumn(
                name: "Modalidade",
                table: "Atividades");

            migrationBuilder.DropColumn(
                name: "ResponsavelId",
                table: "Atividades");

            migrationBuilder.DropColumn(
                name: "SalaId",
                table: "Atividades");
        }
    }
}
