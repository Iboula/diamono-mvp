using Diamono.Application.Bookings;
using Diamono.Domain.Notifications;
using Diamono.Domain.Bookings;
using Diamono.Domain.Pricing;
using Xunit;

namespace Diamono.Application.Tests;

public sealed class BookingApplicationServiceWorkflowTests
{
    private static readonly Guid ResourceId = Guid.Parse("7f099973-c811-4afe-beca-c9a1b8fcd001");

    private static Booking NewBooking() =>
        new(
            ResourceId,
            new DateTimeOffset(2026, 8, 15, 18, 0, 0, TimeSpan.Zero),
            new DateTimeOffset(2026, 8, 15, 20, 0, 0, TimeSpan.Zero),
            "Awa Diop",
            "+221 77 000 00 00",
            CustomerCategory.Individual,
            "Football",
            rentalAmount: 50_000m,
            lightingAmount: 5_000m,
            depositAmount: 25_000m);

    // Ces tests portent sur le workflow, pas sur l'autorisation : garde permissif.
    private static BookingApplicationService Service(FakeBookingRepository repository) =>
        new(repository, new FakeStadiumBookingSettingsRepository(), new MvpPricingPolicy(),
            FakePermissionGuard.AllowAll(), new FakeAuditWriter(), new FakeNotificationService());

    [Fact]
    public async Task GetBackOfficeBookingsAsync_retourne_les_reservations_pour_le_tableau_de_bord()
    {
        var booking = NewBooking();
        var repository = new FakeBookingRepository(booking);

        var result = await Service(repository).GetBackOfficeBookingsAsync();

        Assert.Single(result);
        Assert.Equal(booking.Id, result[0].Id);
    }

    [Fact]
    public async Task ApproveBookingAsync_passe_de_pending_approval_a_awaiting_payment()
    {
        var booking = NewBooking();
        var repository = new FakeBookingRepository(booking);

        await Service(repository).ApproveBookingAsync(booking.Id);

        Assert.Equal(BookingStatus.AwaitingPayment, booking.Status);
        Assert.NotNull(booking.ApprovedAt);
        Assert.Equal(1, repository.SaveChangesCallCount);
    }

    [Fact]
    public async Task MarkBookingAsPaidAsync_direct_est_remplace_par_payment_service()
    {
        var booking = NewBooking();
        booking.Approve();
        var repository = new FakeBookingRepository(booking);

        await Assert.ThrowsAsync<InvalidOperationException>(() => Service(repository).MarkBookingAsPaidAsync(booking.Id));

        Assert.Equal(BookingStatus.AwaitingPayment, booking.Status);
        Assert.Null(booking.PaidAt);
        Assert.Equal(0, repository.SaveChangesCallCount);
    }

    [Fact]
    public async Task RejectBookingAsync_passe_de_pending_approval_a_rejected()
    {
        var booking = NewBooking();
        var repository = new FakeBookingRepository(booking);

        await Service(repository).RejectBookingAsync(booking.Id, "Creneau reserve pour un evenement municipal.");

        Assert.Equal(BookingStatus.Rejected, booking.Status);
        Assert.NotNull(booking.RejectedAt);
        Assert.Equal("Creneau reserve pour un evenement municipal.", booking.RejectionReason);
        Assert.Equal(1, repository.SaveChangesCallCount);
    }

    [Fact]
    public async Task ApproveBookingAsync_rejette_une_reservation_deja_confirmed()
    {
        var booking = NewBooking();
        booking.Approve();
        booking.ConfirmPayment();
        var repository = new FakeBookingRepository(booking);

        await Assert.ThrowsAsync<InvalidOperationException>(() => Service(repository).ApproveBookingAsync(booking.Id));

        Assert.Equal(BookingStatus.Confirmed, booking.Status);
        Assert.Equal(0, repository.SaveChangesCallCount);
    }

    [Fact]
    public async Task MarkBookingAsPaidAsync_rejette_une_reservation_pending_approval()
    {
        var booking = NewBooking();
        var repository = new FakeBookingRepository(booking);

        await Assert.ThrowsAsync<InvalidOperationException>(() => Service(repository).MarkBookingAsPaidAsync(booking.Id));

        Assert.Equal(BookingStatus.PendingApproval, booking.Status);
        Assert.Null(booking.PaidAt);
        Assert.Equal(0, repository.SaveChangesCallCount);
    }

    [Fact]
    public async Task RejectBookingAsync_rejette_une_reservation_confirmed()
    {
        var booking = NewBooking();
        booking.Approve();
        booking.ConfirmPayment();
        var repository = new FakeBookingRepository(booking);

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => Service(repository).RejectBookingAsync(booking.Id, "Le client annule."));

        Assert.Equal(BookingStatus.Confirmed, booking.Status);
        Assert.Null(booking.RejectionReason);
        Assert.Equal(0, repository.SaveChangesCallCount);
    }
}
