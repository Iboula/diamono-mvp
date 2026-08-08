using Diamono.Domain.Security;
using Diamono.Infrastructure.Identity;
using Microsoft.AspNetCore.Identity;

namespace Diamono.Web.Startup;

public sealed class DemoAdminBootstrapper
{
    public const string EnableDemoAdminConfigurationKey = "DIAMONO_ENABLE_DEMO_ADMIN";

    // TEMPORARY MVP DEMO CREDENTIAL.
    // MUST BE REMOVED BEFORE MUNICIPAL PRODUCTION.
    public const string DemoAdminPassword = "DiamonoDemo2026!";

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
            return;
        }

        var admin = await userManager.FindByEmailAsync(IdentitySeed.DemoAdminEmail);
        if (admin is null)
        {
            admin = new ApplicationUser
            {
                UserName = IdentitySeed.DemoAdminEmail,
                Email = IdentitySeed.DemoAdminEmail,
                EmailConfirmed = true,
                DisplayName = "Administrateur Diamono"
            };

            var created = await userManager.CreateAsync(admin, DemoAdminPassword);
            if (!created.Succeeded)
                return;
        }
        else
        {
            if (!await ResetDemoAdminPasswordAsync(admin, cancellationToken))
                return;
        }

        if (!await userManager.IsInRoleAsync(admin, DiamonoRoles.SuperAdmin))
        {
            var role = await userManager.AddToRoleAsync(admin, DiamonoRoles.SuperAdmin);
            if (!role.Succeeded)
                return;
        }

        if (!await ClearLockoutAsync(admin))
            return;

        logger.LogInformation("Demo admin account ensured.");
    }

    public static bool ShouldBootstrapDemoAdmin(IHostEnvironment environment, IConfiguration configuration)
        => string.Equals(
            configuration[EnableDemoAdminConfigurationKey],
            "true",
            StringComparison.OrdinalIgnoreCase);

    private async Task<bool> ResetDemoAdminPasswordAsync(
        ApplicationUser admin,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var resetToken = await userManager.GeneratePasswordResetTokenAsync(admin);
        var reset = await userManager.ResetPasswordAsync(admin, resetToken, DemoAdminPassword);
        if (!reset.Succeeded)
            return false;

        logger.LogInformation("Demo admin password reset.");
        return true;
    }

    private async Task<bool> ClearLockoutAsync(ApplicationUser admin)
    {
        var resetAccessFailed = await userManager.ResetAccessFailedCountAsync(admin);
        if (!resetAccessFailed.Succeeded)
            return false;

        var clearLockout = await userManager.SetLockoutEndDateAsync(admin, null);
        if (!clearLockout.Succeeded)
            return false;

        var securityStamp = await userManager.UpdateSecurityStampAsync(admin);
        if (!securityStamp.Succeeded)
            return false;

        logger.LogInformation("Demo admin lockout cleared.");
        return true;
    }
}
