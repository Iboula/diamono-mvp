using Diamono.Application.Abstractions;
using Diamono.Application.Audit;
using Diamono.Domain.Audit;
using Diamono.Domain.Payments;
using Diamono.Domain.Security;

namespace Diamono.Application.Payments;

public sealed class PaymentReceiptService(
    IPaymentRepository paymentRepository,
    IBookingRepository bookingRepository,
    IPermissionGuard permissionGuard,
    IAuditWriter auditWriter,
    IPaymentReceiptRenderer renderer) : IPaymentReceiptService
{
    public async Task<PaymentReceiptFile> GenerateReceiptAsync(
        Guid paymentId,
        CancellationToken cancellationToken = default)
    {
        await permissionGuard.EnsurePermissionAsync(Permissions.PaymentsMarkPaid, cancellationToken);

        var payment = await paymentRepository.GetByIdAsync(paymentId, cancellationToken)
            ?? throw new KeyNotFoundException("Paiement introuvable.");
        if (payment.Status != PaymentStatus.Paid)
            throw new InvalidOperationException("Un recu ne peut etre genere que pour un paiement paye.");

        var booking = await bookingRepository.GetByIdAsync(payment.BookingId, cancellationToken)
            ?? throw new KeyNotFoundException("Reservation introuvable.");

        var receiptReference = BuildReceiptReference(payment);
        var data = new PaymentReceiptData(
            receiptReference,
            payment.Reference,
            booking.Reference,
            payment.PaidAt ?? payment.CreatedAt,
            booking.CustomerName,
            booking.Phone,
            booking.StartsAt,
            booking.EndsAt,
            booking.ActivityType,
            payment.Method,
            payment.Amount,
            payment.Currency,
            "PAYÉ");

        var content = renderer.Render(data);
        await auditWriter.WriteAsync(new AuditWriteRequest(
            AuditActions.PaymentReceiptGenerated,
            "Payment",
            payment.Id.ToString(),
            "Recu de paiement genere.",
            Metadata: new
            {
                paymentId = payment.Id,
                bookingId = booking.Id,
                receiptReference
            }),
            cancellationToken);
        await paymentRepository.SaveChangesAsync(cancellationToken);

        return new PaymentReceiptFile(
            receiptReference,
            $"{receiptReference}.pdf",
            "application/pdf",
            content);
    }

    private static string BuildReceiptReference(Payment payment)
    {
        var year = (payment.PaidAt ?? payment.CreatedAt).Year;
        var suffix = payment.Reference.StartsWith($"PAY-{year}-", StringComparison.OrdinalIgnoreCase)
            ? payment.Reference[$"PAY-{year}-".Length..]
            : payment.Reference.Replace("PAY-", string.Empty, StringComparison.OrdinalIgnoreCase);
        return $"RCT-{year}-{suffix}";
    }
}
