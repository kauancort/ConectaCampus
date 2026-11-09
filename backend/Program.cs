using AprendizagemColaborativa.Api.Data;
using AprendizagemColaborativa.Api.Features.Auth;
using AprendizagemColaborativa.Api.Features.Atividades;
using AprendizagemColaborativa.Api.Models;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using MySql.Data.MySqlClient;
using Microsoft.IdentityModel.Tokens;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
if (string.IsNullOrWhiteSpace(connectionString))
{
    var settings = new MySqlConnectionStringBuilder
    {
        Server = builder.Configuration["MYSQL_HOST"]?.Trim() ?? "127.0.0.1",
        Port = uint.TryParse(builder.Configuration["MYSQL_PORT"]?.Trim(), out var port) ? port : 3306,
        Database = RequiredSetting(builder.Configuration, "MYSQL_DATABASE"),
        UserID = RequiredSetting(builder.Configuration, "MYSQL_USER"),
        Password = RequiredSetting(builder.Configuration, "MYSQL_PASSWORD")
    };
    connectionString = settings.ConnectionString;
}

builder.Services.AddDbContext<ApplicationDbContext>(options => options.UseMySQL(connectionString));
var jwtIssuer = RequiredSetting(builder.Configuration, "JWT_ISSUER");
var jwtAudience = RequiredSetting(builder.Configuration, "JWT_AUDIENCE");
var jwtSigningKey = RequiredSetting(builder.Configuration, "JWT_SIGNING_KEY");
if (Encoding.UTF8.GetByteCount(jwtSigningKey) < 32)
{
    throw new InvalidOperationException("JWT_SIGNING_KEY deve conter pelo menos 32 bytes UTF-8.");
}

var jwtExpirationMinutes = int.TryParse(builder.Configuration["JWT_EXPIRATION_MINUTES"]?.Trim(), out var configuredMinutes)
    ? configuredMinutes
    : 60;
if (jwtExpirationMinutes <= 0)
{
    throw new InvalidOperationException("JWT_EXPIRATION_MINUTES deve ser um número inteiro positivo.");
}

var jwtKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSigningKey));

builder.Services.AddIdentityCore<Aluno>(options => options.User.RequireUniqueEmail = true)
    .AddEntityFrameworkStores<ApplicationDbContext>();
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.MapInboundClaims = false;
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = jwtIssuer,
            ValidateAudience = true,
            ValidAudience = jwtAudience,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = jwtKey,
            ValidateLifetime = true,
            ClockSkew = TimeSpan.FromSeconds(30),
            NameClaimType = "name"
        };
    });
builder.Services.AddAuthorization();
builder.Services.AddOpenApi();
builder.Services.AddProblemDetails(options =>
{
    options.CustomizeProblemDetails = context =>
    {
        var statusCode = context.HttpContext.Response.StatusCode;
        context.ProblemDetails.Title = statusCode switch
        {
            StatusCodes.Status400BadRequest => "Requisição inválida.",
            StatusCodes.Status401Unauthorized => "Autenticação necessária.",
            StatusCodes.Status403Forbidden => "Acesso negado.",
            StatusCodes.Status404NotFound => "Recurso não encontrado.",
            StatusCodes.Status409Conflict => "Conflito ao processar a solicitação.",
            _ => "Erro interno do servidor."
        };
        context.ProblemDetails.Detail = statusCode switch
        {
            StatusCodes.Status400BadRequest => "Confira os dados enviados e tente novamente.",
            StatusCodes.Status401Unauthorized => "Entre na sua conta para continuar.",
            StatusCodes.Status403Forbidden => "Você não tem permissão para realizar esta ação.",
            StatusCodes.Status404NotFound => "O recurso solicitado não foi encontrado.",
            StatusCodes.Status409Conflict => "A solicitação não pode ser concluída no estado atual.",
            _ => "Ocorreu um erro inesperado. Tente novamente."
        };
    };
});
builder.Services.AddCors(options => options.AddPolicy("Frontend", policy =>
    policy.WithOrigins("http://localhost:5173", "http://127.0.0.1:5173")
        .AllowAnyHeader()
        .AllowAnyMethod()));

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler();
}
app.UseStatusCodePages();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseCors("Frontend");
app.UseAuthentication();
app.UseAuthorization();

app.MapGet("/health", () => Results.Ok(new { status = "ok" }));
app.MapAuthEndpoints(new JwtSettings(jwtIssuer, jwtAudience, jwtKey, TimeSpan.FromMinutes(jwtExpirationMinutes)));
app.MapAtividadesEndpoints();
app.MapInscricoesEndpoints();

await using (var scope = app.Services.CreateAsyncScope())
{
    var database = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    await database.Database.MigrateAsync();
}

await app.RunAsync();

static string RequiredSetting(IConfiguration configuration, string name) =>
    configuration[name]?.Trim() is { Length: > 0 } value
        ? value
        : throw new InvalidOperationException($"Configuração obrigatória ausente: {name}. Configure o arquivo .env antes de iniciar a API.");

public partial class Program { }
