using System.Security.Claims;
using Diamono.Application.Abstractions;
using Diamono.Application.Security;
using Diamono.Domain.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Http;

namespace Diamono.Web.Security;

/// <summary>
/// Implementation du garde applicatif : elle delegue aux policies ASP.NET Core,
/// de sorte que l'ecran et le cas d'usage evaluent exactement la meme regle.
/// </summary>
public sealed class PermissionGuard(
    AuthenticationStateProvider authenticationStateProvider,
    IAuthorizationService authorizationService,
    IHttpContextAccessor httpContextAccessor)
    : IPermissionGuard, ICurrentUserAccessor
{
    public async Task<bool> HasPermissionAsync(string permission, CancellationToken cancellationToken = default)
    {
        var user = await GetPrincipalAsync();
        var result = await authorizationService.AuthorizeAsync(user, permission);
        return result.Succeeded;
    }

    public async Task EnsurePermissionAsync(string permission, CancellationToken cancellationToken = default)
    {
        if (!await HasPermissionAsync(permission, cancellationToken))
            throw new PermissionDeniedException(permission);
    }

    public async Task<CurrentUser?> GetCurrentUserAsync(CancellationToken cancellationToken = default)
    {
        var user = await GetPrincipalAsync();
        if (user.Identity?.IsAuthenticated != true) return null;

        var identity = user.Identities.First(x => x.IsAuthenticated);
        var email = user.FindFirst(ClaimTypes.Email)?.Value ?? user.Identity.Name ?? string.Empty;

        return new CurrentUser(
            Id: user.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? string.Empty,
            Email: email,
            DisplayName: user.FindFirst("display_name")?.Value is { Length: > 0 } name ? name : email,
            Roles: [.. identity.FindAll(identity.RoleClaimType).Select(x => x.Value)],
            Permissions: [.. user.FindAll(Permissions.ClaimType).Select(x => x.Value)]);
    }

    private async Task<ClaimsPrincipal> GetPrincipalAsync()
    {
        if (httpContextAccessor.HttpContext?.User is { } httpUser)
            return httpUser;

        return (await authenticationStateProvider.GetAuthenticationStateAsync()).User;
    }
}
