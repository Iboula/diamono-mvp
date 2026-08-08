using Diamono.Application.Bookings;

namespace Diamono.Application.Tests;

internal sealed class FakeBookingRequestReceiptRenderer : IBookingRequestReceiptRenderer
{
    public List<BookingRequestReceiptData> Receipts { get; } = [];

    public byte[] Render(BookingRequestReceiptData receipt)
    {
        Receipts.Add(receipt);
        return System.Text.Encoding.UTF8.GetBytes(
            $"PDF {receipt.BookingReference} {receipt.TotalAmount:N0} ne constitue pas une reservation confirmee");
    }
}
