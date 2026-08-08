namespace Diamono.Domain.Security;

/// <summary>
/// Matrice role -> permissions du MVP (DIA-016B). Source de verite unique :
/// la transformation des claims, le seed et les tests la consomment tous.
/// </summary>
public static class PermissionMatrix
{
    private static readonly Dictionary<string, IReadOnlyList<string>> ByRole =
        new(StringComparer.OrdinalIgnoreCase)
        {
            [DiamonoRoles.SuperAdmin] = Permissions.All,

            [DiamonoRoles.Gestionnaire] =
            [
                Permissions.BookingsView,
                Permissions.BookingsApprove,
                Permissions.BookingsReject,
                Permissions.BookingBlocksView,
                Permissions.BookingBlocksManage,
                Permissions.SettingsView
            ],

            [DiamonoRoles.Caissier] =
            [
                Permissions.BookingsView,
                Permissions.PaymentsMarkPaid
            ],

            [DiamonoRoles.Lecteur] =
            [
                Permissions.BookingsView,
                Permissions.BookingBlocksView,
                Permissions.SettingsView
            ]
        };

    /// <summary>Permissions d'un role. Role inconnu : aucune permission.</summary>
    public static IReadOnlyList<string> PermissionsFor(string role)
        => ByRole.TryGetValue(role, out var permissions) ? permissions : [];

    /// <summary>Union des permissions de plusieurs roles, sans doublon.</summary>
    public static IReadOnlyList<string> PermissionsForRoles(IEnumerable<string> roles)
    {
        var permissions = new HashSet<string>(StringComparer.Ordinal);
        foreach (var role in roles)
        {
            foreach (var permission in PermissionsFor(role))
                permissions.Add(permission);
        }

        return [.. permissions];
    }

    public static bool RoleHasPermission(string role, string permission)
        => PermissionsFor(role).Contains(permission, StringComparer.Ordinal);
}
