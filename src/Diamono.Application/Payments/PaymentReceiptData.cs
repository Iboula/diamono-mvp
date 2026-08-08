using Diamono.Domain.Payments;

namespace Diamono.Application.Payments;

public sealed record PaymentReceiptData(
    string ReceiptReference,
    string PaymentReference,
    string BookingReference,
    DateTimeOffset PaymentDate,
    string CustomerName,
    string Phone,
    DateTimeOffset BookingStartsAt,
    DateTimeOffset BookingEndsAt,
    string ActivityType,
    PaymentMethod Method,
    decimal Amount,
    string Currency,
    string Status);
