using Diamono.Application.Payments;
using Diamono.Domain.Audit;
using Diamono.Domain.Bookings;
using Diamono.Domain.Notifications;
using Diamono.Domain.Payments;
using Diamono.Domain.Security;
using Xunit;

namespace Diamono.Application.Tests;

public sealed class PaymentApplicationServiceTests
{
    private static readonly Guid ResourceId = Guid.Parse("7f099973-c811-4afe-beca-c9a1b8fcd001");

    [Fact]
    public async Task Creation_paiement_pending()
    {
        var booking = AwaitingPaymentBooking();
        var repository = new FakePaymentRepository();

        var payment = await Service(repository, new FakeBookingRepository(booking)).CreatePaymentAsync(
            new CreatePaymentRequest(booking.Id, PaymentMethod.Cash));

        Assert.Equal(PaymentStatus.Pending, payment.Status);
        Assert.Equal(booking.TotalAmount, payment.Amount);
        Assert.Equal(Payment.MvpCurrency, payment.Currency);
        Assert.Equal(1, repository.SaveChangesCallCount);
    }

    [Fact]
    public async Task Paiement_cash_passe_paid_et_confirme_booking()
    {
        var booking = AwaitingPaymentBooking();
        var payment = await Service(new FakePaymentRepository(), new FakeBookingRepository(booking))
            .MarkCashPaymentAsPaidAsync(new MarkCashPaymentAsPaidRequest(booking.Id, PaymentMethod.Cash));

        Assert.Equal(PaymentStatus.Paid, payment.Status);
        Assert.NotNull(payment.PaidAt);
        Assert.Equal(BookingStatus.Confirmed, booking.Status);
        Assert.NotNull(booking.PaidAt);
    }

    [Fact]
    public async Task Paiement_failed_laisse_booking_awaiting_payment()
    {
        var booking = AwaitingPaymentBooking();
        var payment = await Service(
                new FakePaymentRepository(),
                new FakeBookingRepository(booking),
                provider: new FakePaymentProvider(PaymentProviderResult.Failed("Refuse")))
            .MarkCashPaymentAsPaidAsync(new MarkCashPaymentAsPaidRequest(booking.Id, PaymentMethod.MobileMoney));

        Assert.Equal(PaymentStatus.Failed, payment.Status);
        Assert.Equal(BookingStatus.AwaitingPayment, booking.Status);
    }

    [Fact]
    public async Task Montant_paiement_reste_celui_du_booking()
    {
        var booking = AwaitingPaymentBooking(totalRental: 75_000m);
        var payment = await Service(new FakePaymentRepository(), new FakeBookingRepository(booking))
            .CreatePaymentAsync(new CreatePaymentRequest(booking.Id, PaymentMethod.BankTransfer));

        Assert.Equal(105_000m, payment.Amount);
    }

    [Fact]
    public async Task Tarif_modifie_apres_booking_ne_change_pas_montant_paiement()
    {
        var booking = AwaitingPaymentBooking(totalRental: 75_000m);
        var payment = await Service(new FakePaymentRepository(), new FakeBookingRepository(booking))
            .MarkCashPaymentAsPaidAsync(new MarkCashPaymentAsPaidRequest(booking.Id, PaymentMethod.Cash));

        Assert.Equal(105_000m, payment.Amount);
    }

    [Fact]
    public async Task Impossible_de_payer_deux_fois_completement()
    {
        var booking = AwaitingPaymentBooking();
        var repository = new FakePaymentRepository();
        var service = Service(repository, new FakeBookingRepository(booking));

        await service.MarkCashPaymentAsPaidAsync(new MarkCashPaymentAsPaidRequest(booking.Id, PaymentMethod.Cash));

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => service.MarkCashPaymentAsPaidAsync(new MarkCashPaymentAsPaidRequest(booking.Id, PaymentMethod.Cash)));
    }

    [Fact]
    public async Task Audit_payment_paid_cree()
    {
        var booking = AwaitingPaymentBooking();
        var audit = new FakeAuditWriter();

        await Service(new FakePaymentRepository(), new FakeBookingRepository(booking), audit: audit)
            .MarkCashPaymentAsPaidAsync(new MarkCashPaymentAsPaidRequest(booking.Id, PaymentMethod.Cash));

        Assert.Contains(audit.Entries, x => x.Action == AuditActions.PaymentPaid);
        Assert.Contains(audit.Entries, x => x.Action == AuditActions.BookingMarkedPaid);
    }

    [Fact]
    public async Task Notification_envoyee_apres_paiement_reussi()
    {
        var booking = AwaitingPaymentBooking();
        var notifications = new FakeNotificationService();

        await Service(new FakePaymentRepository(), new FakeBookingRepository(booking), notifications: notifications)
            .MarkCashPaymentAsPaidAsync(new MarkCashPaymentAsPaidRequest(booking.Id, PaymentMethod.Cash));

        Assert.Equal(NotificationTemplate.BookingMarkedPaid, Assert.Single(notifications.Requests).Template);
    }

    [Fact]
    public async Task Aucune_notification_si_paiement_echoue()
    {
        var booking = AwaitingPaymentBooking();
        var notifications = new FakeNotificationService();

        await Service(
                new FakePaymentRepository(),
                new FakeBookingRepository(booking),
                provider: new FakePaymentProvider(PaymentProviderResult.Failed("Refuse")),
                notifications: notifications)
            .MarkCashPaymentAsPaidAsync(new MarkCashPaymentAsPaidRequest(booking.Id, PaymentMethod.Cash));

        Assert.Empty(notifications.Requests);
    }

    [Fact]
    public async Task Permission_payments_markpaid_requise()
    {
        var booking = AwaitingPaymentBooking();
        var service = Service(
            new FakePaymentRepository(),
            new FakeBookingRepository(booking),
            permissionGuard: FakePermissionGuard.ForRoles(DiamonoRoles.Gestionnaire));

        var error = await Assert.ThrowsAsync<Diamono.Application.Security.PermissionDeniedException>(
            () => service.MarkCashPaymentAsPaidAsync(new MarkCashPaymentAsPaidRequest(booking.Id, PaymentMethod.Cash)));

        Assert.Equal(Permissions.PaymentsMarkPaid, error.Permission);
    }

    private static Booking AwaitingPaymentBooking(decimal totalRental = 50_000m)
    {
        var booking = new Booking(
            ResourceId,
            new DateTimeOffset(2026, 8, 15, 18, 0, 0, TimeSpan.Zero),
            new DateTimeOffset(2026, 8, 15, 20, 0, 0, TimeSpan.Zero),
            "Awa Diop",
            "+221 77 000 00 00",
            CustomerCategory.Individual,
            "Football",
            totalRental,
            lightingAmount: 5_000m,
            depositAmount: 25_000m);
        booking.Approve();
        return booking;
    }

    private static PaymentApplicationService Service(
        FakePaymentRepository repository,
        FakeBookingRepository bookingRepository,
        FakePaymentProvider? provider = null,
        FakeAuditWriter? audit = null,
        FakeNotificationService? notifications = null,
        FakePermissionGuard? permissionGuard = null)
        => new(
            repository,
            bookingRepository,
            permissionGuard ?? FakePermissionGuard.AllowAll(),
            provider ?? new FakePaymentProvider(),
            audit ?? new FakeAuditWriter(),
            notifications ?? new FakeNotificationService());
}
