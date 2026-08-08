namespace Diamono.Application.Bookings;

public interface IBookingRequestReceiptService
{
    Task<BookingRequestReceiptFile> GeneratePublicReceiptAsync(string publicAccessToken, CancellationToken cancellationToken = default);
    Task<BookingRequestReceiptFile> GenerateBackOfficeReceiptAsync(Guid bookingId, CancellationToken cancellationToken = default);
}
