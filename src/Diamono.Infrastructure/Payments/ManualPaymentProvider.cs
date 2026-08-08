using Diamono.Application.Payments;
using Diamono.Domain.Payments;

namespace Diamono.Infrastructure.Payments;

public sealed class ManualPaymentProvider : IPaymentProvider
{
    public Task<PaymentProviderResult> CaptureManualPaymentAsync(
        Payment payment,
        CancellationToken cancellationToken = default)
        => Task.FromResult(PaymentProviderResult.Paid($"MANUAL-{payment.Reference}"));
}
