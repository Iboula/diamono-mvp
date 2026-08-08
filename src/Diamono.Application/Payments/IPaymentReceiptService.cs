namespace Diamono.Application.Payments;

public interface IPaymentReceiptService
{
    Task<PaymentReceiptFile> GenerateReceiptAsync(Guid paymentId, CancellationToken cancellationToken = default);
}
