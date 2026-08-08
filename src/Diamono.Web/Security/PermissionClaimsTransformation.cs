using System.Security.Claims;
using Diamono.Domain.Security;
using Microsoft.AspNetCore.Authentication;

namespace Diamono.Web.Security;

/// <summary>
/// Traduit les roles portes par le ClaimsPrincipal en claims de permission.
/// Volontairement branchee sur <see cref="IClaimsTransformation"/> et non sur
/// Identity : elle s'applique de la meme facon a un cookie Identity aujourd'hui
/// et a un jeton OIDC demain, tant que les roles arrivent dans le principal.
/// </summary>
public sealed class PermissionClaimsTransformation : IClaimsTransformation
{
    public Task<ClaimsPrincipal> TransformAsync(ClaimsPrincipal principal)
    {
        if (principal.Identity?.IsAuthenticated != true)
            return Task.FromResult(principal);

        var identity = principal.Identities.FirstOrDefault(x => x.IsAuthenticated);
        if (identity is null)
            return Task.FromResult(principal);

        // Idempotence : la transformation peut etre invoquee plusieurs fois par requete.
        if (identity.HasClaim(c => c.Type == Permissions.ClaimType))
            return Task.FromResult(principal);

        var roles = identity.FindAll(identity.RoleClaimType).Select(x => x.Value);
        foreach (var permission in PermissionMatrix.PermissionsForRoles(roles))
            identity.AddClaim(new Claim(Permissions.ClaimType, permission));

        return Task.FromResult(principal);
    }
}
