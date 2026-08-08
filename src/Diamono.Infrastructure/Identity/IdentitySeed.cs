using Diamono.Domain.Security;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;

namespace Diamono.Infrastructure.Identity;

/// <summary>
/// Seed des roles (toujours) et du compte de demonstration (Development/Demo uniquement).
/// Aucun mot de passe n'est ecrit dans le depot : il provient de la configuration
/// <c>DIAMONO_ADMIN_PASSWORD</c>. Sans valeur fournie, le compte n'est pas cree et la
/// procedure locale est journalisee — on n'invente jamais de secret silencieusement.
/// </summary>
public static class IdentitySeed
{
    public const string DemoAdminEmail = "admin@diamono.local";
    public const string AdminPasswordConfigurationKey = "DIAMONO_ADMIN_PASSWORD";

    public static async Task SeedRolesAsync(
        RoleManager<ApplicationRole> roleManager,
        CancellationToken cancellationToken = default)
    {
        foreach (var role in DiamonoRoles.All)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (!await roleManager.RoleExistsAsync(role))
                await roleManager.CreateAsync(new ApplicationRole(role));
        }
    }

    /// <summary>
    /// Cree ou met a jour le compte SuperAdmin de demonstration.
    /// A n'appeler que hors production.
    /// </summary>
    public static async Task SeedDemoAdminAsync(
        UserManager<ApplicationUser> userManager,
        string? adminPassword,
        ILogger logger,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (string.IsNullOrWhiteSpace(adminPassword))
        {
            logger.LogWarning(
                "Compte de demonstration non cree : {Key} n'est pas defini. " +
                "Definir un mot de passe local puis relancer, par exemple : " +
                "dotnet user-secrets set {Key} \"<mot-de-passe-local>\" --project src/Diamono.Web",
                AdminPasswordConfigurationKey,
                AdminPasswordConfigurationKey);
            return;
        }

        var existing = await userManager.FindByEmailAsync(DemoAdminEmail);
        if (existing is null)
        {
            var user = new ApplicationUser
            {
                UserName = DemoAdminEmail,
                Email = DemoAdminEmail,
                EmailConfirmed = true,
                DisplayName = "Administrateur Diamono"
            };

            var created = await userManager.CreateAsync(user, adminPassword);
            if (!created.Succeeded)
            {
                // Les descriptions Identity ne contiennent pas le mot de passe.
                logger.LogError(
                    "Creation du compte de demonstration echouee : {Errors}",
                    string.Join(" | ", created.Errors.Select(e => e.Description)));
                return;
            }

            await userManager.AddToRoleAsync(user, DiamonoRoles.SuperAdmin);
            logger.LogInformation("Compte de demonstration {Email} cree avec le role {Role}.",
                DemoAdminEmail, DiamonoRoles.SuperAdmin);
            return;
        }

        if (!await userManager.IsInRoleAsync(existing, DiamonoRoles.SuperAdmin))
            await userManager.AddToRoleAsync(existing, DiamonoRoles.SuperAdmin);
    }
}
