using System.Security.Claims;
using Diamono.Domain.Security;
using Diamono.Web.Security;
using Xunit;

namespace Diamono.Web.Tests;

public sealed class PermissionClaimsTransformationTests
{
    private static readonly PermissionClaimsTransformation Transformation = new();

    [Fact]
    public async Task Un_principal_anonyme_reste_sans_permission()
    {
        var principal = await Transformation.TransformAsync(PrincipalFactory.Anonymous());

        Assert.Empty(principal.FindAll(Permissions.ClaimType));
    }

    [Fact]
    public async Task Les_roles_sont_traduits_en_claims_de_permission()
    {
        var principal = PrincipalFactory.ForRoles(DiamonoRoles.Caissier);
        var permissions = principal.FindAll(Permissions.ClaimType).Select(x => x.Value).ToList();

        Assert.Equal(
            PermissionMatrix.PermissionsFor(DiamonoRoles.Caissier).OrderBy(x => x),
            permissions.OrderBy(x => x));

        await Task.CompletedTask;
    }

    [Fact]
    public async Task La_transformation_est_idempotente()
    {
        var principal = PrincipalFactory.ForRoles(DiamonoRoles.Gestionnaire);
        var before = principal.FindAll(Permissions.ClaimType).Count();

        await Transformation.TransformAsync(principal);
        await Transformation.TransformAsync(principal);

        Assert.Equal(before, principal.FindAll(Permissions.ClaimType).Count());
    }

    [Fact]
    public void Un_role_inconnu_n_accorde_rien()
    {
        var principal = PrincipalFactory.ForRoles("RoleInexistant");

        Assert.Empty(principal.FindAll(Permissions.ClaimType));
        Assert.True(principal.Identity?.IsAuthenticated);
    }

    [Fact]
    public void Le_cumul_de_roles_additionne_les_permissions()
    {
        var principal = PrincipalFactory.ForRoles(DiamonoRoles.Caissier, DiamonoRoles.Lecteur);
        var permissions = principal.FindAll(Permissions.ClaimType).Select(x => x.Value).ToList();

        Assert.Contains(Permissions.PaymentsMarkPaid, permissions);
        Assert.Contains(Permissions.SettingsView, permissions);
        Assert.DoesNotContain(Permissions.BookingsApprove, permissions);
        Assert.Equal(permissions.Count, permissions.Distinct().Count());
    }

    [Fact]
    public async Task Le_type_de_claim_de_role_de_l_identite_est_respecte()
    {
        // Un fournisseur OIDC peut poser les roles sur un autre type de claim.
        var identity = new ClaimsIdentity("oidc", ClaimTypes.Name, roleType: "roles");
        identity.AddClaim(new Claim("roles", DiamonoRoles.Lecteur));

        var principal = await Transformation.TransformAsync(new ClaimsPrincipal(identity));

        Assert.Contains(Permissions.BookingsView, principal.FindAll(Permissions.ClaimType).Select(x => x.Value));
    }
}
