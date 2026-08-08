using Diamono.Domain.Bookings;

namespace Diamono.Application.Bookings;

public sealed record CreateBookingBlockRequest(
    Guid ResourceId,
    DateTimeOffset StartsAt,
    DateTimeOffset EndsAt,
    BookingBlockType Type,
    string Description);
