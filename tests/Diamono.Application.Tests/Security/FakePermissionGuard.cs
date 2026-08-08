using Diamono.Application.Abstractions;
using Diamono.Application.Security;
using Diamono.Domain.Security;

namespace Diamono.Application.Tests;

/// <summary>
/// Garde de test adosse a la vraie <see cref="PermissionMatrix"/> : les tests
/// d'autorisation des cas d'usage exercent donc la matrice reelle, pas une
/// liste de permissions recopiee.
/// </summary>
internal sealed class FakePermissionGuard : IPermissionGuard
{
    private readonly HashSet<string> permissions;

    private FakePermissionGuard(IEnumerable<string> permissions)
        => this.permissions = new HashSet<string>(permissions, StringComparer.Ordinal);

    /// <summary>Garde d'un operateur portant les roles indiques.</summary>
    public static FakePermissionGuard ForRoles(params string[] roles)
        => new(PermissionMatrix.PermissionsForRoles(roles));

    /// <summary>Garde permissif : utilise par les tests qui ne portent pas sur l'autorisation.</summary>
    public static FakePermissionGuard AllowAll() => new(Permissions.All);

    /// <summary>Garde anonyme : aucune permission.</summary>
    public static FakePermissionGuard Anonymous() => new([]);

    public Task<bool> HasPermissionAsync(string permission, CancellationToken cancellationToken = default)
        => Task.FromResult(permissions.Contains(permission));

    public Task EnsurePermissionAsync(string permission, CancellationToken cancellationToken = default)
        => permissions.Contains(permission)
            ? Task.CompletedTask
            : throw new PermissionDeniedException(permission);
}
