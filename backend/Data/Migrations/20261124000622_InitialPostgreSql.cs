using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace AprendizagemColaborativa.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class InitialPostgreSql : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "conectacampus");

            // As tabelas da aplicação e do Identity são acessadas somente pela API.
            migrationBuilder.Sql("REVOKE ALL ON SCHEMA conectacampus FROM PUBLIC;");

            migrationBuilder.CreateTable(
                name: "AspNetUsers",
                schema: "conectacampus",
                columns: table => new
                {
                    Id = table.Column<string>(type: "text", nullable: false),
                    Nome = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Curso = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    UserName = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    NormalizedUserName = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    Email = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    NormalizedEmail = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    EmailConfirmed = table.Column<bool>(type: "boolean", nullable: false),
                    PasswordHash = table.Column<string>(type: "text", nullable: true),
                    SecurityStamp = table.Column<string>(type: "text", nullable: true),
                    ConcurrencyStamp = table.Column<string>(type: "text", nullable: true),
                    PhoneNumber = table.Column<string>(type: "text", nullable: true),
                    PhoneNumberConfirmed = table.Column<bool>(type: "boolean", nullable: false),
                    TwoFactorEnabled = table.Column<bool>(type: "boolean", nullable: false),
                    LockoutEnd = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    LockoutEnabled = table.Column<bool>(type: "boolean", nullable: false),
                    AccessFailedCount = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetUsers", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Salas",
                schema: "conectacampus",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Nome = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    Tipo = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    Capacidade = table.Column<int>(type: "integer", nullable: false),
                    Status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Salas", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AspNetUserClaims",
                schema: "conectacampus",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    UserId = table.Column<string>(type: "text", nullable: false),
                    ClaimType = table.Column<string>(type: "text", nullable: true),
                    ClaimValue = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetUserClaims", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AspNetUserClaims_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalSchema: "conectacampus",
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AspNetUserLogins",
                schema: "conectacampus",
                columns: table => new
                {
                    LoginProvider = table.Column<string>(type: "text", nullable: false),
                    ProviderKey = table.Column<string>(type: "text", nullable: false),
                    ProviderDisplayName = table.Column<string>(type: "text", nullable: true),
                    UserId = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetUserLogins", x => new { x.LoginProvider, x.ProviderKey });
                    table.ForeignKey(
                        name: "FK_AspNetUserLogins_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalSchema: "conectacampus",
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AspNetUserTokens",
                schema: "conectacampus",
                columns: table => new
                {
                    UserId = table.Column<string>(type: "text", nullable: false),
                    LoginProvider = table.Column<string>(type: "text", nullable: false),
                    Name = table.Column<string>(type: "text", nullable: false),
                    Value = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetUserTokens", x => new { x.UserId, x.LoginProvider, x.Name });
                    table.ForeignKey(
                        name: "FK_AspNetUserTokens_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalSchema: "conectacampus",
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Assentos",
                schema: "conectacampus",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    SalaId = table.Column<Guid>(type: "uuid", nullable: false),
                    Codigo = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    Fileira = table.Column<string>(type: "character varying(1)", maxLength: 1, nullable: false),
                    Coluna = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Assentos", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Assentos_Salas_SalaId",
                        column: x => x.SalaId,
                        principalSchema: "conectacampus",
                        principalTable: "Salas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Atividades",
                schema: "conectacampus",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Modalidade = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: true),
                    SalaId = table.Column<Guid>(type: "uuid", nullable: true),
                    ResponsavelId = table.Column<string>(type: "text", nullable: true),
                    Inicio = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Fim = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    OrganizadorId = table.Column<string>(type: "text", nullable: false),
                    Tipo = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    Tema = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Descricao = table.Column<string>(type: "character varying(5000)", maxLength: 5000, nullable: false),
                    Nivel = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    DataHoraUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Local = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                    NumeroDeVagas = table.Column<int>(type: "integer", nullable: false),
                    Status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Atividades", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Atividades_AspNetUsers_OrganizadorId",
                        column: x => x.OrganizadorId,
                        principalSchema: "conectacampus",
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Atividades_AspNetUsers_ResponsavelId",
                        column: x => x.ResponsavelId,
                        principalSchema: "conectacampus",
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Atividades_Salas_SalaId",
                        column: x => x.SalaId,
                        principalSchema: "conectacampus",
                        principalTable: "Salas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Inscricoes",
                schema: "conectacampus",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    AtividadeId = table.Column<Guid>(type: "uuid", nullable: false),
                    AlunoId = table.Column<string>(type: "text", nullable: false),
                    AssentoId = table.Column<Guid>(type: "uuid", nullable: true),
                    Data = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    CriadaEmUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    AtualizadaEmUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Inscricoes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Inscricoes_AspNetUsers_AlunoId",
                        column: x => x.AlunoId,
                        principalSchema: "conectacampus",
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Inscricoes_Assentos_AssentoId",
                        column: x => x.AssentoId,
                        principalSchema: "conectacampus",
                        principalTable: "Assentos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Inscricoes_Atividades_AtividadeId",
                        column: x => x.AtividadeId,
                        principalSchema: "conectacampus",
                        principalTable: "Atividades",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.InsertData(
                schema: "conectacampus",
                table: "Salas",
                columns: new[] { "Id", "Capacidade", "Nome", "Status", "Tipo" },
                values: new object[,]
                {
                    { new Guid("0e9abb4a-5aa5-4e6d-b94a-774a5fad4f10"), 24, "Laboratório Desktop", "disponivel", "DESKTOP" },
                    { new Guid("70cb6f07-08e2-4664-8d45-334564cdd768"), 20, "Laboratório Notebook", "disponivel", "NOTEBOOK" }
                });

            migrationBuilder.InsertData(
                schema: "conectacampus",
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
                name: "IX_AspNetUserClaims_UserId",
                schema: "conectacampus",
                table: "AspNetUserClaims",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_AspNetUserLogins_UserId",
                schema: "conectacampus",
                table: "AspNetUserLogins",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "EmailIndex",
                schema: "conectacampus",
                table: "AspNetUsers",
                column: "NormalizedEmail",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "UserNameIndex",
                schema: "conectacampus",
                table: "AspNetUsers",
                column: "NormalizedUserName",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Assentos_SalaId_Codigo",
                schema: "conectacampus",
                table: "Assentos",
                columns: new[] { "SalaId", "Codigo" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Assentos_SalaId_Fileira_Coluna",
                schema: "conectacampus",
                table: "Assentos",
                columns: new[] { "SalaId", "Fileira", "Coluna" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Atividades_DataHoraUtc",
                schema: "conectacampus",
                table: "Atividades",
                column: "DataHoraUtc");

            migrationBuilder.CreateIndex(
                name: "IX_Atividades_OrganizadorId",
                schema: "conectacampus",
                table: "Atividades",
                column: "OrganizadorId");

            migrationBuilder.CreateIndex(
                name: "IX_Atividades_ResponsavelId",
                schema: "conectacampus",
                table: "Atividades",
                column: "ResponsavelId");

            migrationBuilder.CreateIndex(
                name: "IX_Atividades_SalaId",
                schema: "conectacampus",
                table: "Atividades",
                column: "SalaId");

            migrationBuilder.CreateIndex(
                name: "IX_Inscricoes_AlunoId",
                schema: "conectacampus",
                table: "Inscricoes",
                column: "AlunoId");

            migrationBuilder.CreateIndex(
                name: "IX_Inscricoes_AssentoId",
                schema: "conectacampus",
                table: "Inscricoes",
                column: "AssentoId");

            migrationBuilder.CreateIndex(
                name: "IX_Inscricoes_AtividadeId_AlunoId",
                schema: "conectacampus",
                table: "Inscricoes",
                columns: new[] { "AtividadeId", "AlunoId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Inscricoes_AtividadeId_AssentoId",
                schema: "conectacampus",
                table: "Inscricoes",
                columns: new[] { "AtividadeId", "AssentoId" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AspNetUserClaims",
                schema: "conectacampus");

            migrationBuilder.DropTable(
                name: "AspNetUserLogins",
                schema: "conectacampus");

            migrationBuilder.DropTable(
                name: "AspNetUserTokens",
                schema: "conectacampus");

            migrationBuilder.DropTable(
                name: "Inscricoes",
                schema: "conectacampus");

            migrationBuilder.DropTable(
                name: "Assentos",
                schema: "conectacampus");

            migrationBuilder.DropTable(
                name: "Atividades",
                schema: "conectacampus");

            migrationBuilder.DropTable(
                name: "AspNetUsers",
                schema: "conectacampus");

            migrationBuilder.DropTable(
                name: "Salas",
                schema: "conectacampus");
        }
    }
}
