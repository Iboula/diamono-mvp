using Diamono.Infrastructure.Identity;
using Microsoft.AspNetCore.Identity;

namespace Diamono.Web.Startup;

public sealed class DemoAdminBootstrapper
{
    public const string EnableDemoAdminConfigurationKey = "DIAMONO_ENABLE_DEMO_ADMIN";

    private readonly UserManager<ApplicationUser> userManager;
    private readonly IConfiguration configuration;
    private readonly IHostEnvironment environment;
    private readonly ILogger<DemoAdminBootstrapper> logger;

    public DemoAdminBootstrapper(
        UserManager<ApplicationUser> userManager,
        IConfiguration configuration,
        IHostEnvironment environment,
        ILogger<DemoAdminBootstrapper> logger)
    {
        this.userManager = userManager;
        this.configuration = configuration;
        this.environment = environment;
        this.logger = logger;
    }

    public async Task BootstrapAsync(CancellationToken cancellationToken = default)
    {
        if (!ShouldBootstrapDemoAdmin(environment, configuration))
        {
            logger.LogInformation("Demo admin bootstrap disabled.");
            return;
        }

        logger.LogInformation("Demo admin bootstrap enabled.");
        var existedBefore = await userManager.FindByEmailAsync(IdentitySeed.DemoAdminEmail) is not null;

        await IdentitySeed.SeedDemoAdminAsync(
            userManager,
            configuration[IdentitySeed.AdminPasswordConfigurationKey],
            logger,
            cancellationToken);

        var existsAfter = await userManager.FindByEmailAsync(IdentitySeed.DemoAdminEmail) is not null;
        if (existsAfter && existedBefore)
            logger.LogInformation("Demo admin already exists.");
        else if (existsAfter)
            logger.LogInformation("Demo admin created.");
    }

    public static bool ShouldBootstrapDemoAdmin(IHostEnvironment environment, IConfiguration configuration)
        => !environment.IsProduction()
           || string.Equals(
               configuration[EnableDemoAdminConfigurationKey],
               "true",
               StringComparison.OrdinalIgnoreCase);
}
