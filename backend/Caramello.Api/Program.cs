using System.Security.Claims;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.RateLimiting;
using Caramello.Api.Domain;
using Caramello.Api.Endpoints;
using Caramello.Api.Infrastructure;
using Caramello.Api.Services;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);
var ambienteDesktop = builder.Environment.IsEnvironment("Desktop");
var desktopShutdownToken = Environment.GetEnvironmentVariable("CARAMELLO_DESKTOP_SHUTDOWN_TOKEN");
var servidorDesktop = ambienteDesktop && !string.IsNullOrWhiteSpace(desktopShutdownToken);
if (ambienteDesktop)
    builder.Logging.ClearProviders().AddSimpleConsole(options => options.ColorBehavior = Microsoft.Extensions.Logging.Console.LoggerColorBehavior.Disabled);
if (servidorDesktop)
{
    builder.WebHost.ConfigureKestrel(options =>
    {
        options.Configure(new ConfigurationBuilder().Build());
        options.Listen(IPAddress.Loopback, 0);
    });
}
var caminhoBanco = Environment.GetEnvironmentVariable("CARAMELLO_DATABASE_PATH")
    ?? Path.Combine(builder.Environment.ContentRootPath, "data", "caramello.db");
if (!Path.IsPathRooted(caminhoBanco)) caminhoBanco = Path.GetFullPath(caminhoBanco, builder.Environment.ContentRootPath);
Directory.CreateDirectory(Path.GetDirectoryName(caminhoBanco)!);
var connectionSqlite = new Microsoft.Data.Sqlite.SqliteConnectionStringBuilder { DataSource = caminhoBanco }.ToString();
var connection = builder.Configuration.GetConnectionString("Database") ?? connectionSqlite;
builder.Configuration["ConnectionStrings:Database"] = connection;
var caminhoChaves = Environment.GetEnvironmentVariable("CARAMELLO_KEYS_PATH")
    ?? Path.Combine(builder.Environment.ContentRootPath, "data", "keys");
if (!Path.IsPathRooted(caminhoChaves)) caminhoChaves = Path.GetFullPath(caminhoChaves, builder.Environment.ContentRootPath);
Directory.CreateDirectory(caminhoChaves);

builder.Services.AddHttpContextAccessor();
builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
    options.ForwardLimit = 1;
    options.KnownProxies.Add(IPAddress.Loopback);
    options.KnownProxies.Add(IPAddress.IPv6Loopback);
});
builder.Services.AddDataProtection().SetApplicationName("Caramello.GestaoSustentavel").PersistKeysToFileSystem(new DirectoryInfo(caminhoChaves));
builder.Services.AddDbContext<AppDbContext>(options => options.UseSqlite(connection));
builder.Services.AddIdentityCore<ApplicationUser>(options =>
{
    options.User.RequireUniqueEmail = true;
    options.Password.RequiredLength = 12;
    options.Password.RequireDigit = true;
    options.Password.RequireLowercase = true;
    options.Password.RequireUppercase = true;
    options.Password.RequireNonAlphanumeric = true;
    options.Lockout.MaxFailedAccessAttempts = 5;
    options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15);
})
    .AddRoles<IdentityRole>()
    .AddEntityFrameworkStores<AppDbContext>()
    .AddSignInManager()
    .AddDefaultTokenProviders();
builder.Services.AddAuthentication(IdentityConstants.ApplicationScheme).AddIdentityCookies();
builder.Services.ConfigureApplicationCookie(options =>
{
    options.Cookie.Name = "caramello.sessao";
    options.Cookie.HttpOnly = true;
    options.Cookie.SameSite = SameSiteMode.Strict;
    options.Cookie.SecurePolicy = builder.Environment.IsProduction() ? CookieSecurePolicy.Always : CookieSecurePolicy.None;
    options.ExpireTimeSpan = TimeSpan.FromHours(8);
    options.SlidingExpiration = true;
    options.Events.OnRedirectToLogin = context => { context.Response.StatusCode = StatusCodes.Status401Unauthorized; return Task.CompletedTask; };
    options.Events.OnRedirectToAccessDenied = context => { context.Response.StatusCode = StatusCodes.Status403Forbidden; return Task.CompletedTask; };
});
builder.Services.AddAuthorization(options =>
{
    var administrador = new AuthorizationPolicyBuilder().RequireAuthenticatedUser().RequireRole("Administrador").Build();
    options.AddPolicy("Administrador", administrador);
    options.FallbackPolicy = administrador;
});
builder.Services.AddAntiforgery(options =>
{
    options.HeaderName = "X-CSRF-TOKEN";
    options.Cookie.Name = "caramello.csrf";
    options.Cookie.HttpOnly = true;
    options.Cookie.SameSite = SameSiteMode.Strict;
    options.Cookie.SecurePolicy = builder.Environment.IsProduction() ? CookieSecurePolicy.Always : CookieSecurePolicy.None;
});
builder.Services.AddRateLimiter(options => options.AddPolicy("login", context =>
    RateLimitPartition.GetFixedWindowLimiter(context.Connection.RemoteIpAddress?.ToString() ?? "local", _ => new FixedWindowRateLimiterOptions
    {
        PermitLimit = 5,
        Window = TimeSpan.FromMinutes(1),
        QueueLimit = 0
    })));
