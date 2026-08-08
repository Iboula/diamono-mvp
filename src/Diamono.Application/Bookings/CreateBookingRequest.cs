using Diamono.Domain.Bookings;

namespace Diamono.Application.Bookings;

public sealed record CreateBookingRequest(
    Guid ResourceId,
    DateTimeOffset StartsAt,
    DateTimeOffset EndsAt,
    string CustomerName,
    string Phone,
    CustomerCategory CustomerCategory,
    string ActivityType);
