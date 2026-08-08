using Diamono.Domain.Security;
using Diamono.Infrastructure.Identity;
using Diamono.Infrastructure.Persistence;
using Diamono.Web.Startup;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Xunit;

namespace Diamono.Web.Tests;

public sealed class DemoAdminBootstrapperTests
{
    private const string OldPassword = "LocalDemo!23456";
    private const string DemoPassword = DemoAdminBootstrapper.DemoAdminPassword;

    [Fact]
    public async Task Flag_absent_ne_cree_pas_admin_demo()
    {
        await using var services = BuildServices(environmentName: Environments.Production, enableDemoAdmin: null);

        await BootstrapAsync(services);

        Assert.Null(await UserManager(services).FindByEmailAsync(IdentitySeed.DemoAdminEmail));
    }

    [Fact]
    public async Task Flag_false_ne_reset_pas_admin_existant()
    {
        await using var services = BuildServices(environmentName: Environments.Production, enableDemoAdmin: "false");
        var admin = await CreateExistingAdminAsync(services, OldPassword);

        await BootstrapAsync(services);

        Assert.True(await UserManager(services).CheckPasswordAsync(admin, OldPassword));
        Assert.False(await UserManager(services).CheckPasswordAsync(admin, DemoPassword));
    }

    [Fact]
    public async Task Flag_true_et_compte_absent_cree_admin_demo()
    {
        await using var services = BuildServices(environmentName: Environments.Production, enableDemoAdmin: "true");

        await BootstrapAsync(services);

        var admin = await GetDemoAdminAsync(services);
        Assert.Equal(IdentitySeed.DemoAdminEmail, admin.Email);
        Assert.Equal("Administrateur Diamono", admin.DisplayName);
        Assert.True(admin.EmailConfirmed);
    }

    [Fact]
    public async Task Login_avec_password_demo_reussit()
    {
        await using var services = BuildServices(environmentName: Environments.Production, enableDemoAdmin: "true");

        await BootstrapAsync(services);

        var admin = await GetDemoAdminAsync(services);
        Assert.True(await UserManager(services).CheckPasswordAsync(admin, DemoPassword));
    }

    [Fact]
    public async Task Compte_existant_ancien_password_est_remplace()
    {
        await using var services = BuildServices(environmentName: Environments.Production, enableDemoAdmin: "true");
        await CreateExistingAdminAsync(services, OldPassword);

        await BootstrapAsync(services);

        var admin = await GetDemoAdminAsync(services);
        Assert.True(await UserManager(services).CheckPasswordAsync(admin, DemoPassword));
        Assert.False(await UserManager(services).CheckPasswordAsync(admin, OldPassword));
    }

    [Fact]
    public async Task Compte_locke_est_deverrouille()
    {
        await using var services = BuildServices(environmentName: Environments.Production, enableDemoAdmin: "true");
        var admin = await CreateExistingAdminAsync(services, OldPassword);
        await UserManager(services).SetLockoutEnabledAsync(admin, true);
        await UserManager(services).SetLockoutEndDateAsync(admin, DateTimeOffset.UtcNow.AddHours(2));

        await BootstrapAsync(services);

        admin = await GetDemoAdminAsync(services);
        Assert.Null(await UserManager(services).GetLockoutEndDateAsync(admin));
    }

    [Fact]
    public async Task AccessFailedCount_est_remis_a_zero()
    {
        await using var services = BuildServices(environmentName: Environments.Production, enableDemoAdmin: "true");
        var admin = await CreateExistingAdminAsync(services, OldPassword);
        await UserManager(services).AccessFailedAsync(admin);
        await UserManager(services).AccessFailedAsync(admin);

        await BootstrapAsync(services);

        admin = await GetDemoAdminAsync(services);
        Assert.Equal(0, await UserManager(services).GetAccessFailedCountAsync(admin));
    }

    [Fact]
    public async Task Role_SuperAdmin_est_garanti()
    {
        await using var services = BuildServices(environmentName: Environments.Production, enableDemoAdmin: "true");
        await CreateExistingAdminAsync(services, OldPassword, addSuperAdminRole: false);

        await BootstrapAsync(services);

        var admin = await GetDemoAdminAsync(services);
        Assert.True(await UserManager(services).IsInRoleAsync(admin, DiamonoRoles.SuperAdmin));
    }

    [Fact]
    public async Task Mot_de_passe_jamais_present_dans_les_logs()
    {
        var logSink = new InMemoryLogSink();
        await using var services = BuildServices(
            environmentName: Environments.Production,
            enableDemoAdmin: "true",
            logSink: logSink);
        await CreateExistingAdminAsync(services, OldPassword);

        await BootstrapAsync(services);

        Assert.DoesNotContain(logSink.Messages, message => message.Contains(DemoPassword, StringComparison.Ordinal));
        Assert.DoesNotContain(logSink.Messages, message => message.Contains(OldPassword, StringComparison.Ordinal));
    }

