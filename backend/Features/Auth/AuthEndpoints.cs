using System.ComponentModel.DataAnnotations;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using AprendizagemColaborativa.Api.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Npgsql;

namespace AprendizagemColaborativa.Api.Features.Auth;

public static class AuthEndpoints
{
    private const int MaxNameLength = 200;
    private const int MaxCourseLength = 120;
    private const int MaxEmailLength = 256;

    public static void MapAuthEndpoints(this IEndpointRouteBuilder app, JwtSettings jwt)
    {
        var group = app.MapGroup("/api/auth").WithTags("Auth");

        group.MapPost("/register", async (RegisterRequest? request, UserManager<Aluno> users) =>
        {
            var errors = ValidateRegister(request);
            if (errors.Count > 0)
            {
                return Results.ValidationProblem(errors, title: "Revise os dados informados.");
            }

            var name = request!.Name!.Trim();
            var email = request.Email!.Trim();
            var course = request.Course!.Trim();
            if (await users.FindByEmailAsync(email) is not null)
            {
                return Results.Conflict(new { message = "Já existe uma conta com este e-mail." });
            }

            var student = new Aluno
            {
                UserName = email,
                Email = email,
                Nome = name,
                Curso = course,
                TipoConta = request.AccountType?.Trim().ToLowerInvariant() ?? TiposConta.Aluno
            };

            try
            {
                var result = await users.CreateAsync(student, request.Password!);
                if (!result.Succeeded)
                {
                    if (result.Errors.Any(error => error.Code is "DuplicateEmail" or "DuplicateUserName"))
                    {
                        return Results.Conflict(new { message = "Já existe uma conta com este e-mail." });
                    }

                    return Results.ValidationProblem(result.Errors
                        .GroupBy(error => error.Code)
                        .ToDictionary(grouping => grouping.Key, grouping => grouping.Select(TranslateIdentityError).ToArray()),
                        title: "Não foi possível concluir o cadastro.");
                }
            }
            catch (DbUpdateException exception) when (IsDuplicateKey(exception))
            {
                return Results.Conflict(new { message = "Já existe uma conta com este e-mail." });
            }

            return Results.Json(ToResponse(student), statusCode: StatusCodes.Status201Created);
        });

        group.MapPost("/login", async (LoginRequest? request, UserManager<Aluno> users) =>
        {
            var errors = ValidateLogin(request);
            if (errors.Count > 0)
            {
                return Results.ValidationProblem(errors, title: "Revise os dados informados.");
            }

            var email = request!.Email!.Trim();
            var student = await users.FindByEmailAsync(email);
            if (student is null || !await users.CheckPasswordAsync(student, request.Password!))
            {
                return Results.Json(new { message = "E-mail ou senha inválidos." }, statusCode: StatusCodes.Status401Unauthorized);
            }

            var now = DateTimeOffset.UtcNow;
            var expires = now.Add(jwt.Lifetime);
            var claims = new[]
            {
                new Claim(JwtRegisteredClaimNames.Sub, student.Id),
                new Claim(JwtRegisteredClaimNames.Email, student.Email!),
                new Claim("name", student.Nome),
                new Claim("account_type", student.TipoConta)
            };
            var token = new JwtSecurityToken(
                issuer: jwt.Issuer,
                audience: jwt.Audience,
                claims: claims,
                notBefore: now.UtcDateTime,
                expires: expires.UtcDateTime,
                signingCredentials: new SigningCredentials(jwt.SigningKey, SecurityAlgorithms.HmacSha256));

            return Results.Ok(new LoginResponse(
                new JwtSecurityTokenHandler().WriteToken(token),
                "Bearer",
                expires,
                ToResponse(student)));
        });

        group.MapGet("/me", async (ClaimsPrincipal principal, UserManager<Aluno> users) =>
        {
            var studentId = principal.FindFirstValue(JwtRegisteredClaimNames.Sub);
            if (string.IsNullOrWhiteSpace(studentId))
            {
                return Results.Unauthorized();
            }

            var student = await users.FindByIdAsync(studentId);
            return student is null ? Results.Unauthorized() : Results.Ok(ToResponse(student));
        }).RequireAuthorization();
        group.MapPut("/me", async (UpdateProfileRequest? request, ClaimsPrincipal principal, UserManager<Aluno> users) =>
        {
            var id = principal.FindFirstValue(JwtRegisteredClaimNames.Sub);
            if (string.IsNullOrWhiteSpace(id)) return Results.Unauthorized();
            var student = await users.FindByIdAsync(id);
            if (student is null) return Results.Unauthorized();
            var errors = new Dictionary<string, string[]>();
            AddRequired(errors, "name", request?.Name);
            AddRequired(errors, "course", request?.Course);
            if (request?.Name?.Trim().Length > MaxNameLength) errors["name"] = [$"O nome deve ter no máximo {MaxNameLength} caracteres."];
            if (request?.Course?.Trim().Length > MaxCourseLength) errors["course"] = [$"O curso deve ter no máximo {MaxCourseLength} caracteres."];
            if (errors.Count > 0) return Results.ValidationProblem(errors);
            student.Nome = request!.Name!.Trim();
            student.Curso = request.Course!.Trim();
            var result = await users.UpdateAsync(student);
            if (!result.Succeeded) return Results.Conflict(new { message = "Não foi possível atualizar o perfil. Tente novamente." });
            return Results.Ok(ToResponse(student));
        }).RequireAuthorization();
    }

