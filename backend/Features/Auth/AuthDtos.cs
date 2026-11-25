namespace AprendizagemColaborativa.Api.Features.Auth;

public sealed record RegisterRequest(string? Name, string? Email, string? Course, string? Password, string? AccountType = null);

public sealed record LoginRequest(string? Email, string? Password);
public sealed record UpdateProfileRequest(string? Name, string? Course);

<<<<<<< HEAD
public sealed record StudentResponse(string Id, string Name, string Email, string Course, bool Professor, bool PodeMinistrar);
=======
public sealed record StudentResponse(string Id, string Name, string Email, string Course, string AccountType);
>>>>>>> 1524e16cfcfef7673dca4642fc242c4617009a43

public sealed record LoginResponse(string AccessToken, string TokenType, DateTimeOffset ExpiresAtUtc, StudentResponse Student);

public sealed record JwtSettings(string Issuer, string Audience, Microsoft.IdentityModel.Tokens.SymmetricSecurityKey SigningKey, TimeSpan Lifetime);