builder.Services.AddScoped<IInsumoService, InsumoService>();
builder.Services.AddScoped<IDescarteService, DescarteService>();
builder.Services.AddScoped<IIndicadoresService, IndicadoresService>();
builder.Services.AddScoped<IBorraService, BorraService>();
builder.Services.AddScoped<IBackupService, BackupService>();
builder.Services.AddHostedService<BackupAgendadoService>();
builder.Services.ConfigureHttpJsonOptions(options => options.SerializerOptions.Converters.Add(new JsonStringEnumConverter()));
builder.Services.AddCors(options => options.AddPolicy("development", policy => policy.WithOrigins("http://localhost:5173").AllowAnyHeader().AllowAnyMethod().AllowCredentials()));
builder.Services.AddProblemDetails();

var app = builder.Build();
if (app.Environment.IsDevelopment()) app.UseDeveloperExceptionPage(); else app.UseExceptionHandler();
if (app.Environment.IsDevelopment()) app.UseCors("development");
app.UseForwardedHeaders();
if (app.Environment.IsProduction()) { app.UseHsts(); app.UseHttpsRedirection(); }
app.UseDefaultFiles();
app.UseStaticFiles();
app.UseRateLimiter();
app.UseAuthentication();
app.UseAuthorization();
app.Use(async (context, next) =>
{
    if (context.Request.Path.StartsWithSegments("/api") && context.Request.Path != "/api/desktop/shutdown" &&
        !HttpMethods.IsGet(context.Request.Method) && !HttpMethods.IsHead(context.Request.Method) && !HttpMethods.IsOptions(context.Request.Method))
    {
        try { await context.RequestServices.GetRequiredService<IAntiforgery>().ValidateRequestAsync(context); }
        catch (AntiforgeryValidationException)
        {
            context.Response.StatusCode = StatusCodes.Status400BadRequest;
            await context.Response.WriteAsJsonAsync(new { mensagem = "A validação de segurança expirou. Atualize a página e tente novamente." });
            return;
        }
    }
    await next(context);
});

if (args.Contains("--migrate-desktop", StringComparer.OrdinalIgnoreCase))
{
    using var scope = app.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    var pendentes = await db.Database.GetPendingMigrationsAsync();
    if (pendentes.Any())
    {
        if (File.Exists(caminhoBanco) && new FileInfo(caminhoBanco).Length > 0)
            await scope.ServiceProvider.GetRequiredService<IBackupService>().CriarAsync(CancellationToken.None);
        await db.Database.MigrateAsync();
    }
    return;
}

await CriarAdministradorInicialAsync(app.Services);
app.MapSegurancaEndpoints();
app.MapCaramelloEndpoints();
app.MapFase3Endpoints();
if (!string.IsNullOrWhiteSpace(desktopShutdownToken))
{
    app.MapPost("/api/desktop/shutdown", (HttpContext context, IHostApplicationLifetime lifetime) =>
    {
        var recebido = context.Request.Headers["X-Caramello-Desktop-Token"].ToString();
        var valido = recebido.Length == desktopShutdownToken.Length && CryptographicOperations.FixedTimeEquals(Encoding.UTF8.GetBytes(recebido), Encoding.UTF8.GetBytes(desktopShutdownToken));
        if (!valido) return Results.NotFound();
        lifetime.StopApplication();
        return Results.Accepted();
    }).AllowAnonymous();
}
app.MapFallbackToFile("index.html").AllowAnonymous();
if (servidorDesktop)
{
    await app.StartAsync();
    var enderecos = app.Services.GetRequiredService<IServer>().Features.Get<IServerAddressesFeature>();
    var endereco = new Uri(enderecos!.Addresses.Single());
    Console.WriteLine($"CARAMELLO_DESKTOP_PORT={endereco.Port}");
    await app.WaitForShutdownAsync();
}
else
{
    app.Run();
}

static async Task CriarAdministradorInicialAsync(IServiceProvider services)
{
    var email = Environment.GetEnvironmentVariable("CARAMELLO_BOOTSTRAP_ADMIN_EMAIL")?.Trim();
    var senha = Environment.GetEnvironmentVariable("CARAMELLO_BOOTSTRAP_ADMIN_PASSWORD");
    if (string.IsNullOrWhiteSpace(email) && string.IsNullOrWhiteSpace(senha)) return;
    if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(senha))
        throw new InvalidOperationException("Defina juntas as variáveis de criação do administrador.");

    using var scope = services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    if ((await db.Database.GetPendingMigrationsAsync()).Any())
        throw new InvalidOperationException("Aplique as migrations antes de criar o administrador inicial.");
    var users = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
    var roles = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
    if (await users.Users.AnyAsync())
        throw new InvalidOperationException("A criação inicial foi recusada porque já existe uma conta. Remova as variáveis temporárias.");
    if (!await roles.RoleExistsAsync("Administrador"))
    {
        var roleResult = await roles.CreateAsync(new IdentityRole("Administrador"));
        if (!roleResult.Succeeded) throw new InvalidOperationException(string.Join(" ", roleResult.Errors.Select(x => x.Description)));
    }
    var user = new ApplicationUser { UserName = email, Email = email, EmailConfirmed = true };
    var result = await users.CreateAsync(user, senha);
    if (!result.Succeeded) throw new InvalidOperationException(string.Join(" ", result.Errors.Select(x => x.Description)));
    await users.AddToRoleAsync(user, "Administrador");
}

public partial class Program;
