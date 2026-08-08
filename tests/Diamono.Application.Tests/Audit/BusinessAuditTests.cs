using Diamono.Application.Bookings;
using Diamono.Application.Payments;
using Diamono.Application.Settings;
using Diamono.Domain.Audit;
using Diamono.Domain.Bookings;
using Diamono.Domain.Pricing;
using Diamono.Domain.Payments;
using Diamono.Domain.Security;
using Xunit;

namespace Diamono.Application.Tests;

public sealed class BusinessAuditTests
{
    private static readonly Guid ResourceId = Guid.Parse("7f099973-c811-4afe-beca-c9a1b8fcd001");

    private static DateTimeOffset At(int hour) => new(2026, 8, 15, hour, 0, 0, TimeSpan.Zero);

    private static Booking NewBooking() =>
        new(ResourceId, At(18), At(20), "Awa Diop", "+221 77 000 00 00",
            CustomerCategory.Individual, "Football",
            rentalAmount: 50_000m, lightingAmount: 5_000m, depositAmount: 25_000m);

    [Fact]
    public async Task Approve_booking_cree_AuditEntry()
    {
        var booking = NewBooking();
        var audit = new FakeAuditWriter();
        var service = BookingService(new FakeBookingRepository(booking), audit);

        await service.ApproveBookingAsync(booking.Id);

        var entry = Assert.Single(audit.Entries);
        Assert.Equal(AuditActions.BookingApproved, entry.Action);
        Assert.Contains("PendingApproval", entry.OldValues!.ToString());
        Assert.Contains("AwaitingPayment", entry.NewValues!.ToString());
    }

    [Fact]
    public async Task Reject_booking_cree_AuditEntry()
    {
        var booking = NewBooking();
        var audit = new FakeAuditWriter();
        var service = BookingService(new FakeBookingRepository(booking), audit);

        await service.RejectBookingAsync(booking.Id, "Evenement prioritaire.");

        Assert.Equal(AuditActions.BookingRejected, Assert.Single(audit.Entries).Action);
    }

    [Fact]
    public async Task MarkPaid_cree_AuditEntry()
    {
        var booking = NewBooking();
        booking.Approve();
        var audit = new FakeAuditWriter();
        var service = new PaymentApplicationService(
            new FakePaymentRepository(),
            new FakeBookingRepository(booking),
            FakePermissionGuard.AllowAll(),
            new FakePaymentProvider(),
            audit,
            new FakeNotificationService());

        await service.MarkCashPaymentAsPaidAsync(new MarkCashPaymentAsPaidRequest(booking.Id, PaymentMethod.Cash));

        Assert.Contains(audit.Entries, x => x.Action == AuditActions.PaymentPaid);
        Assert.Contains(audit.Entries, x => x.Action == AuditActions.BookingMarkedPaid);
    }

    [Fact]
    public async Task BookingBlock_create_cree_AuditEntry()
    {
        var audit = new FakeAuditWriter();
        var service = new BookingBlockApplicationService(
            new FakeBookingBlockRepository(),
            new FakeBookingRepository(),
            FakePermissionGuard.AllowAll(),
            audit);

        await service.CreateBookingBlockAsync(new CreateBookingBlockRequest(
            ResourceId, At(8), At(10), BookingBlockType.Maintenance, "Entretien."));

        Assert.Equal(AuditActions.BookingBlockCreated, Assert.Single(audit.Entries).Action);
    }

    [Fact]
    public async Task Settings_update_capture_old_new_values()
    {
        var audit = new FakeAuditWriter();
        var service = new StadiumSettingsApplicationService(
            new FakeStadiumBookingSettingsRepository(),
            FakePermissionGuard.AllowAll(),
            audit);

        await service.UpdateSettingsAsync(new UpdateStadiumBookingSettingsRequest(
            new TimeOnly(7, 0),
            new TimeOnly(22, 0),
            1,
            5,
            30_000m,
            12_000m,
            7_500m,
            new TimeOnly(18, 0),
            20_000m,
            45,
            12,
            true));

        var entry = Assert.Single(audit.Entries);
        Assert.Equal(AuditActions.SettingsUpdated, entry.Action);
        Assert.Contains("25000", entry.OldValues!.ToString());
        Assert.Contains("30000", entry.NewValues!.ToString());
    }

    [Fact]
    public async Task Action_refusee_ne_cree_pas_audit_success()
    {
        var booking = NewBooking();
        var audit = new FakeAuditWriter();
        var service = new BookingApplicationService(
            new FakeBookingRepository(booking),
            new FakeStadiumBookingSettingsRepository(),
            new MvpPricingPolicy(),
            FakePermissionGuard.ForRoles(DiamonoRoles.Lecteur),
            audit,
            new FakeNotificationService());

        await Assert.ThrowsAsync<Diamono.Application.Security.PermissionDeniedException>(
            () => service.ApproveBookingAsync(booking.Id));

        Assert.Empty(audit.Entries);
    }

    private static BookingApplicationService BookingService(FakeBookingRepository repository, FakeAuditWriter audit)
        => new(repository, new FakeStadiumBookingSettingsRepository(), new MvpPricingPolicy(),
            FakePermissionGuard.AllowAll(), audit, new FakeNotificationService());
}
