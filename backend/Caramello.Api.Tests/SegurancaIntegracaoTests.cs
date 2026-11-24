using System.Net;
using System.Net.Http.Json;
using Caramello.Api.Domain;
using Caramello.Api.Infrastructure;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Xunit;

namespace Caramello.Api.Tests;

public sealed class SegurancaIntegracaoTests : IClassFixture<SegurancaFactory>
{
    private readonly SegurancaFactory _factory;
    public SegurancaIntegracaoTests(SegurancaFactory factory) => _factory = factory;

    [Fact]
    public async Task EndpointAdministrativoSemSessaoRetorna401()
    {
        using var client = _factory.CreateClient();
        Assert.Equal(HttpStatusCode.Unauthorized, (await client.GetAsync("/api/insumos")).StatusCode);
    }

    [Fact]
    public async Task LoginSemCsrfEhBloqueado()
    {
        using var client = _factory.CreateClient();
        var response = await client.PostAsJsonAsync("/api/auth/login", new { email = "admin@teste.local", senha = "Senha-Forte-123" });
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task LoginValidoPermiteAcessoELogoutEncerraSessao()
    {
        using var client = _factory.CreateClient(new WebApplicationFactoryClientOptions { HandleCookies = false });
        await _factory.CriarUsuarioAsync("admin@teste.local", "Senha-Forte-123", true);
        var (token, csrfCookie) = await ObterCsrf(client);
        using var login = new HttpRequestMessage(HttpMethod.Post, "/api/auth/login") { Content = JsonContent.Create(new { email = "admin@teste.local", senha = "Senha-Forte-123" }) };
        login.Headers.Add("X-CSRF-TOKEN", token); login.Headers.Add("Cookie", csrfCookie);
        var loginResponse = await client.SendAsync(login);
        Assert.True(loginResponse.StatusCode == HttpStatusCode.OK, await loginResponse.Content.ReadAsStringAsync());
        var authCookie = loginResponse.Headers.GetValues("Set-Cookie").Single(x => x.StartsWith("caramello.sessao=")).Split(';')[0];
        using var acesso = new HttpRequestMessage(HttpMethod.Get, "/api/insumos"); acesso.Headers.Add("Cookie", authCookie);
        Assert.Equal(HttpStatusCode.OK, (await client.SendAsync(acesso)).StatusCode);
        (token, csrfCookie) = await ObterCsrf(client, authCookie);
        using var logout = new HttpRequestMessage(HttpMethod.Post, "/api/auth/logout"); logout.Headers.Add("X-CSRF-TOKEN", token); logout.Headers.Add("Cookie", $"{csrfCookie}; {authCookie}");
        Assert.Equal(HttpStatusCode.NoContent, (await client.SendAsync(logout)).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await client.GetAsync("/api/insumos")).StatusCode);
    }

    [Fact]
    public async Task UsuarioSemPapelRecebe403()
    {
        using var client = _factory.CreateClient(new WebApplicationFactoryClientOptions { HandleCookies = false });
        await _factory.CriarUsuarioAsync("sem-papel@teste.local", "Senha-Forte-123", false);
        var (token, csrfCookie) = await ObterCsrf(client);
        using var login = new HttpRequestMessage(HttpMethod.Post, "/api/auth/login") { Content = JsonContent.Create(new { email = "sem-papel@teste.local", senha = "Senha-Forte-123" }) };
        login.Headers.Add("X-CSRF-TOKEN", token); login.Headers.Add("Cookie", csrfCookie);
        var loginResponse = await client.SendAsync(login);
        Assert.True(loginResponse.StatusCode == HttpStatusCode.OK, await loginResponse.Content.ReadAsStringAsync());
        var authCookie = loginResponse.Headers.GetValues("Set-Cookie").Single(x => x.StartsWith("caramello.sessao=")).Split(';')[0];
        using var acesso = new HttpRequestMessage(HttpMethod.Get, "/api/insumos"); acesso.Headers.Add("Cookie", authCookie);
        Assert.Equal(HttpStatusCode.Forbidden, (await client.SendAsync(acesso)).StatusCode);
    }

    [Fact]
    public async Task LoginInvalidoNaoInformaSeContaExiste()
    {
        using var client = _factory.CreateClient();
        var (token, csrfCookie) = await ObterCsrf(client);
        using var login = new HttpRequestMessage(HttpMethod.Post, "/api/auth/login") { Content = JsonContent.Create(new { email = "naoexiste@teste.local", senha = "Senha-Errada-123" }) };
        login.Headers.Add("X-CSRF-TOKEN", token); login.Headers.Add("Cookie", csrfCookie);
        var response = await client.SendAsync(login);
        Assert.True(response.StatusCode == HttpStatusCode.Unauthorized, await response.Content.ReadAsStringAsync());
        Assert.Contains("E-mail ou senha inválidos", await response.Content.ReadAsStringAsync());
    }

