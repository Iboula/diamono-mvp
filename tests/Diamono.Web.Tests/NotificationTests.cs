using Diamono.Application.Abstractions;
using Diamono.Application.Notifications;
using Diamono.Application.Security;
using Diamono.Domain.Bookings;
using Diamono.Domain.Notifications;
using Diamono.Domain.Security;
using Diamono.Infrastructure.Notifications;
using Diamono.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Diamono.Web.Tests;

public sealed class NotificationTests
{
    private static readonly Guid ResourceId = Guid.Parse("7f099973-c811-4afe-beca-c9a1b8fcd001");

    [Fact]
    public async Task Historique_notification_persiste_et_reste_consultable()
    {
        await using var provider = BuildProvider(hasAdministrationManage: true);
        var db = provider.GetRequiredService<DiamonoDbContext>();
        var booking = NewBooking();
        db.Bookings.Add(booking);

        await provider.GetRequiredService<INotificationService>().NotifyAsync(new NotificationRequest(
            booking.Id,
            booking.Phone,
            NotificationChannel.Sms,
            NotificationTemplate.BookingApproved,
            "Demande approuvee",
            "Votre demande a ete approuvee.",
            new { reference = booking.Reference, paymentDeadlineHours = 24 }));
        await db.SaveChangesAsync();

        var result = await provider.GetRequiredService<INotificationReader>()
            .GetNotificationsAsync(new NotificationQuery(Status: NotificationStatus.Sent, Channel: NotificationChannel.Sms));

        var notification = Assert.Single(result);
        Assert.Equal(booking.Reference, notification.BookingReference);
        Assert.Equal(NotificationTemplate.BookingApproved, notification.Template);
        Assert.Equal(NotificationStatus.Sent, notification.Status);
        Assert.Contains("paymentDeadlineHours", notification.MetadataJson);
    }

    [Fact]
    public async Task Notification_reader_exige_Administration_Manage()
    {
        await using var provider = BuildProvider(hasAdministrationManage: false);
        var reader = provider.GetRequiredService<INotificationReader>();

        var error = await Assert.ThrowsAsync<PermissionDeniedException>(
            () => reader.GetNotificationsAsync(new NotificationQuery()));

        Assert.Equal(Permissions.AdministrationManage, error.Permission);
    }

    [Fact]
    public async Task MetadataJson_ne_contient_pas_de_secret_provider()
    {
        await using var provider = BuildProvider(hasAdministrationManage: true);
        var db = provider.GetRequiredService<DiamonoDbContext>();
        var booking = NewBooking();
        db.Bookings.Add(booking);

        await provider.GetRequiredService<INotificationService>().NotifyAsync(new NotificationRequest(
            booking.Id,
            booking.Phone,
            NotificationChannel.Sms,
            NotificationTemplate.BookingCreated,
            "Demande recue",
            "Votre demande a bien ete recue.",
            new { reference = booking.Reference, status = booking.Status.ToString() }));
        await db.SaveChangesAsync();

        var serialized = Assert.Single(db.NotificationLogs).MetadataJson!;
        Assert.DoesNotContain("Password", serialized, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("Token", serialized, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("Secret", serialized, StringComparison.OrdinalIgnoreCase);
    }

    private static Booking NewBooking()
    {
        var day = DateTimeOffset.UtcNow.Date.AddDays(7);
        return new Booking(ResourceId, new DateTimeOffset(day.AddHours(18), TimeSpan.Zero),
            new DateTimeOffset(day.AddHours(20), TimeSpan.Zero),
            "Awa Diop", "+221 77 000 00 00", CustomerCategory.Individual,
            "Football", 50_000m, 5_000m, 25_000m);
    }

    private static ServiceProvider BuildProvider(bool hasAdministrationManage)
    {
        var services = new ServiceCollection();
        services.AddDbContext<DiamonoDbContext>(options => options.UseInMemoryDatabase(Guid.NewGuid().ToString()));
        services.AddSingleton<IPermissionGuard>(new TestPermissionGuard(hasAdministrationManage));
        services.AddScoped<INotificationProvider, DevelopmentNotificationProvider>();
        services.AddScoped<INotificationService, DevelopmentNotificationService>();
        services.AddScoped<INotificationReader, NotificationReader>();
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
