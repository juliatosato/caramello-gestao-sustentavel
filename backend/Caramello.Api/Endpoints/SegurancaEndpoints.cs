using Caramello.Api.Domain;
using Caramello.Api.Infrastructure;
using Caramello.Api.Services;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using System.Net;

namespace Caramello.Api.Endpoints;

public static class SegurancaEndpoints
{
    public static IEndpointRouteBuilder MapSegurancaEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet("/api/auth/csrf", (IAntiforgery antiforgery, HttpContext context) =>
        {
            var tokens = antiforgery.GetAndStoreTokens(context);
            return Results.Ok(new { token = tokens.RequestToken });
        }).AllowAnonymous();

        app.MapGet("/api/auth/setup-status", async (UserManager<ApplicationUser> users, IWebHostEnvironment environment, HttpContext context) =>
            Results.Ok(new { necessario = SetupPermitido(environment, context) && !await users.Users.AnyAsync() })).AllowAnonymous();

        app.MapPost("/api/auth/setup", async (SetupInput input, UserManager<ApplicationUser> users, RoleManager<IdentityRole> roles, SignInManager<ApplicationUser> signIn, IWebHostEnvironment environment, HttpContext context) =>
        {
            if (!SetupPermitido(environment, context)) return Results.NotFound();
            if (await users.Users.AnyAsync()) return Results.NotFound();
            var email = input.Email?.Trim();
            if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(input.Senha)) return Results.BadRequest(new { mensagem = "Informe e-mail e senha." });
            if (!await roles.RoleExistsAsync("Administrador"))
            {
                var roleResult = await roles.CreateAsync(new IdentityRole("Administrador"));
                if (!roleResult.Succeeded) return Results.BadRequest(new { mensagem = string.Join(" ", roleResult.Errors.Select(x => x.Description)) });
            }
            var user = new ApplicationUser { UserName = email, Email = email, EmailConfirmed = true };
            var result = await users.CreateAsync(user, input.Senha);
            if (!result.Succeeded) return Results.BadRequest(new { mensagem = string.Join(" ", result.Errors.Select(x => x.Description)) });
            await users.AddToRoleAsync(user, "Administrador");
            await signIn.SignInAsync(user, false);
            return Results.Ok(new { email });
        }).AllowAnonymous().RequireRateLimiting("login");

        app.MapPost("/api/auth/login", async (LoginInput input, SignInManager<ApplicationUser> signIn) =>
        {
            var email = input.Email?.Trim();
            if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(input.Senha))
                return Results.BadRequest(new { mensagem = "Informe e-mail e senha." });
            var resultado = await signIn.PasswordSignInAsync(email, input.Senha, false, true);
            return resultado.Succeeded
                ? Results.Ok(new { mensagem = "Login realizado." })
                : Results.Json(new { mensagem = "E-mail ou senha inválidos." }, statusCode: StatusCodes.Status401Unauthorized);
        }).AllowAnonymous().RequireRateLimiting("login");

        app.MapGet("/api/auth/me", (HttpContext context) => Results.Ok(new
        {
            email = context.User.Identity!.Name,
            papeis = context.User.Claims.Where(x => x.Type.EndsWith("/role")).Select(x => x.Value)
        }));

        app.MapPost("/api/auth/logout", async (SignInManager<ApplicationUser> signIn) =>
        {
            await signIn.SignOutAsync();
            return Results.NoContent();
        });

        app.MapPost("/api/auth/trocar-senha", async (TrocaSenhaInput input, UserManager<ApplicationUser> users, HttpContext context) =>
        {
            var user = await users.GetUserAsync(context.User);
            if (user is null) return Results.Unauthorized();
            var resultado = await users.ChangePasswordAsync(user, input.SenhaAtual ?? "", input.NovaSenha ?? "");
            return resultado.Succeeded ? Results.NoContent() : Results.BadRequest(new { mensagem = string.Join(" ", resultado.Errors.Select(x => x.Description)) });
        });

        app.MapGet("/api/auditoria", async (AppDbContext db, int? limite, CancellationToken ct) =>
            Results.Ok(await db.RegistrosAuditoria.AsNoTracking().OrderByDescending(x => x.DataUtc).Take(Math.Clamp(limite ?? 100, 1, 500)).ToListAsync(ct)));

        app.MapGet("/api/backups", (IBackupService service) => Results.Ok(service.Listar()));
        app.MapPost("/api/backups", async (IBackupService service, CancellationToken ct) => Results.Ok(await service.CriarAsync(ct)));
        return app;
    }

    private static bool SetupPermitido(IWebHostEnvironment environment, HttpContext context)
    {
        if (!environment.IsEnvironment("Desktop") && !environment.IsDevelopment()) return false;
        var ip = context.Connection.RemoteIpAddress;
        return ip is null || IPAddress.IsLoopback(ip);
    }
}

public sealed record LoginInput(string? Email, string? Senha);
public sealed record TrocaSenhaInput(string? SenhaAtual, string? NovaSenha);
public sealed record SetupInput(string? Email, string? Senha);
