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
    private const string ResetPassword = "LocalDemo!65432";

    [Fact]
    public async Task Production_flag_absent_ne_cree_pas_admin_demo()
    {
        await using var services = BuildServices(
            environmentName: Environments.Production,
            enableDemoAdmin: null,
            resetDemoAdminPassword: null,
            password: DemoPassword);

        await BootstrapAsync(services);

        Assert.Null(await UserManager(services).FindByEmailAsync(IdentitySeed.DemoAdminEmail));
    }

    [Fact]
    public async Task Production_flag_false_ne_cree_pas_admin_demo()
    {
        await using var services = BuildServices(
            environmentName: Environments.Production,
            enableDemoAdmin: "false",
            resetDemoAdminPassword: null,
            password: DemoPassword);

        await BootstrapAsync(services);

        Assert.Null(await UserManager(services).FindByEmailAsync(IdentitySeed.DemoAdminEmail));
    }

    [Fact]
    public async Task Production_flag_true_et_password_cree_admin_demo()
    {
        await using var services = BuildServices(
            environmentName: Environments.Production,
            enableDemoAdmin: "true",
            resetDemoAdminPassword: null,
            password: DemoPassword);

        await BootstrapAsync(services);

        var admin = await UserManager(services).FindByEmailAsync(IdentitySeed.DemoAdminEmail);
        Assert.NotNull(admin);
        Assert.True(await UserManager(services).CheckPasswordAsync(admin, DemoPassword));
    }

    [Fact]
    public async Task Flag_true_sans_password_ne_cree_pas_admin_demo()
    {
        await using var services = BuildServices(
            environmentName: Environments.Production,
            enableDemoAdmin: "true",
            resetDemoAdminPassword: null,
            password: null);

        await BootstrapAsync(services);

        Assert.Null(await UserManager(services).FindByEmailAsync(IdentitySeed.DemoAdminEmail));
    }

    [Fact]
    public async Task Admin_existant_n_est_pas_duplique()
    {
        await using var services = BuildServices(
            environmentName: Environments.Production,
            enableDemoAdmin: "true",
            resetDemoAdminPassword: null,
            password: DemoPassword);

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
        await using var services = BuildServices(
            environmentName: Environments.Production,
            enableDemoAdmin: "true",
            resetDemoAdminPassword: null,
            password: DemoPassword);

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
            resetDemoAdminPassword: "true",
            password: DemoPassword,
            logSink: logSink);

        await BootstrapAsync(services);
        SetConfiguration(services, IdentitySeed.AdminPasswordConfigurationKey, ResetPassword);
        await BootstrapAsync(services);

        Assert.DoesNotContain(logSink.Messages, message => message.Contains(DemoPassword, StringComparison.Ordinal));
        Assert.DoesNotContain(logSink.Messages, message => message.Contains(ResetPassword, StringComparison.Ordinal));
    }

    [Fact]
    public async Task Admin_existant_reset_false_garde_l_ancien_password()
    {
        await using var services = BuildServices(
            environmentName: Environments.Production,
            enableDemoAdmin: "true",
            resetDemoAdminPassword: "false",
            password: DemoPassword);

        await BootstrapAsync(services);
        SetConfiguration(services, IdentitySeed.AdminPasswordConfigurationKey, ResetPassword);
        await BootstrapAsync(services);

        var admin = await GetDemoAdminAsync(services);
        Assert.True(await UserManager(services).CheckPasswordAsync(admin, DemoPassword));
        Assert.False(await UserManager(services).CheckPasswordAsync(admin, ResetPassword));
    }

    [Fact]
    public async Task Admin_existant_reset_true_utilise_le_nouveau_password()
    {
        await using var services = BuildServices(
            environmentName: Environments.Production,
            enableDemoAdmin: "true",
            resetDemoAdminPassword: "false",
            password: DemoPassword);

        await BootstrapAsync(services);
        SetConfiguration(services, IdentitySeed.AdminPasswordConfigurationKey, ResetPassword);
        SetConfiguration(services, DemoAdminBootstrapper.ResetDemoAdminPasswordConfigurationKey, "true");
        await BootstrapAsync(services);

        var admin = await GetDemoAdminAsync(services);
        Assert.True(await UserManager(services).CheckPasswordAsync(admin, ResetPassword));
        Assert.False(await UserManager(services).CheckPasswordAsync(admin, DemoPassword));
    }

    [Fact]
    public async Task Reset_true_conserve_le_role_SuperAdmin()
    {
        await using var services = BuildServices(
            environmentName: Environments.Production,
            enableDemoAdmin: "true",
            resetDemoAdminPassword: "false",
            password: DemoPassword);

        await BootstrapAsync(services);
        SetConfiguration(services, IdentitySeed.AdminPasswordConfigurationKey, ResetPassword);
        SetConfiguration(services, DemoAdminBootstrapper.ResetDemoAdminPasswordConfigurationKey, "true");
        await BootstrapAsync(services);

        var admin = await GetDemoAdminAsync(services);
        Assert.True(await UserManager(services).IsInRoleAsync(admin, DiamonoRoles.SuperAdmin));
    }

    [Fact]
    public async Task Reset_false_par_defaut()
    {
        await using var services = BuildServices(
            environmentName: Environments.Production,
            enableDemoAdmin: "true",
            resetDemoAdminPassword: null,
            password: DemoPassword);

        await BootstrapAsync(services);
        SetConfiguration(services, IdentitySeed.AdminPasswordConfigurationKey, ResetPassword);
        await BootstrapAsync(services);

        var admin = await GetDemoAdminAsync(services);
        Assert.True(await UserManager(services).CheckPasswordAsync(admin, DemoPassword));
        Assert.False(await UserManager(services).CheckPasswordAsync(admin, ResetPassword));
    }

    [Fact]
    public async Task Production_sans_demo_flag_ne_reset_pas()
    {
        await using var services = BuildServices(
            environmentName: Environments.Production,
            enableDemoAdmin: "true",
            resetDemoAdminPassword: "false",
            password: DemoPassword);

        await BootstrapAsync(services);
        SetConfiguration(services, DemoAdminBootstrapper.EnableDemoAdminConfigurationKey, null);
        SetConfiguration(services, DemoAdminBootstrapper.ResetDemoAdminPasswordConfigurationKey, "true");
        SetConfiguration(services, IdentitySeed.AdminPasswordConfigurationKey, ResetPassword);
        await BootstrapAsync(services);

        var admin = await GetDemoAdminAsync(services);
        Assert.True(await UserManager(services).CheckPasswordAsync(admin, DemoPassword));
        Assert.False(await UserManager(services).CheckPasswordAsync(admin, ResetPassword));
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

    private static void SetConfiguration(ServiceProvider services, string key, string? value)
        => services.GetRequiredService<IConfiguration>()[key] = value;

    private static ServiceProvider BuildServices(
        string environmentName,
        string? enableDemoAdmin,
        string? resetDemoAdminPassword,
        string? password,
        InMemoryLogSink? logSink = null)
    {
        var configurationValues = new Dictionary<string, string?>();
        if (enableDemoAdmin is not null)
            configurationValues[DemoAdminBootstrapper.EnableDemoAdminConfigurationKey] = enableDemoAdmin;
        if (resetDemoAdminPassword is not null)
            configurationValues[DemoAdminBootstrapper.ResetDemoAdminPasswordConfigurationKey] = resetDemoAdminPassword;
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
