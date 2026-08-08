using Diamono.Application.Bookings;
using Diamono.Application.Payments;
using Diamono.Application.Security;
using Diamono.Application.Settings;
using Diamono.Domain.Bookings;
using Diamono.Domain.Payments;
using Diamono.Domain.Pricing;
using Diamono.Domain.Security;
using Xunit;

namespace Diamono.Application.Tests;

/// <summary>
/// Autorisation cote cas d'usage : c'est la barriere qui compte, un bouton masque
/// n'etant pas une securite. Chaque test part d'un role reel de la matrice.
/// </summary>
public sealed class UseCasePermissionTests
{
    private static readonly Guid ResourceId = Guid.Parse("7f099973-c811-4afe-beca-c9a1b8fcd001");

    private static DateTimeOffset At(int hour) => new(2026, 8, 15, hour, 0, 0, TimeSpan.Zero);

    private static Booking NewBooking() =>
        new(ResourceId, At(18), At(20), "Awa Diop", "+221 77 000 00 00",
            CustomerCategory.Individual, "Football",
            rentalAmount: 50_000m, lightingAmount: 5_000m, depositAmount: 25_000m);

    private static BookingApplicationService BookingServiceFor(string role, FakeBookingRepository repository) =>
        new(repository, new FakeStadiumBookingSettingsRepository(), new MvpPricingPolicy(),
            FakePermissionGuard.ForRoles(role), new FakeAuditWriter(), new FakeNotificationService());

    private static BookingBlockApplicationService BlockServiceFor(string role, FakeBookingBlockRepository repository) =>
        new(repository, new FakeBookingRepository(), FakePermissionGuard.ForRoles(role), new FakeAuditWriter());

    private static PaymentApplicationService PaymentServiceFor(
        string role,
        FakePaymentRepository paymentRepository,
        FakeBookingRepository bookingRepository)
        => new(paymentRepository, bookingRepository, FakePermissionGuard.ForRoles(role),
            new FakePaymentProvider(), new FakeAuditWriter(), new FakeNotificationService());

    private static StadiumSettingsApplicationService SettingsServiceFor(
        string role, FakeStadiumBookingSettingsRepository repository) =>
        new(repository, FakePermissionGuard.ForRoles(role), new FakeAuditWriter());

    private static UpdateStadiumBookingSettingsRequest ValidUpdate() =>
        new(new TimeOnly(8, 0), new TimeOnly(23, 0), 2, 6, 30_000m, 15_000m, 5_000m,
            new TimeOnly(19, 0), 25_000m, 60, 24, true);

    // --- Lecteur ---------------------------------------------------------------

    [Fact]
    public async Task Lecteur_peut_consulter_les_reservations()
    {
        var result = await BookingServiceFor(DiamonoRoles.Lecteur, new FakeBookingRepository(NewBooking()))
            .GetBackOfficeBookingsAsync();

        Assert.Single(result);
    }

    [Fact]
    public async Task Lecteur_ne_peut_pas_approuver()
    {
        var booking = NewBooking();
        var repository = new FakeBookingRepository(booking);

        var error = await Assert.ThrowsAsync<PermissionDeniedException>(
            () => BookingServiceFor(DiamonoRoles.Lecteur, repository).ApproveBookingAsync(booking.Id));

        Assert.Equal(Permissions.BookingsApprove, error.Permission);
        Assert.Equal(BookingStatus.PendingApproval, booking.Status);
        Assert.Equal(0, repository.SaveChangesCallCount);
    }

    [Fact]
    public async Task Lecteur_ne_peut_pas_gerer_les_blocages()
    {
        var repository = new FakeBookingBlockRepository();

        await Assert.ThrowsAsync<PermissionDeniedException>(
            () => BlockServiceFor(DiamonoRoles.Lecteur, repository).CreateBookingBlockAsync(
                new CreateBookingBlockRequest(ResourceId, At(8), At(10), BookingBlockType.Maintenance, "")));

        Assert.Equal(0, repository.SaveChangesCallCount);
    }

    // --- Gestionnaire ----------------------------------------------------------

    [Fact]
    public async Task Gestionnaire_peut_approuver()
    {
        var booking = NewBooking();
        var repository = new FakeBookingRepository(booking);

        await BookingServiceFor(DiamonoRoles.Gestionnaire, repository).ApproveBookingAsync(booking.Id);

        Assert.Equal(BookingStatus.AwaitingPayment, booking.Status);
        Assert.Equal(1, repository.SaveChangesCallCount);
    }

    [Fact]
    public async Task Gestionnaire_ne_peut_pas_marquer_paye()
    {
        var booking = NewBooking();
        booking.Approve();
        var repository = new FakeBookingRepository(booking);

        var error = await Assert.ThrowsAsync<PermissionDeniedException>(
            () => PaymentServiceFor(DiamonoRoles.Gestionnaire, new FakePaymentRepository(), repository)
                .MarkCashPaymentAsPaidAsync(new MarkCashPaymentAsPaidRequest(booking.Id, PaymentMethod.Cash)));

        Assert.Equal(Permissions.PaymentsMarkPaid, error.Permission);
        Assert.Equal(BookingStatus.AwaitingPayment, booking.Status);
        Assert.Null(booking.PaidAt);
        Assert.Equal(0, repository.SaveChangesCallCount);
    }

