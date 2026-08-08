using Diamono.Domain.Security;
using Diamono.Web.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Diamono.Web.Tests;

/// <summary>
/// Evalue les policies reellement enregistrees par l'application
/// (<see cref="DiamonoAuthorizationExtensions.AddDiamonoAuthorization"/>),
/// sur des principals produits par la vraie transformation de claims.
/// </summary>
public sealed class AuthorizationPolicyTests
{
    private static readonly IAuthorizationService Authorization = BuildAuthorizationService();

    private static IAuthorizationService BuildAuthorizationService()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddDiamonoAuthorization();
        return services.BuildServiceProvider().GetRequiredService<IAuthorizationService>();
    }

    private static async Task<bool> AllowedAsync(string role, string permission)
    {
        var result = await Authorization.AuthorizeAsync(PrincipalFactory.ForRoles(role), permission);
        return result.Succeeded;
    }

    [Fact]
    public async Task Une_policy_est_declaree_pour_chaque_permission()
    {
        var provider = new ServiceCollection()
            .AddLogging()
            .AddDiamonoAuthorization()
            .BuildServiceProvider()
            .GetRequiredService<IAuthorizationPolicyProvider>();

        foreach (var permission in Permissions.All)
            Assert.NotNull(await provider.GetPolicyAsync(permission));
    }

    [Fact]
    public async Task Anonyme_ne_satisfait_aucune_policy()
    {
        foreach (var permission in Permissions.All)
        {
            var result = await Authorization.AuthorizeAsync(PrincipalFactory.Anonymous(), permission);
            Assert.False(result.Succeeded);
        }
    }

    [Fact]
    public async Task SuperAdmin_satisfait_toutes_les_policies()
    {
        foreach (var permission in Permissions.All)
            Assert.True(await AllowedAsync(DiamonoRoles.SuperAdmin, permission), permission);
    }

    [Theory]
    [InlineData(DiamonoRoles.Gestionnaire, Permissions.BookingsView, true)]
    [InlineData(DiamonoRoles.Gestionnaire, Permissions.BookingsApprove, true)]
    [InlineData(DiamonoRoles.Gestionnaire, Permissions.BookingsReject, true)]
    [InlineData(DiamonoRoles.Gestionnaire, Permissions.BookingBlocksManage, true)]
    [InlineData(DiamonoRoles.Gestionnaire, Permissions.SettingsView, true)]
    [InlineData(DiamonoRoles.Gestionnaire, Permissions.PaymentsMarkPaid, false)]
    [InlineData(DiamonoRoles.Gestionnaire, Permissions.SettingsManage, false)]
    [InlineData(DiamonoRoles.Caissier, Permissions.BookingsView, true)]
    [InlineData(DiamonoRoles.Caissier, Permissions.PaymentsMarkPaid, true)]
    [InlineData(DiamonoRoles.Caissier, Permissions.BookingsApprove, false)]
    [InlineData(DiamonoRoles.Caissier, Permissions.SettingsView, false)]
    [InlineData(DiamonoRoles.Caissier, Permissions.SettingsManage, false)]
    [InlineData(DiamonoRoles.Lecteur, Permissions.BookingsView, true)]
    [InlineData(DiamonoRoles.Lecteur, Permissions.BookingBlocksView, true)]
    [InlineData(DiamonoRoles.Lecteur, Permissions.SettingsView, true)]
    [InlineData(DiamonoRoles.Lecteur, Permissions.BookingsApprove, false)]
    [InlineData(DiamonoRoles.Lecteur, Permissions.BookingsReject, false)]
    [InlineData(DiamonoRoles.Lecteur, Permissions.PaymentsMarkPaid, false)]
    [InlineData(DiamonoRoles.Lecteur, Permissions.BookingBlocksManage, false)]
    [InlineData(DiamonoRoles.Lecteur, Permissions.SettingsManage, false)]
    public async Task La_policy_reflete_la_matrice(string role, string permission, bool expected)
        => Assert.Equal(expected, await AllowedAsync(role, permission));

    [Fact]
    public async Task Administration_Manage_est_reservee_au_SuperAdmin()
    {
        Assert.True(await AllowedAsync(DiamonoRoles.SuperAdmin, Permissions.AdministrationManage));
        Assert.False(await AllowedAsync(DiamonoRoles.Gestionnaire, Permissions.AdministrationManage));
        Assert.False(await AllowedAsync(DiamonoRoles.Caissier, Permissions.AdministrationManage));
        Assert.False(await AllowedAsync(DiamonoRoles.Lecteur, Permissions.AdministrationManage));
    }
}
