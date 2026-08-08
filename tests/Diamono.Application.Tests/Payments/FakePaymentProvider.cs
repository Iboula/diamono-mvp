using Diamono.Application.Payments;
using Diamono.Domain.Payments;

namespace Diamono.Application.Tests;

internal sealed class FakePaymentProvider(PaymentProviderResult? result = null) : IPaymentProvider
{
    private readonly PaymentProviderResult result = result ?? PaymentProviderResult.Paid("TEST-PAYMENT");

    public Task<PaymentProviderResult> CaptureManualPaymentAsync(
        Payment payment,
        CancellationToken cancellationToken = default)
        => Task.FromResult(result);
}
