using Diamono.Domain.Security;
using Xunit;

namespace Diamono.Domain.Tests;

public sealed class PermissionMatrixTests
{
    [Fact]
    public void SuperAdmin_detient_toutes_les_permissions()
    {
        var permissions = PermissionMatrix.PermissionsFor(DiamonoRoles.SuperAdmin);

        Assert.Equal(Permissions.All.OrderBy(x => x), permissions.OrderBy(x => x));
    }

    [Fact]
    public void Gestionnaire_correspond_exactement_a_la_matrice_DIA_016B()
    {
        AssertExactly(
            DiamonoRoles.Gestionnaire,
            Permissions.BookingsView,
            Permissions.BookingsApprove,
            Permissions.BookingsReject,
            Permissions.BookingBlocksView,
            Permissions.BookingBlocksManage,
            Permissions.SettingsView);
    }

    [Fact]
    public void Caissier_correspond_exactement_a_la_matrice_DIA_016B()
    {
        AssertExactly(
            DiamonoRoles.Caissier,
            Permissions.BookingsView,
            Permissions.PaymentsMarkPaid);
    }

    [Fact]
    public void Lecteur_correspond_exactement_a_la_matrice_DIA_016B()
    {
        AssertExactly(
            DiamonoRoles.Lecteur,
            Permissions.BookingsView,
            Permissions.BookingBlocksView,
            Permissions.SettingsView);
    }

    [Theory]
    // Le Gestionnaire n'encaisse pas.
    [InlineData(DiamonoRoles.Gestionnaire, Permissions.PaymentsMarkPaid)]
    [InlineData(DiamonoRoles.Gestionnaire, Permissions.SettingsManage)]
    [InlineData(DiamonoRoles.Gestionnaire, Permissions.AdministrationManage)]
    // Le Caissier ne decide pas et ne voit pas les parametres.
    [InlineData(DiamonoRoles.Caissier, Permissions.BookingsApprove)]
    [InlineData(DiamonoRoles.Caissier, Permissions.BookingsReject)]
    [InlineData(DiamonoRoles.Caissier, Permissions.SettingsView)]
    [InlineData(DiamonoRoles.Caissier, Permissions.SettingsManage)]
    [InlineData(DiamonoRoles.Caissier, Permissions.BookingBlocksView)]
    // Le Lecteur n'ecrit jamais.
    [InlineData(DiamonoRoles.Lecteur, Permissions.BookingsApprove)]
    [InlineData(DiamonoRoles.Lecteur, Permissions.BookingsReject)]
    [InlineData(DiamonoRoles.Lecteur, Permissions.PaymentsMarkPaid)]
    [InlineData(DiamonoRoles.Lecteur, Permissions.BookingBlocksManage)]
    [InlineData(DiamonoRoles.Lecteur, Permissions.SettingsManage)]
    public void Permission_absente_du_role(string role, string permission)
        => Assert.False(PermissionMatrix.RoleHasPermission(role, permission));

    [Fact]
    public void Role_inconnu_ne_donne_aucune_permission()
        => Assert.Empty(PermissionMatrix.PermissionsFor("RoleInexistant"));

    [Fact]
    public void Cumul_de_roles_fait_l_union_sans_doublon()
    {
        var permissions = PermissionMatrix.PermissionsForRoles([DiamonoRoles.Caissier, DiamonoRoles.Lecteur]);

        Assert.Equal(permissions.Count, permissions.Distinct().Count());
        Assert.Contains(Permissions.PaymentsMarkPaid, permissions);
        Assert.Contains(Permissions.BookingBlocksView, permissions);
        Assert.DoesNotContain(Permissions.BookingsApprove, permissions);
    }

    private static void AssertExactly(string role, params string[] expected)
        => Assert.Equal(
            expected.OrderBy(x => x),
            PermissionMatrix.PermissionsFor(role).OrderBy(x => x));
}
