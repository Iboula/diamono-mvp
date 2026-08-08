using Diamono.Application.Abstractions;
using Diamono.Application.Audit;
using Diamono.Domain.Audit;
using Diamono.Domain.Bookings;
using Diamono.Domain.Security;

namespace Diamono.Application.Bookings;

public sealed class BookingRequestReceiptService(
    IBookingRepository bookingRepository,
    IPermissionGuard permissionGuard,
    IAuditWriter auditWriter,
    IBookingRequestReceiptRenderer renderer) : IBookingRequestReceiptService
{
    private const string ContentType = "application/pdf";
    private const string ResourceName = "Terrain principal";

    public async Task<BookingRequestReceiptFile> GeneratePublicReceiptAsync(
        string publicAccessToken,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(publicAccessToken))
            throw new KeyNotFoundException("Recu provisoire introuvable.");

        var booking = await bookingRepository.GetByPublicAccessTokenAsync(publicAccessToken, cancellationToken)
            ?? throw new KeyNotFoundException("Recu provisoire introuvable.");

        return await GenerateReceiptAsync(booking, "PublicToken", cancellationToken);
    }

    public async Task<BookingRequestReceiptFile> GenerateBackOfficeReceiptAsync(
        Guid bookingId,
        CancellationToken cancellationToken = default)
    {
        await permissionGuard.EnsurePermissionAsync(Permissions.BookingsView, cancellationToken);

        var booking = await bookingRepository.GetByIdAsync(bookingId, cancellationToken)
            ?? throw new KeyNotFoundException("Reservation introuvable.");

        return await GenerateReceiptAsync(booking, "BackOffice", cancellationToken);
    }

    private async Task<BookingRequestReceiptFile> GenerateReceiptAsync(
        Booking booking,
        string accessMode,
        CancellationToken cancellationToken)
    {
        var receiptReference = BuildReceiptReference(booking);
        var data = new BookingRequestReceiptData(
            receiptReference,
            booking.Reference,
            booking.CreatedAt,
            booking.CustomerName,
            booking.Phone,
            booking.CustomerCategory,
            ResourceName,
            booking.StartsAt,
            booking.EndsAt,
            booking.ActivityType,
            booking.RentalAmount,
            booking.LightingAmount,
            booking.DepositAmount,
            booking.TotalAmount,
            StatusLabel(booking.Status));

        var content = renderer.Render(data);
        await auditWriter.WriteAsync(new AuditWriteRequest(
            AuditActions.BookingRequestReceiptGenerated,
            "Booking",
            booking.Id.ToString(),
            "Recu provisoire de demande de reservation genere.",
            Metadata: new
            {
                bookingId = booking.Id,
                bookingReference = booking.Reference,
                receiptReference,
                accessMode
            }),
            cancellationToken);
        await bookingRepository.SaveChangesAsync(cancellationToken);

        return new BookingRequestReceiptFile(
            receiptReference,
            $"{receiptReference}.pdf",
            ContentType,
            content);
    }

    private static string BuildReceiptReference(Booking booking)
    {
        var year = booking.CreatedAt.Year;
        var suffix = booking.Reference.StartsWith($"DIA-{year}-", StringComparison.OrdinalIgnoreCase)
            ? booking.Reference[$"DIA-{year}-".Length..]
            : booking.Reference.Replace("DIA-", string.Empty, StringComparison.OrdinalIgnoreCase);
        return $"RPD-{year}-{suffix}";
    }

    private static string StatusLabel(BookingStatus status)
        => status switch
        {
            BookingStatus.PendingApproval => "EN ATTENTE DE VALIDATION",
            BookingStatus.AwaitingPayment => "DEMANDE VALIDEE - PAIEMENT ATTENDU",
            BookingStatus.Confirmed => "STATUT ACTUEL : CONFIRMEE",
            BookingStatus.Rejected => "STATUT ACTUEL : REFUSEE",
            BookingStatus.Cancelled => "STATUT ACTUEL : ANNULEE",
            _ => $"STATUT ACTUEL : {status.ToString().ToUpperInvariant()}"
        };
}
