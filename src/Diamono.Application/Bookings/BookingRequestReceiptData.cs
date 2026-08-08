using Diamono.Domain.Bookings;

namespace Diamono.Application.Bookings;

public sealed record BookingRequestReceiptData(
    string ReceiptReference,
    string BookingReference,
    DateTimeOffset CreatedAt,
    string CustomerName,
    string Phone,
    CustomerCategory CustomerCategory,
    string ResourceName,
    DateTimeOffset StartsAt,
    DateTimeOffset EndsAt,
    string ActivityType,
    decimal RentalAmount,
    decimal LightingAmount,
    decimal DepositAmount,
    decimal TotalAmount,
    string StatusLabel);
