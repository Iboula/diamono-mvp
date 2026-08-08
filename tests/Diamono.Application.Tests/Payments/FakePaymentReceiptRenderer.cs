using System.Text;
using Diamono.Application.Payments;

namespace Diamono.Application.Tests;

internal sealed class FakePaymentReceiptRenderer : IPaymentReceiptRenderer
{
    public List<PaymentReceiptData> Receipts { get; } = [];

    public byte[] Render(PaymentReceiptData receipt)
    {
        Receipts.Add(receipt);
        return Encoding.UTF8.GetBytes(
            $"PDF {receipt.ReceiptReference} {receipt.PaymentReference} {receipt.BookingReference} {receipt.Amount:N0} {receipt.Currency}");
    }
}