    [Fact]
    public async Task Gestionnaire_peut_lire_les_parametres_mais_pas_les_modifier()
    {
        var repository = new FakeStadiumBookingSettingsRepository();
        var service = SettingsServiceFor(DiamonoRoles.Gestionnaire, repository);

        Assert.NotNull(await service.GetSettingsAsync());

        var error = await Assert.ThrowsAsync<PermissionDeniedException>(
            () => service.UpdateSettingsAsync(ValidUpdate()));

        Assert.Equal(Permissions.SettingsManage, error.Permission);
        Assert.Equal(0, repository.SaveChangesCallCount);
    }

    // --- Caissier --------------------------------------------------------------

    [Fact]
    public async Task Caissier_peut_marquer_paye()
    {
        var booking = NewBooking();
        booking.Approve();
        var repository = new FakeBookingRepository(booking);
        var paymentRepository = new FakePaymentRepository();

        await PaymentServiceFor(DiamonoRoles.Caissier, paymentRepository, repository)
            .MarkCashPaymentAsPaidAsync(new MarkCashPaymentAsPaidRequest(booking.Id, PaymentMethod.Cash));

        Assert.Equal(BookingStatus.Confirmed, booking.Status);
        Assert.NotNull(booking.PaidAt);
        Assert.Equal(1, paymentRepository.SaveChangesCallCount);
    }

    [Fact]
    public async Task Caissier_ne_peut_pas_approuver()
    {
        var booking = NewBooking();
        var repository = new FakeBookingRepository(booking);

        await Assert.ThrowsAsync<PermissionDeniedException>(
            () => BookingServiceFor(DiamonoRoles.Caissier, repository).ApproveBookingAsync(booking.Id));

        Assert.Equal(BookingStatus.PendingApproval, booking.Status);
    }

    [Fact]
    public async Task Caissier_ne_peut_ni_lire_ni_modifier_les_parametres()
    {
        var repository = new FakeStadiumBookingSettingsRepository();
        var service = SettingsServiceFor(DiamonoRoles.Caissier, repository);

        var readError = await Assert.ThrowsAsync<PermissionDeniedException>(() => service.GetSettingsAsync());
        var writeError = await Assert.ThrowsAsync<PermissionDeniedException>(
            () => service.UpdateSettingsAsync(ValidUpdate()));

        Assert.Equal(Permissions.SettingsView, readError.Permission);
        Assert.Equal(Permissions.SettingsManage, writeError.Permission);
        Assert.Equal(0, repository.SaveChangesCallCount);
    }

    // --- SuperAdmin ------------------------------------------------------------

    [Fact]
    public async Task SuperAdmin_peut_enchainer_approbation_et_encaissement()
    {
        var booking = NewBooking();
        var repository = new FakeBookingRepository(booking);
        var service = BookingServiceFor(DiamonoRoles.SuperAdmin, repository);

        await service.ApproveBookingAsync(booking.Id);
        await PaymentServiceFor(DiamonoRoles.SuperAdmin, new FakePaymentRepository(), repository)
            .MarkCashPaymentAsPaidAsync(new MarkCashPaymentAsPaidRequest(booking.Id, PaymentMethod.Cash));

        Assert.Equal(BookingStatus.Confirmed, booking.Status);
    }

    [Fact]
    public async Task SuperAdmin_peut_gerer_blocages_et_parametres()
    {
        var blockRepository = new FakeBookingBlockRepository();
        await BlockServiceFor(DiamonoRoles.SuperAdmin, blockRepository).CreateBookingBlockAsync(
            new CreateBookingBlockRequest(ResourceId, At(8), At(10), BookingBlockType.Maintenance, ""));

        var settingsRepository = new FakeStadiumBookingSettingsRepository();
        await SettingsServiceFor(DiamonoRoles.SuperAdmin, settingsRepository).UpdateSettingsAsync(ValidUpdate());

        Assert.Equal(1, blockRepository.SaveChangesCallCount);
        Assert.Equal(1, settingsRepository.SaveChangesCallCount);
    }

    // --- Anonyme ---------------------------------------------------------------

    [Fact]
    public async Task Anonyme_ne_peut_pas_lire_le_backoffice()
    {
        var service = new BookingApplicationService(
            new FakeBookingRepository(NewBooking()), new FakeStadiumBookingSettingsRepository(),
            new MvpPricingPolicy(), FakePermissionGuard.Anonymous(), new FakeAuditWriter(), new FakeNotificationService());

        var error = await Assert.ThrowsAsync<PermissionDeniedException>(() => service.GetBackOfficeBookingsAsync());

        Assert.Equal(Permissions.BookingsView, error.Permission);
    }

    [Fact]
    public async Task Le_parcours_public_reste_anonyme()
    {
        // Devis et creation de demande ne passent par aucune permission.
        var service = new BookingApplicationService(
            new FakeBookingRepository(), new FakeStadiumBookingSettingsRepository(),
            new MvpPricingPolicy(), FakePermissionGuard.Anonymous(), new FakeAuditWriter(), new FakeNotificationService());

        var quote = await service.QuoteAsync(At(16), At(18), CustomerCategory.Individual);
        var booking = await service.CreateAsync(new CreateBookingRequest(
            ResourceId, At(16), At(18), "Awa Diop", "+221 77 000 00 00",
            CustomerCategory.Individual, "Football"));

        Assert.Equal(50_000m, quote.RentalAmount);
        Assert.Equal(BookingStatus.PendingApproval, booking.Status);
    }
}