    private static Dictionary<string, string[]> ValidateRegister(RegisterRequest? request)
    {
        var errors = new Dictionary<string, string[]>();
        if (request is null)
        {
            return new() { ["body"] = ["O corpo da requisição é obrigatório."] };
        }

        AddRequired(errors, "name", request.Name);
        AddRequired(errors, "email", request.Email);
        AddRequired(errors, "course", request.Course);
        AddRequired(errors, "password", request.Password);

        if (request.AccountType is not null &&
            request.AccountType.Trim().ToLowerInvariant() is not (TiposConta.Aluno or TiposConta.Professor))
        {
            errors["accountType"] = ["Escolha aluno ou professor para o tipo de conta."];
        }

        if (!string.IsNullOrWhiteSpace(request.Email))
        {
            var email = request.Email.Trim();
            if (!new EmailAddressAttribute().IsValid(email))
            {
                errors["email"] = ["Informe um endereço de e-mail válido."];
            }
            else if (email.Length > MaxEmailLength)
            {
                errors["email"] = [$"O e-mail deve ter no máximo {MaxEmailLength} caracteres."];
            }
        }

        if (!string.IsNullOrWhiteSpace(request.Name) && request.Name.Trim().Length > MaxNameLength)
        {
            errors["name"] = [$"O nome deve ter no máximo {MaxNameLength} caracteres."];
        }

        if (!string.IsNullOrWhiteSpace(request.Course) && request.Course.Trim().Length > MaxCourseLength)
        {
            errors["course"] = [$"O curso deve ter no máximo {MaxCourseLength} caracteres."];
        }

        return errors;
    }

    private static Dictionary<string, string[]> ValidateLogin(LoginRequest? request)
    {
        var errors = new Dictionary<string, string[]>();
        if (request is null)
        {
            return new() { ["body"] = ["O corpo da requisição é obrigatório."] };
        }

        AddRequired(errors, "email", request.Email);
        AddRequired(errors, "password", request.Password);
        if (!string.IsNullOrWhiteSpace(request.Email) && !new EmailAddressAttribute().IsValid(request.Email.Trim()))
        {
            errors["email"] = ["Informe um endereço de e-mail válido."];
        }

        return errors;
    }

    private static void AddRequired(IDictionary<string, string[]> errors, string field, string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            errors[field] = ["Este campo é obrigatório."];
        }
    }

    private static StudentResponse ToResponse(Aluno student) =>
        new(student.Id, student.Nome, student.Email!, student.Curso, student.Professor, student.PodeMinistrar, student.TipoConta);

    private static string TranslateIdentityError(IdentityError error) => error.Code switch
    {
        "DuplicateEmail" or "DuplicateUserName" => "Já existe uma conta com este e-mail.",
        "InvalidEmail" => "Informe um endereço de e-mail válido.",
        "PasswordTooShort" => "A senha não atende ao tamanho mínimo exigido.",
        "PasswordRequiresNonAlphanumeric" => "A senha deve conter ao menos um caractere especial.",
        "PasswordRequiresDigit" => "A senha deve conter ao menos um número.",
        "PasswordRequiresLower" => "A senha deve conter ao menos uma letra minúscula.",
        "PasswordRequiresUpper" => "A senha deve conter ao menos uma letra maiúscula.",
        _ => "Não foi possível criar a conta. Confira os dados e tente novamente."
    };

    private static bool IsDuplicateKey(DbUpdateException exception) =>
        exception.InnerException is PostgresException { SqlState: PostgresErrorCodes.UniqueViolation };
}
