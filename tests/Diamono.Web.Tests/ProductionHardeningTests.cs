using System.Net;
using Diamono.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Xunit;

namespace Diamono.Web.Tests;

public sealed class ProductionHardeningTests
{
    [Fact]
    public async Task Health_live_retourne_healthy()
    {
        await using var factory = new DiamonoWebApplicationFactory();
        using var client = factory.CreateClient();
        client.BaseAddress = new Uri("https://localhost");

        var response = await client.GetAsync("/health/live");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("Healthy", await response.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task Health_ready_retourne_healthy_avec_db()
    {
        await using var factory = new DiamonoWebApplicationFactory();
        using var client = factory.CreateClient();
        client.BaseAddress = new Uri("https://localhost");

        var response = await client.GetAsync("/health/ready");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("Healthy", await response.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task Login_est_rate_limite()
    {
        await using var factory = new DiamonoWebApplicationFactory();
        using var client = factory.CreateClient();
        client.BaseAddress = new Uri("https://localhost");

        HttpResponseMessage? last = null;
        for (var i = 0; i < 9; i++)
            last = await client.GetAsync("/login");

        Assert.NotNull(last);
        Assert.Equal((HttpStatusCode)429, last!.StatusCode);
    }

    [Fact]
    public async Task Headers_securite_principaux_sont_presents()
    {
        await using var factory = new DiamonoWebApplicationFactory();
        using var client = factory.CreateClient();
        client.BaseAddress = new Uri("https://localhost");

        var response = await client.GetAsync("/health/live");

        Assert.True(response.Headers.Contains("X-Content-Type-Options"));
        Assert.True(response.Headers.Contains("Referrer-Policy"));
        Assert.True(response.Headers.Contains("Permissions-Policy"));
        Assert.True(response.Headers.Contains("Content-Security-Policy"));
        Assert.True(response.Headers.Contains("X-Frame-Options"));
    }

    [Fact]
    public async Task Erreur_non_development_ne_renvoie_pas_stack_trace()
    {
        await using var factory = new DiamonoWebApplicationFactory();
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });
        client.BaseAddress = new Uri("https://localhost");

        var response = await client.GetAsync("/__test/throw");
        var body = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.InternalServerError, response.StatusCode);
        Assert.DoesNotContain("Sensitive stack trace marker", body);
        Assert.DoesNotContain("System.InvalidOperationException", body);
    }

    [Fact]
    public void Cookies_identity_conservent_les_proprietes_securite()
    {
        using var factory = new DiamonoWebApplicationFactory();

        var options = factory.Services
            .GetRequiredService<IOptionsMonitor<CookieAuthenticationOptions>>()
            .Get(IdentityConstants.ApplicationScheme);

        Assert.True(options.Cookie.HttpOnly);
        Assert.Equal(CookieSecurePolicy.Always, options.Cookie.SecurePolicy);
        Assert.Equal(SameSiteMode.Lax, options.Cookie.SameSite);
    }

    private sealed class DiamonoWebApplicationFactory : WebApplicationFactory<Program>
    {
        public DiamonoWebApplicationFactory()
        {
            Environment.SetEnvironmentVariable(
                "DIAMONO_CONNECTION",
                "Host=localhost;Database=diamono_test;Username=test;Password=test");
        }

        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.UseEnvironment("Testing");
            builder.ConfigureServices(services =>
            {
                services.RemoveAll<DbContextOptions<DiamonoDbContext>>();
                services.RemoveAll<IDbContextOptionsConfiguration<DiamonoDbContext>>();
                services.AddDbContext<DiamonoDbContext>(options =>
                    options.UseInMemoryDatabase($"diamono-hardening-{Guid.NewGuid()}"));
            });
        }
    }
}
