using Diamono.Application.Abstractions;
using Diamono.Application.Audit;
using Diamono.Application.Security;
using Diamono.Domain.Audit;
using Diamono.Domain.Security;
using Diamono.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Diamono.Web.Tests;

public sealed class AuditReaderTests
{
    [Fact]
    public async Task Audit_reader_filtrage_fonctionne()
    {
        await using var provider = BuildProvider(hasAdministrationManage: true);
        var db = provider.GetRequiredService<DiamonoDbContext>();
        db.AuditLogs.Add(new AuditEntry(
            "user-1",
            "admin@diamono.local",
            AuditActions.BookingApproved,
            "Booking",
            Guid.NewGuid().ToString(),
            "Reservation approuvee."));
        db.AuditLogs.Add(new AuditEntry(
            "user-2",
            "cashier@diamono.local",
            AuditActions.UserDisabled,
            "ApplicationUser",
            Guid.NewGuid().ToString(),
            "Utilisateur desactive."));
        await db.SaveChangesAsync();

        var reader = provider.GetRequiredService<IAuditReader>();

        var result = await reader.GetAuditEntriesAsync(new AuditQuery(
            User: "admin",
            Action: AuditActions.BookingApproved,
            EntityType: "Booking"));

        var entry = Assert.Single(result);
        Assert.Equal("admin@diamono.local", entry.UserEmail);
        Assert.Equal(AuditActions.BookingApproved, entry.Action);
        Assert.Equal("Booking", entry.EntityType);
    }

    [Fact]
    public async Task Audit_reader_exige_Administration_Manage()
    {
        await using var provider = BuildProvider(hasAdministrationManage: false);
        var reader = provider.GetRequiredService<IAuditReader>();

        var error = await Assert.ThrowsAsync<PermissionDeniedException>(
            () => reader.GetAuditEntriesAsync(new AuditQuery()));

        Assert.Equal(Permissions.AdministrationManage, error.Permission);
    }

    private static ServiceProvider BuildProvider(bool hasAdministrationManage)
    {
        var services = new ServiceCollection();
        services.AddDbContext<DiamonoDbContext>(options => options.UseInMemoryDatabase(Guid.NewGuid().ToString()));
        services.AddSingleton<IPermissionGuard>(new TestPermissionGuard(hasAdministrationManage));
        services.AddScoped<IAuditReader, AuditReader>();
        return services.BuildServiceProvider();
    }

    private sealed class TestPermissionGuard(bool hasAdministrationManage) : IPermissionGuard
    {
        public Task<bool> HasPermissionAsync(string permission, CancellationToken cancellationToken = default)
            => Task.FromResult(hasAdministrationManage && permission == Permissions.AdministrationManage);

        public Task EnsurePermissionAsync(string permission, CancellationToken cancellationToken = default)
            => hasAdministrationManage && permission == Permissions.AdministrationManage
                ? Task.CompletedTask
                : throw new PermissionDeniedException(permission);
    }
}
