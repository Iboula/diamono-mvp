using Microsoft.AspNetCore.Identity;

namespace Diamono.Infrastructure.Identity;

/// <summary>
/// Role du backoffice. Les permissions ne sont pas stockees ici : elles sont
/// derivees de <see cref="Diamono.Domain.Security.PermissionMatrix"/> au moment
/// de la construction du ClaimsPrincipal.
/// </summary>
public sealed class ApplicationRole : IdentityRole<Guid>
{
    public ApplicationRole() { }

    public ApplicationRole(string roleName) : base(roleName) { }
}