    private static async Task BootstrapAsync(ServiceProvider services)
    {
        await IdentitySeed.SeedRolesAsync(services.GetRequiredService<RoleManager<ApplicationRole>>());
        await services.GetRequiredService<DemoAdminBootstrapper>().BootstrapAsync();
    }

    private static UserManager<ApplicationUser> UserManager(ServiceProvider services)
        => services.GetRequiredService<UserManager<ApplicationUser>>();

    private static async Task<ApplicationUser> GetDemoAdminAsync(ServiceProvider services)
        => await UserManager(services).FindByEmailAsync(IdentitySeed.DemoAdminEmail)
           ?? throw new InvalidOperationException("Demo admin should exist.");

    private static async Task<ApplicationUser> CreateExistingAdminAsync(
        ServiceProvider services,
        string password,
        bool addSuperAdminRole = true)
    {
        await IdentitySeed.SeedRolesAsync(services.GetRequiredService<RoleManager<ApplicationRole>>());
        var admin = new ApplicationUser
        {
            UserName = IdentitySeed.DemoAdminEmail,
            Email = IdentitySeed.DemoAdminEmail,
            EmailConfirmed = true,
            DisplayName = "Administrateur Diamono"
        };

        var created = await UserManager(services).CreateAsync(admin, password);
        Assert.True(created.Succeeded, string.Join(" | ", created.Errors.Select(x => x.Description)));

        if (addSuperAdminRole)
            await UserManager(services).AddToRoleAsync(admin, DiamonoRoles.SuperAdmin);

        return admin;
    }

    private static ServiceProvider BuildServices(
        string environmentName,
        string? enableDemoAdmin,
        InMemoryLogSink? logSink = null)
    {
        var configurationValues = new Dictionary<string, string?>();
        if (enableDemoAdmin is not null)
            configurationValues[DemoAdminBootstrapper.EnableDemoAdminConfigurationKey] = enableDemoAdmin;

        var services = new ServiceCollection();
        services.AddSingleton<IConfiguration>(new ConfigurationBuilder()
            .AddInMemoryCollection(configurationValues)
            .Build());
        services.AddSingleton<IHostEnvironment>(new FakeHostEnvironment(environmentName));
        services.AddLogging(builder =>
        {
            if (logSink is not null)
                builder.AddProvider(new InMemoryLoggerProvider(logSink));
        });
        services.AddDbContext<DiamonoDbContext>(options =>
            options.UseInMemoryDatabase($"diamono-demo-admin-{Guid.NewGuid()}"));
        services.AddIdentity<ApplicationUser, ApplicationRole>(options =>
        {
            options.User.RequireUniqueEmail = true;
            options.Password.RequiredLength = 12;
            options.Password.RequireDigit = true;
            options.Password.RequireLowercase = true;
            options.Password.RequireUppercase = true;
            options.Password.RequireNonAlphanumeric = true;
            options.Lockout.AllowedForNewUsers = true;
        })
        .AddEntityFrameworkStores<DiamonoDbContext>()
        .AddDefaultTokenProviders();
        services.AddScoped<DemoAdminBootstrapper>();

        return services.BuildServiceProvider();
    }

    private sealed class FakeHostEnvironment(string environmentName) : IWebHostEnvironment
    {
        public string EnvironmentName { get; set; } = environmentName;
        public string ApplicationName { get; set; } = "Diamono.Web.Tests";
        public string ContentRootPath { get; set; } = AppContext.BaseDirectory;
        public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();
        public string WebRootPath { get; set; } = AppContext.BaseDirectory;
        public IFileProvider WebRootFileProvider { get; set; } = new NullFileProvider();
    }

    private sealed class InMemoryLogSink
    {
        public List<string> Messages { get; } = [];
    }

    private sealed class InMemoryLoggerProvider(InMemoryLogSink sink) : ILoggerProvider
    {
        public ILogger CreateLogger(string categoryName) => new InMemoryLogger(sink);

        public void Dispose()
        {
        }
    }

    private sealed class InMemoryLogger(InMemoryLogSink sink) : ILogger
    {
        public IDisposable? BeginScope<TState>(TState state)
            where TState : notnull
            => null;

        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(
            LogLevel logLevel,
            EventId eventId,
            TState state,
            Exception? exception,
            Func<TState, Exception?, string> formatter)
        {
            sink.Messages.Add(formatter(state, exception));
        }
    }
}
