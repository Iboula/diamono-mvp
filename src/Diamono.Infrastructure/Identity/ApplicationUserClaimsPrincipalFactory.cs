using System.Security.Claims;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;

namespace Diamono.Infrastructure.Identity;

/// <summary>
/// Ajoute le nom affichable au principal. Les permissions ne sont pas ajoutees ici :
/// elles sont derivees des roles par la transformation de claims cote Web, afin de
/// rester valables quel que soit le fournisseur d'authentification.
/// </summary>
public sealed class ApplicationUserClaimsPrincipalFactory(
    UserManager<ApplicationUser> userManager,
    RoleManager<ApplicationRole> roleManager,
    IOptions<IdentityOptions> options)
    : UserClaimsPrincipalFactory<ApplicationUser, ApplicationRole>(userManager, roleManager, options)
{
    public const string DisplayNameClaimType = "display_name";

    protected override async Task<ClaimsIdentity> GenerateClaimsAsync(ApplicationUser user)
    {
        var identity = await base.GenerateClaimsAsync(user);

        var displayName = string.IsNullOrWhiteSpace(user.DisplayName) ? user.Email : user.DisplayName;
        if (!string.IsNullOrWhiteSpace(displayName))
            identity.AddClaim(new Claim(DisplayNameClaimType, displayName));

        return identity;
    }
}
