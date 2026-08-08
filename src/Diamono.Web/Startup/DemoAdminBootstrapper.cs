using Diamono.Domain.Security;
using Diamono.Infrastructure.Identity;
using Microsoft.AspNetCore.Identity;

namespace Diamono.Web.Startup;

public sealed class DemoAdminBootstrapper
{
    public const string EnableDemoAdminConfigurationKey = "DIAMONO_ENABLE_DEMO_ADMIN";
    public const string ResetDemoAdminPasswordConfigurationKey = "DIAMONO_RESET_DEMO_ADMIN_PASSWORD";

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
        var existingAdmin = await userManager.FindByEmailAsync(IdentitySeed.DemoAdminEmail);
        var existedBefore = existingAdmin is not null;
        logger.LogInformation("Demo admin exists: {AdminExists}.", existedBefore);

        var resetPasswordRequested = ShouldResetDemoAdminPassword(configuration);
        logger.LogInformation("Demo admin password reset requested: {ResetPasswordRequested}.", resetPasswordRequested);

        await IdentitySeed.SeedDemoAdminAsync(
            userManager,
            configuration[IdentitySeed.AdminPasswordConfigurationKey],
            logger,
            cancellationToken);

        var admin = await userManager.FindByEmailAsync(IdentitySeed.DemoAdminEmail);
        if (admin is not null && existedBefore)
            logger.LogInformation("Demo admin already exists.");
        else if (admin is not null)
            logger.LogInformation("Demo admin created.");

        if (admin is null)
            return;

        var hasSuperAdminRole = await userManager.IsInRoleAsync(admin, DiamonoRoles.SuperAdmin);
        logger.LogInformation("Demo admin SuperAdmin role present: {SuperAdminRolePresent}.", hasSuperAdminRole);

        if (resetPasswordRequested && existedBefore)
            await ResetExistingDemoAdminPasswordAsync(admin, cancellationToken);
    }

    public static bool ShouldBootstrapDemoAdmin(IHostEnvironment environment, IConfiguration configuration)
        => !environment.IsProduction()
           || string.Equals(
               configuration[EnableDemoAdminConfigurationKey],
               "true",
               StringComparison.OrdinalIgnoreCase);

    private static bool ShouldResetDemoAdminPassword(IConfiguration configuration)
        => string.Equals(
            configuration[ResetDemoAdminPasswordConfigurationKey],
            "true",
            StringComparison.OrdinalIgnoreCase);

    private async Task ResetExistingDemoAdminPasswordAsync(
        ApplicationUser admin,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var adminPassword = configuration[IdentitySeed.AdminPasswordConfigurationKey];
        if (string.IsNullOrWhiteSpace(adminPassword))
        {
            logger.LogWarning(
                "Demo admin password reset failed: {Key} is not defined.",
                IdentitySeed.AdminPasswordConfigurationKey);
            return;
        }

        var resetToken = await userManager.GeneratePasswordResetTokenAsync(admin);
        var reset = await userManager.ResetPasswordAsync(admin, resetToken, adminPassword);
        if (!reset.Succeeded)
        {
            logger.LogError(
                "Demo admin password reset failed: {Errors}",
                string.Join(" | ", reset.Errors.Select(e => $"{e.Code}: {e.Description}")));
            return;
        }

        var securityStamp = await userManager.UpdateSecurityStampAsync(admin);
        if (!securityStamp.Succeeded)
        {
            logger.LogError(
                "Demo admin security stamp renewal failed: {Errors}",
                string.Join(" | ", securityStamp.Errors.Select(e => $"{e.Code}: {e.Description}")));
            return;
        }

        logger.LogInformation("Demo admin password reset completed.");
    }
}
