using Diamono.Application.Bookings;
using Diamono.Application.Payments;
using Diamono.Domain.Bookings;
using Diamono.Domain.Notifications;
using Diamono.Domain.Payments;
using Diamono.Domain.Pricing;
using Diamono.Domain.Settings;
using Xunit;

namespace Diamono.Application.Tests;

public sealed class BookingNotificationTests
{
    private static readonly Guid ResourceId = Guid.Parse("7f099973-c811-4afe-beca-c9a1b8fcd001");

    private static DateTimeOffset At(int dayOffset, int hour)
        => new(DateTimeOffset.UtcNow.Date.AddDays(dayOffset).AddHours(hour), TimeSpan.Zero);

    private static Booking NewBooking() =>
        new(ResourceId, At(7, 18), At(7, 20), "Awa Diop", "+221 77 000 00 00",
            CustomerCategory.Individual, "Football",
            rentalAmount: 50_000m, lightingAmount: 5_000m, depositAmount: 25_000m);

    [Fact]
    public async Task BookingCreated_cree_notification()
    {
        var notifications = new FakeNotificationService();
        var service = Service(new FakeBookingRepository(), notifications);

        var booking = await service.CreateAsync(new CreateBookingRequest(
            ResourceId, At(10, 16), At(10, 18), "Awa Diop", "+221 77 000 00 00",
            CustomerCategory.Individual, "Football"));

        var notification = Assert.Single(notifications.Requests);
        Assert.Equal(booking.Id, notification.BookingId);
        Assert.Equal(NotificationTemplate.BookingCreated, notification.Template);
        Assert.Equal(NotificationChannel.Sms, notification.Channel);
        Assert.Equal($"Stade Diamono : demande {booking.Reference} recue. Nous vous informerons apres validation.", notification.Body);
    }

    [Fact]
    public async Task BookingApproved_cree_notification_avec_delai_paiement_configure()
    {
        var booking = NewBooking();
        var notifications = new FakeNotificationService();
        var settings = StadiumBookingSettings.MvpDefaults();
        settings.Update(settings.OpensAt, settings.ClosesAt, settings.MinimumDurationHours,
            settings.MaximumDurationHours, settings.StandardHourlyRate, settings.LocalAscHourlyRate,
            settings.LightingHourlyRate, settings.LightingStartsAt, settings.DepositAmount,
            settings.MaximumAdvanceBookingDays, paymentDeadlineHours: 8, settings.ApprovalRequired);

        await Service(new FakeBookingRepository(booking), notifications, settings).ApproveBookingAsync(booking.Id);

        var notification = Assert.Single(notifications.Requests);
        Assert.Equal(NotificationTemplate.BookingApproved, notification.Template);
        Assert.Contains("8h", notification.Body);
        Assert.DoesNotContain("24h", notification.Body);
        Assert.Contains("paymentDeadlineHours = 8", notification.Metadata!.ToString());
    }

    [Fact]
    public async Task BookingRejected_cree_notification()
    {
        var booking = NewBooking();
        var notifications = new FakeNotificationService();

        await Service(new FakeBookingRepository(booking), notifications).RejectBookingAsync(booking.Id, "Indisponible.");

        var notification = Assert.Single(notifications.Requests);
        Assert.Equal(NotificationTemplate.BookingRejected, notification.Template);
        Assert.Contains("Indisponible", notification.Body);
    }

    [Fact]
    public async Task BookingMarkedPaid_via_paiement_cree_notification()
    {
        var booking = NewBooking();
        booking.Approve();
        var notifications = new FakeNotificationService();

        var paymentService = new PaymentApplicationService(
            new FakePaymentRepository(),
            new FakeBookingRepository(booking),
            FakePermissionGuard.AllowAll(),
            new FakePaymentProvider(),
            new FakeAuditWriter(),
            notifications);

        await paymentService.MarkCashPaymentAsPaidAsync(new MarkCashPaymentAsPaidRequest(booking.Id, PaymentMethod.Cash));

        var notification = Assert.Single(notifications.Requests);
        Assert.Equal(NotificationTemplate.BookingMarkedPaid, notification.Template);
        Assert.Contains("paiement recu", notification.Body);
    }

    [Fact]
    public async Task BookingCancelled_cree_notification()
    {
        var booking = NewBooking();
        booking.Approve();
        var notifications = new FakeNotificationService();

        await Service(new FakeBookingRepository(booking), notifications).CancelBookingAsync(booking.Id, "Paiement non recu.");

        var notification = Assert.Single(notifications.Requests);
        Assert.Equal(NotificationTemplate.BookingCancelled, notification.Template);
        Assert.Contains("Paiement non recu", notification.Body);
    }

    [Fact]
    public async Task Erreur_metier_ne_cree_aucune_notification_success()
    {
        var booking = NewBooking();
        booking.Approve();
        booking.ConfirmPayment();
        var notifications = new FakeNotificationService();

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => Service(new FakeBookingRepository(booking), notifications).ApproveBookingAsync(booking.Id));

        Assert.Empty(notifications.Requests);
    }

    private static BookingApplicationService Service(
        FakeBookingRepository repository,
        FakeNotificationService notifications,
        StadiumBookingSettings? settings = null)
        => new(
            repository,
            new FakeStadiumBookingSettingsRepository(settings),
            new MvpPricingPolicy(),
            FakePermissionGuard.AllowAll(),
            new FakeAuditWriter(),
            notifications);
}
