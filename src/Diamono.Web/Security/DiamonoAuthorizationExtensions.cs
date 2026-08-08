using Diamono.Domain.Security;
using Microsoft.AspNetCore.Authentication;

namespace Diamono.Web.Security;

public static class DiamonoAuthorizationExtensions
{
    /// <summary>
    /// Declare une policy par permission. Les composants et les cas d'usage
    /// referencent uniquement ces policies : aucun test du type
    /// <c>if (role == "SuperAdmin")</c> ne doit exister dans l'application.
    /// Methode partagee avec les tests pour que ceux-ci evaluent la configuration reelle.
    /// </summary>
    public static IServiceCollection AddDiamonoAuthorization(this IServiceCollection services)
    {
        services.AddAuthorization(options =>
        {
            foreach (var permission in Permissions.All)
            {
                options.AddPolicy(permission, policy => policy
                    .RequireAuthenticatedUser()
                    .RequireClaim(Permissions.ClaimType, permission));
            }
        });

        services.AddSingleton<IClaimsTransformation, PermissionClaimsTransformation>();
        return services;
    }
}