    private static async Task<(string Token, string Cookie)> ObterCsrf(HttpClient client, string? authCookie = null)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, "/api/auth/csrf");
        if (authCookie is not null) request.Headers.Add("Cookie", authCookie);
        var response = await client.SendAsync(request);
        Assert.True(response.IsSuccessStatusCode, $"{response.StatusCode}: {await response.Content.ReadAsStringAsync()}");
        var cookie = response.Headers.GetValues("Set-Cookie").Single(x => x.StartsWith("caramello.csrf=")).Split(';')[0];
        return ((await response.Content.ReadFromJsonAsync<CsrfResposta>())!.Token, cookie);
    }
    private sealed record CsrfResposta(string Token);
}

public sealed class ConfiguracaoInicialIntegracaoTests
{
    [Fact]
    public async Task PrimeiraContaEhCriadaUmaUnicaVezComoAdministradora()
    {
        using var factory = new SegurancaFactory();
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions { HandleCookies = false });
        var csrfResponse = await client.GetAsync("/api/auth/csrf");
        var token = (await csrfResponse.Content.ReadFromJsonAsync<CsrfResposta>())!.Token;
        var cookie = csrfResponse.Headers.GetValues("Set-Cookie").Single(x => x.StartsWith("caramello.csrf=")).Split(';')[0];
        using var setup = new HttpRequestMessage(HttpMethod.Post, "/api/auth/setup") { Content = JsonContent.Create(new { email = "primeira@teste.local", senha = "Senha-Forte-123" }) };
        setup.Headers.Add("X-CSRF-TOKEN", token); setup.Headers.Add("Cookie", cookie);
        Assert.Equal(HttpStatusCode.OK, (await client.SendAsync(setup)).StatusCode);

        using var scope = factory.Services.CreateScope();
        var users = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var user = await users.FindByEmailAsync("primeira@teste.local");
        Assert.NotNull(user);
        Assert.True(await users.IsInRoleAsync(user!, "Administrador"));
        Assert.False((await client.GetFromJsonAsync<SetupStatus>("/api/auth/setup-status"))!.Necessario);

        var csrf2 = await client.GetAsync("/api/auth/csrf");
        token = (await csrf2.Content.ReadFromJsonAsync<CsrfResposta>())!.Token;
        cookie = csrf2.Headers.GetValues("Set-Cookie").Single(x => x.StartsWith("caramello.csrf=")).Split(';')[0];
        using var repetido = new HttpRequestMessage(HttpMethod.Post, "/api/auth/setup") { Content = JsonContent.Create(new { email = "outra@teste.local", senha = "Outra-Senha-123" }) };
        repetido.Headers.Add("X-CSRF-TOKEN", token); repetido.Headers.Add("Cookie", cookie);
        Assert.Equal(HttpStatusCode.NotFound, (await client.SendAsync(repetido)).StatusCode);
    }

    private sealed record CsrfResposta(string Token);
    private sealed record SetupStatus(bool Necessario);
}

public sealed class SegurancaFactory : WebApplicationFactory<Program>
{
    private readonly string _banco = Path.Combine(Path.GetTempPath(), $"caramello-tests-{Guid.NewGuid():N}.db");
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Development");
        builder.ConfigureLogging(logging => logging.ClearProviders());
        builder.ConfigureServices(services =>
        {
            services.RemoveAll<DbContextOptions<AppDbContext>>();
            services.AddDataProtection().UseEphemeralDataProtectionProvider();
            services.AddDbContext<AppDbContext>(options => options.UseSqlite($"Data Source={_banco}"));
            using var provider = services.BuildServiceProvider();
            using var scope = provider.CreateScope();
            scope.ServiceProvider.GetRequiredService<AppDbContext>().Database.EnsureCreated();
        });
    }

    public async Task CriarUsuarioAsync(string email, string senha, bool administrador)
    {
        using var scope = Services.CreateScope();
        var users = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        if (await users.FindByEmailAsync(email) is not null) return;
        var user = new ApplicationUser { UserName = email, Email = email, EmailConfirmed = true };
        Assert.True((await users.CreateAsync(user, senha)).Succeeded);
        if (!administrador) return;
        var roles = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
        if (!await roles.RoleExistsAsync("Administrador")) Assert.True((await roles.CreateAsync(new IdentityRole("Administrador"))).Succeeded);
        Assert.True((await users.AddToRoleAsync(user, "Administrador")).Succeeded);
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        if (disposing && File.Exists(_banco))
        {
            try { File.Delete(_banco); } catch (IOException) { }
        }
    }
}
