using Diamono.Application.Abstractions;
using Diamono.Application.Audit;
using Diamono.Application.Notifications;
using Diamono.Domain.Audit;
using Diamono.Domain.Bookings;
using Diamono.Domain.Notifications;
using Diamono.Domain.Payments;
using Diamono.Domain.Security;

namespace Diamono.Application.Payments;

public sealed class PaymentApplicationService(
    IPaymentRepository paymentRepository,
    IBookingRepository bookingRepository,
    IPermissionGuard permissionGuard,
    IPaymentProvider paymentProvider,
    IAuditWriter auditWriter,
    INotificationService notificationService)
{
    public async Task<Payment> CreatePaymentAsync(
        CreatePaymentRequest request,
        CancellationToken cancellationToken = default)
    {
        await permissionGuard.EnsurePermissionAsync(Permissions.PaymentsMarkPaid, cancellationToken);
        var booking = await GetRequiredBookingAsync(request.BookingId, cancellationToken);
        EnsureBookingAwaitingPayment(booking);
        await EnsureNoPaidPaymentAsync(booking.Id, cancellationToken);

        var payment = new Payment(booking.Id, booking.TotalAmount, request.Method);
        await paymentRepository.AddAsync(payment, cancellationToken);
        await AuditPaymentAsync(
            AuditActions.PaymentCreated,
            payment,
            booking,
            "Paiement cree.",
            cancellationToken: cancellationToken);
        await paymentRepository.SaveChangesAsync(cancellationToken);
        return payment;
    }

    public async Task<Payment> MarkCashPaymentAsPaidAsync(
        MarkCashPaymentAsPaidRequest request,
        CancellationToken cancellationToken = default)
    {
        await permissionGuard.EnsurePermissionAsync(Permissions.PaymentsMarkPaid, cancellationToken);
        var booking = await GetRequiredBookingAsync(request.BookingId, cancellationToken);
        EnsureBookingAwaitingPayment(booking);
        await EnsureNoPaidPaymentAsync(booking.Id, cancellationToken);

        var payment = new Payment(booking.Id, booking.TotalAmount, request.Method);
        await paymentRepository.AddAsync(payment, cancellationToken);
        await AuditPaymentAsync(
            AuditActions.PaymentCreated,
            payment,
            booking,
            "Paiement cree.",
            cancellationToken: cancellationToken);

        var providerResult = await paymentProvider.CaptureManualPaymentAsync(payment, cancellationToken);
        if (!providerResult.Success)
        {
            payment.MarkFailed(providerResult.ProviderTransactionId);
            await AuditPaymentAsync(
                AuditActions.PaymentFailed,
                payment,
                booking,
                "Paiement en echec.",
                new { provider = payment.Provider, error = providerResult.Error },
                cancellationToken);
            await paymentRepository.SaveChangesAsync(cancellationToken);
            return payment;
        }

        payment.MarkPaid(providerResult.ProviderTransactionId);
        var oldStatus = booking.Status;
        booking.ConfirmPayment();

        await AuditPaymentAsync(
            AuditActions.PaymentPaid,
            payment,
            booking,
            "Paiement enregistre.",
            new { provider = payment.Provider, method = payment.Method.ToString() },
            cancellationToken);
        await AuditBookingStatusAsync(booking, oldStatus, cancellationToken);
        await NotifyBookingConfirmedAsync(booking, payment, cancellationToken);
        await paymentRepository.SaveChangesAsync(cancellationToken);
        return payment;
    }

    public async Task<Payment> MarkPaymentAsFailedAsync(
        MarkPaymentAsFailedRequest request,
        CancellationToken cancellationToken = default)
    {
        await permissionGuard.EnsurePermissionAsync(Permissions.PaymentsMarkPaid, cancellationToken);
        var payment = await paymentRepository.GetByIdAsync(request.PaymentId, cancellationToken)
            ?? throw new KeyNotFoundException("Paiement introuvable.");
        var booking = await GetRequiredBookingAsync(payment.BookingId, cancellationToken);

        payment.MarkFailed(request.ProviderTransactionId);
        await AuditPaymentAsync(
            AuditActions.PaymentFailed,
            payment,
            booking,
            "Paiement en echec.",
            new { provider = payment.Provider },
            cancellationToken);
        await paymentRepository.SaveChangesAsync(cancellationToken);
        return payment;
    }

    public async Task<IReadOnlyList<Payment>> GetBookingPaymentsAsync(
        Guid bookingId,
        CancellationToken cancellationToken = default)
    {
        await permissionGuard.EnsurePermissionAsync(Permissions.PaymentsMarkPaid, cancellationToken);
        return await paymentRepository.GetByBookingAsync(bookingId, cancellationToken);
    }

    public async Task<IReadOnlyList<PaymentReadModel>> GetBackOfficePaymentsAsync(
        CancellationToken cancellationToken = default)
    {
        await permissionGuard.EnsurePermissionAsync(Permissions.PaymentsMarkPaid, cancellationToken);
        var payments = await paymentRepository.GetBackOfficeAsync(cancellationToken);
        var result = new List<PaymentReadModel>(payments.Count);

        foreach (var payment in payments)
        {
            var booking = await GetRequiredBookingAsync(payment.BookingId, cancellationToken);
            result.Add(new PaymentReadModel(
                payment.Id,
                payment.BookingId,
                payment.Reference,
                booking.Reference,
                booking.CustomerName,
                payment.Amount,
                payment.Currency,
                payment.Method,
                payment.Status,
                payment.CreatedAt,
                payment.PaidAt,
                payment.Provider));
        }

        return result;
    }

    private async Task<Booking> GetRequiredBookingAsync(Guid bookingId, CancellationToken cancellationToken)
        => await bookingRepository.GetByIdAsync(bookingId, cancellationToken)
            ?? throw new KeyNotFoundException("Reservation introuvable.");

    private static void EnsureBookingAwaitingPayment(Booking booking)
    {
        if (booking.Status != BookingStatus.AwaitingPayment)
            throw new InvalidOperationException("Seule une reservation en attente de paiement peut recevoir un paiement.");
    }

    private async Task EnsureNoPaidPaymentAsync(Guid bookingId, CancellationToken cancellationToken)
    {
        if (await paymentRepository.HasPaidPaymentForBookingAsync(bookingId, cancellationToken))
            throw new InvalidOperationException("Cette reservation possede deja un paiement paye.");
    }

    private async Task AuditPaymentAsync(
        string action,
        Payment payment,
        Booking booking,
        string description,
        object? metadata = null,
        CancellationToken cancellationToken = default)
        => await auditWriter.WriteAsync(new AuditWriteRequest(
            action,
            "Payment",
            payment.Id.ToString(),
            description,
            NewValues: new
            {
                status = payment.Status.ToString(),
                amount = payment.Amount,
                currency = payment.Currency,
                method = payment.Method.ToString()
            },
            Metadata: metadata is null
                ? new { paymentReference = payment.Reference, bookingReference = booking.Reference }
                : new { paymentReference = payment.Reference, bookingReference = booking.Reference, context = metadata }),
            cancellationToken);

    private async Task AuditBookingStatusAsync(
        Booking booking,
        BookingStatus oldStatus,
        CancellationToken cancellationToken)
        => await auditWriter.WriteAsync(new AuditWriteRequest(
            AuditActions.BookingMarkedPaid,
            "Booking",
            booking.Id.ToString(),
            "Reservation marquee payee.",
            OldValues: new { status = oldStatus.ToString() },
            NewValues: new { status = booking.Status.ToString() },
            Metadata: new { reference = booking.Reference }),
            cancellationToken);

    private async Task NotifyBookingConfirmedAsync(
        Booking booking,
        Payment payment,
        CancellationToken cancellationToken)
        => await notificationService.NotifyAsync(new NotificationRequest(
            booking.Id,
            booking.Phone,
            NotificationChannel.Sms,
            NotificationTemplate.BookingMarkedPaid,
            "Reservation confirmee",
            $"Stade Diamono : paiement recu. Votre reservation {booking.Reference} est confirmee.",
            new
            {
                reference = booking.Reference,
                status = booking.Status.ToString(),
                amount = payment.Amount,
                currency = payment.Currency,
                paymentReference = payment.Reference
            }),
            cancellationToken);
}
