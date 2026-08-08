using Diamono.Infrastructure.Identity;
using Diamono.Infrastructure.Persistence;
using Microsoft.AspNetCore.Identity;

namespace Diamono.Web.Security;

public static class DiamonoIdentityExtensions
{
    /// <summary>
    /// ASP.NET Core Identity avec stockage EF Core.
    /// Enregistre cote Web parce que <c>AddIdentity</c> (et les schemas de cookies
    /// associes) appartient au framework ASP.NET Core : Diamono.Infrastructure reste
    /// une bibliotheque de classes sans dependance au pipeline HTTP.
    /// </summary>
    public static IServiceCollection AddDiamonoIdentity(this IServiceCollection services)
    {
        services.AddIdentity<ApplicationUser, ApplicationRole>(options =>
        {
            options.User.RequireUniqueEmail = true;
            options.SignIn.RequireConfirmedAccount = false;

            options.Password.RequiredLength = 12;
            options.Password.RequireDigit = true;
            options.Password.RequireLowercase = true;
            options.Password.RequireUppercase = true;
            options.Password.RequireNonAlphanumeric = true;

            options.Lockout.MaxFailedAccessAttempts = 5;
            options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15);
        })
        .AddEntityFrameworkStores<DiamonoDbContext>()
        .AddClaimsPrincipalFactory<ApplicationUserClaimsPrincipalFactory>()
        .AddDefaultTokenProviders();

        return services;
    }
}
