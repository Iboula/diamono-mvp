using System.Security.Claims;
using Diamono.Web.Security;

namespace Diamono.Web.Tests;

internal static class PrincipalFactory
{
    /// <summary>
    /// Construit un principal comme le ferait le pipeline reel : des roles poses par
    /// l'authentification, puis la transformation qui en derive les permissions.
    /// </summary>
    public static ClaimsPrincipal ForRoles(params string[] roles)
    {
        var identity = new ClaimsIdentity(
            authenticationType: "TestCookie",
            nameType: ClaimTypes.Name,
            roleType: ClaimTypes.Role);

        identity.AddClaim(new Claim(ClaimTypes.Name, "operateur@diamono.local"));
        foreach (var role in roles)
            identity.AddClaim(new Claim(ClaimTypes.Role, role));

        var principal = new ClaimsPrincipal(identity);
        return new PermissionClaimsTransformation().TransformAsync(principal).GetAwaiter().GetResult();
    }

    public static ClaimsPrincipal Anonymous() => new(new ClaimsIdentity());
}
