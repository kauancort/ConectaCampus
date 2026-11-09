namespace AprendizagemColaborativa.Api.Features.Auth;

public sealed record RegisterRequest(string? Name, string? Email, string? Course, string? Password);

public sealed record LoginRequest(string? Email, string? Password);

public sealed record StudentResponse(string Id, string Name, string Email, string Course);

public sealed record LoginResponse(string AccessToken, string TokenType, DateTimeOffset ExpiresAtUtc, StudentResponse Student);

public sealed record JwtSettings(string Issuer, string Audience, Microsoft.IdentityModel.Tokens.SymmetricSecurityKey SigningKey, TimeSpan Lifetime);
