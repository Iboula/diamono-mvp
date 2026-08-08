using Diamono.Application.Abstractions;
using Diamono.Domain.Bookings;
using Diamono.Domain.Pricing;

namespace Diamono.Application.Bookings;

public sealed class BookingApplicationService(IBookingRepository repository, MvpPricingPolicy pricing)
{
    public PriceQuote Quote(DateTimeOffset startsAt, DateTimeOffset endsAt, CustomerCategory category)
        => pricing.Quote(startsAt, endsAt, category);

    public async Task<Booking> CreateAsync(CreateBookingRequest request, CancellationToken cancellationToken = default)
    {
        if (await repository.HasConflictAsync(request.ResourceId, request.StartsAt, request.EndsAt, cancellationToken))
            throw new InvalidOperationException("Ce créneau n'est plus disponible.");

        var quote = pricing.Quote(request.StartsAt, request.EndsAt, request.CustomerCategory);
        var booking = new Booking(request.ResourceId, request.StartsAt, request.EndsAt,
            request.CustomerName, request.Phone, request.CustomerCategory, request.ActivityType,
            quote.RentalAmount, quote.LightingAmount, quote.DepositAmount);

        await repository.AddAsync(booking, cancellationToken);
        await repository.SaveChangesAsync(cancellationToken);
        return booking;
    }
}
