using Diamono.Application.Abstractions;
using Diamono.Infrastructure;
using Diamono.Infrastructure.Identity;
using Diamono.Infrastructure.Persistence;
using Diamono.Web.Components;
using Diamono.Web.Security;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Radzen;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddRazorComponents().AddInteractiveServerComponents();
builder.Services.AddRadzenComponents();
builder.Services.AddDiamonoInfrastructure(builder.Configuration);
builder.Services.AddDiamonoIdentity();
builder.Services.AddDiamonoAuthorization();
builder.Services.AddCascadingAuthenticationState();

// Un seul objet implemente les deux contrats : autorisation des cas d'usage
// et acces a l'identite courante (preparation de l'audit DIA-017).
builder.Services.AddScoped<PermissionGuard>();
builder.Services.AddScoped<IPermissionGuard>(sp => sp.GetRequiredService<PermissionGuard>());
builder.Services.AddScoped<ICurrentUserAccessor>(sp => sp.GetRequiredService<PermissionGuard>());

builder.Services.ConfigureApplicationCookie(options =>
{
    options.Cookie.Name = "diamono.auth";
    options.Cookie.HttpOnly = true;
    options.Cookie.SameSite = SameSiteMode.Lax;
    // Hors developpement le cookie n'est jamais emis en clair.
    options.Cookie.SecurePolicy = builder.Environment.IsDevelopment()
        ? CookieSecurePolicy.SameAsRequest
        : CookieSecurePolicy.Always;

    options.LoginPath = "/login";
    options.LogoutPath = "/logout";
    options.AccessDeniedPath = "/acces-refuse";
    options.ReturnUrlParameter = "returnUrl";
    options.ExpireTimeSpan = TimeSpan.FromHours(8);
    options.SlidingExpiration = true;
});

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();
app.UseAntiforgery();
app.MapStaticAssets();
app.MapRazorComponents<App>().AddInteractiveServerRenderMode();

await using (var scope = app.Services.CreateAsyncScope())
{
    var services = scope.ServiceProvider;
    await SeedData.InitializeAsync(services.GetRequiredService<DiamonoDbContext>());

    await IdentitySeed.SeedRolesAsync(services.GetRequiredService<RoleManager<ApplicationRole>>());

    // Le compte de demonstration n'existe qu'en dehors de la production.
    if (!app.Environment.IsProduction())
    {
        await IdentitySeed.SeedDemoAdminAsync(
            services.GetRequiredService<UserManager<ApplicationUser>>(),
            app.Configuration[IdentitySeed.AdminPasswordConfigurationKey],
            services.GetRequiredService<ILoggerFactory>().CreateLogger(typeof(IdentitySeed)));
    }
}

app.Run();

/// <summary>Expose le point d'entree aux projets de tests.</summary>
public partial class Program;
