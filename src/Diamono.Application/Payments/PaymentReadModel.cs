using Diamono.Domain.Payments;

namespace Diamono.Application.Payments;

public sealed record PaymentReadModel(
    Guid Id,
    Guid BookingId,
    string PaymentReference,
    string BookingReference,
    string CustomerName,
    decimal Amount,
    string Currency,
    PaymentMethod Method,
    PaymentStatus Status,
    DateTimeOffset CreatedAt,
    DateTimeOffset? PaidAt,
    string Provider);
