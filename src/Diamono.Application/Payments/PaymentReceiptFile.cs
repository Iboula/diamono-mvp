namespace Diamono.Application.Payments;

public sealed record PaymentReceiptFile(
    string ReceiptReference,
    string FileName,
    string ContentType,
    byte[] Content);
