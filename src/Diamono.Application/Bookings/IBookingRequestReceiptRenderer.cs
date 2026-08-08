namespace Diamono.Application.Bookings;

public interface IBookingRequestReceiptRenderer
{
    byte[] Render(BookingRequestReceiptData receipt);
}
