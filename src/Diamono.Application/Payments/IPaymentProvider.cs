using Diamono.Domain.Payments;

namespace Diamono.Application.Payments;

public interface IPaymentProvider
{
    Task<PaymentProviderResult> CaptureManualPaymentAsync(
        Payment payment,
        CancellationToken cancellationToken = default);
}
