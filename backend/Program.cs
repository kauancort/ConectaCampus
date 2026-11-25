using AprendizagemColaborativa.Api.Data;
using AprendizagemColaborativa.Api.Features.Auth;
using AprendizagemColaborativa.Api.Features.Atividades;
using AprendizagemColaborativa.Api.Models;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

var connectionString = RequiredSetting(builder.Configuration, "ConnectionStrings:DefaultConnection");

builder.Services.AddDbContext<ApplicationDbContext>(options => options.UseNpgsql(connectionString,
    postgres => postgres.MigrationsHistoryTable("__EFMigrationsHistory", ApplicationDbContext.Schema)));
builder.Services.AddScoped<AtividadeRepository>();
builder.Services.AddScoped<AtividadeService>();
builder.Services.AddScoped<AssentosService>();
builder.Services.AddScoped<InscricaoService>();
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
var frontendOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>()
    ?? ["http://localhost:5173", "http://127.0.0.1:5173"];
builder.Services.AddCors(options => options.AddPolicy("Frontend", policy =>
    policy.WithOrigins(frontendOrigins)
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
app.MapAcessoEndpoints();
app.MapAlunosEndpoints();
app.MapAtividadesEndpoints();
app.MapPropostasEndpoints();
app.MapAssentosEndpoints();
app.MapInscricoesEndpoints();

await using (var scope = app.Services.CreateAsyncScope())
{
    var database = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    await database.Database.MigrateAsync();
    if (args.Length == 2 && args[0] == "conceder-professor")
    {
        var users = scope.ServiceProvider.GetRequiredService<UserManager<Aluno>>();
        var user = await users.FindByEmailAsync(args[1]) ?? throw new InvalidOperationException("Conta não encontrada.");
        user.Professor = true;
        var result = await users.UpdateAsync(user);
        if (!result.Succeeded) throw new InvalidOperationException("Não foi possível atualizar a conta.");
        Console.WriteLine("Acesso de professor concedido.");
        return;
    }
    if (builder.Configuration.GetValue<bool>("DEMO_ACCOUNTS_ENABLED"))
    {
        await DemoAccountSeeder.SeedAsync(scope.ServiceProvider.GetRequiredService<UserManager<Aluno>>(), app.Logger);
        await CampusSeed.SeedAsync(database, scope.ServiceProvider.GetRequiredService<UserManager<Aluno>>(), app.Logger);
    }
}

await app.RunAsync();

static string RequiredSetting(IConfiguration configuration, string name) =>
    configuration[name]?.Trim() is { Length: > 0 } value
        ? value
        : throw new InvalidOperationException($"Configuração obrigatória ausente: {name}. Configure as variáveis de ambiente da API (use __ no lugar de :).");

public partial class Program { }
