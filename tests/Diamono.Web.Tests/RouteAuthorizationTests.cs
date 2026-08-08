using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Components;
using Diamono.Domain.Security;
using Xunit;

namespace Diamono.Web.Tests;

/// <summary>
/// Verifie quelles routes exigent une autorisation. Le site public doit rester
/// anonyme et le backoffice doit etre couvert : c'est <c>AuthorizeRouteView</c>
/// qui redirige les anonymes vers /login.
/// </summary>
public sealed class RouteAuthorizationTests
{
    private static readonly IReadOnlyList<Type> RoutableComponents =
    [
        .. typeof(Diamono.Web.Components.App).Assembly
            .GetTypes()
            .Where(t => typeof(IComponent).IsAssignableFrom(t))
            .Where(t => t.GetCustomAttributes(typeof(RouteAttribute), inherit: true).Length > 0)
    ];

    private static Type ComponentForRoute(string template)
        => RoutableComponents.Single(t => t
            .GetCustomAttributes(typeof(RouteAttribute), inherit: true)
            .Cast<RouteAttribute>()
            .Any(r => string.Equals(r.Template, template, StringComparison.OrdinalIgnoreCase)));

    private static AuthorizeAttribute? AuthorizeFor(string template)
        => ComponentForRoute(template)
            .GetCustomAttributes(typeof(AuthorizeAttribute), inherit: true)
            .Cast<AuthorizeAttribute>()
            .FirstOrDefault();

    [Theory]
    [InlineData("/")]
    [InlineData("/disponibilites")]
    [InlineData("/reserve")]
    [InlineData("/login")]
    public void Le_site_public_reste_accessible_anonymement(string template)
        => Assert.Null(AuthorizeFor(template));

    [Fact]
    public void Admin_exige_la_permission_Bookings_View()
    {
        var authorize = AuthorizeFor("/admin");

        Assert.NotNull(authorize);
        Assert.Equal(Permissions.BookingsView, authorize.Policy);
    }

    [Fact]
    public void Admin_parametres_exige_la_permission_Settings_View()
    {
        var authorize = AuthorizeFor("/admin/parametres");

        Assert.NotNull(authorize);
        Assert.Equal(Permissions.SettingsView, authorize.Policy);
    }

    [Fact]
    public void Toute_route_admin_est_protegee()
    {
        var unprotected = RoutableComponents
            .Where(t => t.GetCustomAttributes(typeof(RouteAttribute), inherit: true)
                .Cast<RouteAttribute>()
                .Any(r => r.Template.StartsWith("/admin", StringComparison.OrdinalIgnoreCase)))
            .Where(t => t.GetCustomAttributes(typeof(AuthorizeAttribute), inherit: true).Length == 0)
            .Select(t => t.FullName)
            .ToList();

        Assert.Empty(unprotected);
    }

    [Fact]
    public void Les_policies_referencees_par_les_routes_existent()
    {
        var referenced = RoutableComponents
            .SelectMany(t => t.GetCustomAttributes(typeof(AuthorizeAttribute), inherit: true).Cast<AuthorizeAttribute>())
            .Select(a => a.Policy)
            .Where(p => !string.IsNullOrEmpty(p))!
            .Distinct();

        Assert.All(referenced, policy => Assert.Contains(policy, Permissions.All));
    }
}
