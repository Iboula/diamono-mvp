using Diamono.Application.Abstractions;
using Diamono.Application.Availability;
using Diamono.Application.Bookings;
using Diamono.Application.Notifications;
using Diamono.Application.Payments;
using Diamono.Application.Reporting;
using Diamono.Application.Settings;
using Diamono.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Diamono.Web.Health;

public sealed class DiamonoReadinessHealthCheck : IHealthCheck
{
    private readonly IServiceScopeFactory scopeFactory;
    private readonly ILogger<DiamonoReadinessHealthCheck> logger;

    public DiamonoReadinessHealthCheck(
        IServiceScopeFactory scopeFactory,
        ILogger<DiamonoReadinessHealthCheck> logger)
    {
        this.scopeFactory = scopeFactory;
        this.logger = logger;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        try
        {
            await using var scope = scopeFactory.CreateAsyncScope();
            var services = scope.ServiceProvider;
            var db = services.GetRequiredService<DiamonoDbContext>();

            if (!await db.Database.CanConnectAsync(cancellationToken))
                return HealthCheckResult.Unhealthy("Database unavailable.");

            if (db.Database.IsRelational())
            {
                var pendingMigrations = await db.Database.GetPendingMigrationsAsync(cancellationToken);
                _ = pendingMigrations.ToList();
            }

            services.GetRequiredService<BookingApplicationService>();
            services.GetRequiredService<PaymentApplicationService>();
            services.GetRequiredService<BookingBlockApplicationService>();
            services.GetRequiredService<AvailabilityApplicationService>();
            services.GetRequiredService<ReportingApplicationService>();
            services.GetRequiredService<StadiumSettingsApplicationService>();
            services.GetRequiredService<INotificationService>();
            services.GetRequiredService<IPaymentReceiptService>();
            services.GetRequiredService<IPermissionGuard>();

            return HealthCheckResult.Healthy();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Readiness health check failed.");
            return HealthCheckResult.Unhealthy("Readiness check failed.");
        }
    }
}

public static class DiamonoHealthCheckResponse
{
    public static Task WriteAsync(HttpContext context, HealthReport report)
    {
        context.Response.ContentType = "application/json";
        return context.Response.WriteAsJsonAsync(new
        {
            status = report.Status.ToString()
        });
    }
}
