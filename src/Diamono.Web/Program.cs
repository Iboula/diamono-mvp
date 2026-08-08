using Diamono.Application.Abstractions;
using Diamono.Application.Payments;
using Diamono.Domain.Security;
using Diamono.Infrastructure;
using Diamono.Infrastructure.Identity;
using Diamono.Infrastructure.Persistence;
using Diamono.Web.Components;
using Diamono.Web.Health;
using Diamono.Web.Security;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Radzen;
using System.Threading.RateLimiting;

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
builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
    options.KnownIPNetworks.Clear();
    options.KnownProxies.Clear();
});
builder.Services.AddHttpsRedirection(options =>
{
    options.HttpsPort = 443;
});
builder.Services.AddHealthChecks()
    .AddCheck("self", () => Microsoft.Extensions.Diagnostics.HealthChecks.HealthCheckResult.Healthy(), tags: ["live"])
    .AddCheck<DiamonoReadinessHealthCheck>("diamono-ready", tags: ["ready"]);
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(context =>
    {
        var remoteIp = context.Connection.RemoteIpAddress?.ToString() ?? "unknown";
        if (context.Request.Path.StartsWithSegments("/login"))
        {
            return RateLimitPartition.GetFixedWindowLimiter($"login:{remoteIp}", _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 8,
                Window = TimeSpan.FromMinutes(1),
                QueueLimit = 0,
                AutoReplenishment = true
            });
        }

        if (context.Request.Path.StartsWithSegments("/admin"))
        {
            return RateLimitPartition.GetFixedWindowLimiter($"admin:{remoteIp}", _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 120,
                Window = TimeSpan.FromMinutes(1),
                QueueLimit = 0,
                AutoReplenishment = true
            });
        }

        return RateLimitPartition.GetNoLimiter($"public:{remoteIp}");
    });
    options.OnRejected = (context, cancellationToken) =>
    {
        var logger = context.HttpContext.RequestServices
            .GetRequiredService<ILoggerFactory>()
            .CreateLogger("Diamono.RateLimiting");
        logger.LogWarning(
            "Rate limit exceeded for {Path} from {RemoteIp}.",
            context.HttpContext.Request.Path,
            context.HttpContext.Connection.RemoteIpAddress);
        return ValueTask.CompletedTask;
    };
});

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
    app.UseForwardedHeaders();
    app.UseExceptionHandler(errorApp =>
    {
        errorApp.Run(async context =>
        {
            var exception = context.Features.Get<IExceptionHandlerFeature>()?.Error;
            var logger = context.RequestServices
                .GetRequiredService<ILoggerFactory>()
                .CreateLogger("Diamono.UnhandledException");
            logger.LogError(exception, "Unhandled exception for {Path}.", context.Request.Path);

            context.Response.StatusCode = StatusCodes.Status500InternalServerError;
            context.Response.ContentType = "text/plain; charset=utf-8";
            await context.Response.WriteAsync(
                "Une erreur inattendue est survenue. L'equipe du Stade Diamono a ete notifiee.");
        });
    });
    app.UseHsts();
    app.UseWhen(
        context => !context.Request.Path.StartsWithSegments("/health"),
        branch => branch.UseHttpsRedirection());
}

app.UseDiamonoSecurityHeaders();
app.UseRateLimiter();
app.UseAuthentication();
app.UseAuthorization();
app.UseAntiforgery();
app.MapStaticAssets();
app.MapHealthChecks("/health/live", new HealthCheckOptions
{
    Predicate = check => check.Tags.Contains("live"),
    ResponseWriter = DiamonoHealthCheckResponse.WriteAsync
}).AllowAnonymous();
app.MapHealthChecks("/health/ready", new HealthCheckOptions
{
    Predicate = check => check.Tags.Contains("ready"),
    ResponseWriter = DiamonoHealthCheckResponse.WriteAsync
}).AllowAnonymous();
app.MapGet("/admin/paiements/{paymentId:guid}/recu", async (
    Guid paymentId,
    IPaymentReceiptService receiptService,
    CancellationToken cancellationToken) =>
{
    var receipt = await receiptService.GenerateReceiptAsync(paymentId, cancellationToken);
    return Results.File(receipt.Content, receipt.ContentType, receipt.FileName);
}).RequireAuthorization(Permissions.PaymentsMarkPaid);
if (app.Environment.IsEnvironment("Testing"))
{
    app.MapGet("/__test/throw", (HttpContext _) =>
    {
        throw new InvalidOperationException("Sensitive stack trace marker.");
    });
}

app.MapRazorComponents<App>().AddInteractiveServerRenderMode();

await using (var scope = app.Services.CreateAsyncScope())
{
    var services = scope.ServiceProvider;
    var logger = services.GetRequiredService<ILoggerFactory>().CreateLogger("Diamono.Startup");
    var db = services.GetRequiredService<DiamonoDbContext>();

    logger.LogInformation(
        "Starting Diamono Web in {Environment}.",
        app.Environment.EnvironmentName);

    if (db.Database.IsRelational())
    {
        var pendingMigrations = (await db.Database.GetPendingMigrationsAsync()).ToList();
        logger.LogInformation(
            "Database migration state checked. Pending migrations: {PendingMigrationCount}.",
            pendingMigrations.Count);
    }

    await SeedData.InitializeAsync(db);

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
