namespace Diamono.Application.Bookings;

public sealed record BookingRequestReceiptFile(
    string ReceiptReference,
    string FileName,
    string ContentType,
    byte[] Content);
