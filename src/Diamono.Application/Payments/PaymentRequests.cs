using Diamono.Domain.Payments;

namespace Diamono.Application.Payments;

public sealed record CreatePaymentRequest(Guid BookingId, PaymentMethod Method);

public sealed record MarkCashPaymentAsPaidRequest(Guid BookingId, PaymentMethod Method);

public sealed record MarkPaymentAsFailedRequest(Guid PaymentId, string? ProviderTransactionId = null);
