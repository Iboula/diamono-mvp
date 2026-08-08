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
    private const string DemoPassword = "LocalDemo!23456";

    [Fact]
    public async Task Production_flag_absent_ne_cree_pas_admin_demo()
    {
        await using var services = BuildServices(environmentName: Environments.Production, enableDemoAdmin: null, password: DemoPassword);

        await BootstrapAsync(services);

        Assert.Null(await UserManager(services).FindByEmailAsync(IdentitySeed.DemoAdminEmail));
    }

    [Fact]
    public async Task Production_flag_false_ne_cree_pas_admin_demo()
    {
        await using var services = BuildServices(environmentName: Environments.Production, enableDemoAdmin: "false", password: DemoPassword);

        await BootstrapAsync(services);

        Assert.Null(await UserManager(services).FindByEmailAsync(IdentitySeed.DemoAdminEmail));
    }

    [Fact]
    public async Task Production_flag_true_et_password_cree_admin_demo()
    {
        await using var services = BuildServices(environmentName: Environments.Production, enableDemoAdmin: "true", password: DemoPassword);

        await BootstrapAsync(services);

        var admin = await UserManager(services).FindByEmailAsync(IdentitySeed.DemoAdminEmail);
        Assert.NotNull(admin);
        Assert.True(await UserManager(services).CheckPasswordAsync(admin, DemoPassword));
    }

    [Fact]
    public async Task Flag_true_sans_password_ne_cree_pas_admin_demo()
    {
        await using var services = BuildServices(environmentName: Environments.Production, enableDemoAdmin: "true", password: null);

        await BootstrapAsync(services);

        Assert.Null(await UserManager(services).FindByEmailAsync(IdentitySeed.DemoAdminEmail));
    }

    [Fact]
    public async Task Admin_existant_n_est_pas_duplique()
    {
        await using var services = BuildServices(environmentName: Environments.Production, enableDemoAdmin: "true", password: DemoPassword);

        await BootstrapAsync(services);
        await BootstrapAsync(services);

        var users = await services.GetRequiredService<DiamonoDbContext>().Users
            .Where(x => x.Email == IdentitySeed.DemoAdminEmail)
            .ToListAsync();

        Assert.Single(users);
    }

    [Fact]
    public async Task Admin_demo_recoit_le_role_SuperAdmin()
    {
        await using var services = BuildServices(environmentName: Environments.Production, enableDemoAdmin: "true", password: DemoPassword);

        await BootstrapAsync(services);

        var admin = await UserManager(services).FindByEmailAsync(IdentitySeed.DemoAdminEmail);
        Assert.NotNull(admin);
        Assert.True(await UserManager(services).IsInRoleAsync(admin, DiamonoRoles.SuperAdmin));
    }

    [Fact]
    public async Task Mot_de_passe_jamais_present_dans_les_logs()
    {
        var logSink = new InMemoryLogSink();
        await using var services = BuildServices(
            environmentName: Environments.Production,
            enableDemoAdmin: "true",
            password: DemoPassword,
            logSink: logSink);

        await BootstrapAsync(services);

        Assert.DoesNotContain(logSink.Messages, message => message.Contains(DemoPassword, StringComparison.Ordinal));
    }

    private static async Task BootstrapAsync(ServiceProvider services)
    {
        await IdentitySeed.SeedRolesAsync(services.GetRequiredService<RoleManager<ApplicationRole>>());
        await services.GetRequiredService<DemoAdminBootstrapper>().BootstrapAsync();
    }

    private static UserManager<ApplicationUser> UserManager(ServiceProvider services)
        => services.GetRequiredService<UserManager<ApplicationUser>>();

    private static ServiceProvider BuildServices(
        string environmentName,
        string? enableDemoAdmin,
        string? password,
        InMemoryLogSink? logSink = null)
    {
        var configurationValues = new Dictionary<string, string?>();
        if (enableDemoAdmin is not null)
            configurationValues[DemoAdminBootstrapper.EnableDemoAdminConfigurationKey] = enableDemoAdmin;
        if (password is not null)
            configurationValues[IdentitySeed.AdminPasswordConfigurationKey] = password;

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
        })
        .AddEntityFrameworkStores<DiamonoDbContext>();
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
