using Diamono.Application.Abstractions;
using Diamono.Domain.Bookings;
using Diamono.Domain.Pricing;

namespace Diamono.Application.Bookings;

public sealed class BookingApplicationService(
    IBookingRepository repository,
    IStadiumBookingSettingsRepository settingsRepository,
    MvpPricingPolicy pricing)
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
        => await repository.GetBackOfficeAsync(cancellationToken);

    public async Task ApproveBookingAsync(Guid bookingId, CancellationToken cancellationToken = default)
    {
        var booking = await GetRequiredBookingAsync(bookingId, cancellationToken);
        booking.Approve();
        await repository.SaveChangesAsync(cancellationToken);
    }

    public async Task RejectBookingAsync(Guid bookingId, string reason, CancellationToken cancellationToken = default)
    {
        var booking = await GetRequiredBookingAsync(bookingId, cancellationToken);
        booking.Reject(reason);
        await repository.SaveChangesAsync(cancellationToken);
    }

    public async Task MarkBookingAsPaidAsync(Guid bookingId, CancellationToken cancellationToken = default)
    {
        var booking = await GetRequiredBookingAsync(bookingId, cancellationToken);
        booking.ConfirmPayment();
        await repository.SaveChangesAsync(cancellationToken);
    }

    public async Task CancelBookingAsync(Guid bookingId, string reason, CancellationToken cancellationToken = default)
    {
        var booking = await GetRequiredBookingAsync(bookingId, cancellationToken);
        booking.Cancel(reason);
        await repository.SaveChangesAsync(cancellationToken);
    }

    private async Task<Booking> GetRequiredBookingAsync(Guid bookingId, CancellationToken cancellationToken)
        => await repository.GetByIdAsync(bookingId, cancellationToken)
            ?? throw new KeyNotFoundException("Reservation introuvable.");
}
