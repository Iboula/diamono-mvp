using Diamono.Application.Abstractions;
using Diamono.Domain.Bookings;
using Diamono.Domain.Pricing;
using Diamono.Domain.Security;

namespace Diamono.Application.Bookings;

public sealed class BookingApplicationService(
    IBookingRepository repository,
    IStadiumBookingSettingsRepository settingsRepository,
    MvpPricingPolicy pricing,
    IPermissionGuard permissionGuard)
{
    public async Task<PriceQuote> QuoteAsync(
        DateTimeOffset startsAt,
        DateTimeOffset endsAt,
        CustomerCategory category,
        CancellationToken cancellationToken = default)
    {
        var settings = await settingsRepository.GetAsync(cancellationToken);
        return pricing.Quote(settings, startsAt, endsAt, category);
    }

    public async Task<Booking> CreateAsync(CreateBookingRequest request, CancellationToken cancellationToken = default)
    {
        var settings = await settingsRepository.GetAsync(cancellationToken);
        if (request.StartsAt > DateTimeOffset.UtcNow.AddDays(settings.MaximumAdvanceBookingDays))
            throw new InvalidOperationException("La date demandee depasse le delai de reservation anticipee.");

        if (await repository.HasConflictAsync(request.ResourceId, request.StartsAt, request.EndsAt, cancellationToken))
            throw new InvalidOperationException("Ce creneau n'est plus disponible.");

        var quote = pricing.Quote(settings, request.StartsAt, request.EndsAt, request.CustomerCategory);
        var booking = new Booking(request.ResourceId, request.StartsAt, request.EndsAt,
            request.CustomerName, request.Phone, request.CustomerCategory, request.ActivityType,
            quote.RentalAmount, quote.LightingAmount, quote.DepositAmount);

        await repository.AddAsync(booking, cancellationToken);
        await repository.SaveChangesAsync(cancellationToken);
        return booking;
    }

    public async Task<IReadOnlyList<Booking>> GetBackOfficeBookingsAsync(CancellationToken cancellationToken = default)
    {
        await permissionGuard.EnsurePermissionAsync(Permissions.BookingsView, cancellationToken);
        return await repository.GetBackOfficeAsync(cancellationToken);
    }

    public async Task ApproveBookingAsync(Guid bookingId, CancellationToken cancellationToken = default)
    {
        await permissionGuard.EnsurePermissionAsync(Permissions.BookingsApprove, cancellationToken);
        var booking = await GetRequiredBookingAsync(bookingId, cancellationToken);
        booking.Approve();
        await repository.SaveChangesAsync(cancellationToken);
    }

    public async Task RejectBookingAsync(Guid bookingId, string reason, CancellationToken cancellationToken = default)
    {
        await permissionGuard.EnsurePermissionAsync(Permissions.BookingsReject, cancellationToken);
        var booking = await GetRequiredBookingAsync(bookingId, cancellationToken);
        booking.Reject(reason);
        await repository.SaveChangesAsync(cancellationToken);
    }

    public async Task MarkBookingAsPaidAsync(Guid bookingId, CancellationToken cancellationToken = default)
    {
        await permissionGuard.EnsurePermissionAsync(Permissions.PaymentsMarkPaid, cancellationToken);
        var booking = await GetRequiredBookingAsync(bookingId, cancellationToken);
        booking.ConfirmPayment();
        await repository.SaveChangesAsync(cancellationToken);
    }

    // Hypothese assumee : l'annulation d'une reservation par le backoffice est une
    // decision de meme nature que le refus, donc rattachee a Bookings.Reject plutot
    // qu'a une dixieme permission non prevue par la matrice DIA-016B.
    public async Task CancelBookingAsync(Guid bookingId, string reason, CancellationToken cancellationToken = default)
    {
        await permissionGuard.EnsurePermissionAsync(Permissions.BookingsReject, cancellationToken);
        var booking = await GetRequiredBookingAsync(bookingId, cancellationToken);
        booking.Cancel(reason);
        await repository.SaveChangesAsync(cancellationToken);
    }

    private async Task<Booking> GetRequiredBookingAsync(Guid bookingId, CancellationToken cancellationToken)
        => await repository.GetByIdAsync(bookingId, cancellationToken)
            ?? throw new KeyNotFoundException("Reservation introuvable.");
}
