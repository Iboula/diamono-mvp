namespace Diamono.Application.Payments;

public interface IPaymentReceiptRenderer
{
    byte[] Render(PaymentReceiptData receipt);
}
