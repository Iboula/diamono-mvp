using Diamono.Domain.Bookings;
using Diamono.Domain.Settings;

namespace Diamono.Application.Reporting;

public sealed record ReportingData(
    IReadOnlyList<ReportingBooking> Bookings,
    IReadOnlyList<ReportingPayment> Payments,
    IReadOnlyList<ReportingBlock> Blocks,
    StadiumBookingSettings Settings);

public sealed record ReportingBooking(
    Guid Id,
    DateTimeOffset StartsAt,
    DateTimeOffset EndsAt,
    CustomerCategory CustomerCategory,
    string ActivityType,
    decimal TotalAmount,
    BookingStatus Status,
    DateTimeOffset CreatedAt);

public sealed record ReportingBlock(
    Guid Id,
    DateTimeOffset StartsAt,
    DateTimeOffset EndsAt);

public sealed record ReportingPayment(
    Guid Id,
    Guid BookingId,
    decimal Amount,
    DateTimeOffset? PaidAt);
