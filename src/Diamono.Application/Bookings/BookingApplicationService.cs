using Diamono.Application.Abstractions;
using Diamono.Application.Audit;
using Diamono.Application.Notifications;
using Diamono.Domain.Audit;
using Diamono.Domain.Bookings;
using Diamono.Domain.Notifications;
using Diamono.Domain.Pricing;
using Diamono.Domain.Security;
using Diamono.Domain.Settings;

namespace Diamono.Application.Bookings;

public sealed class BookingApplicationService(
    IBookingRepository repository,
    IStadiumBookingSettingsRepository settingsRepository,
    MvpPricingPolicy pricing,
    IPermissionGuard permissionGuard,
    IAuditWriter auditWriter,
    INotificationService notificationService)
{
    public async Task<PriceQuote> QuoteAsync(
        DateTimeOffset startsAt,
        DateTimeOffset endsAt,
        CustomerCategory category,
        CancellationToken cancellationToken = default)
    {
        var settings = await settingsRepository.GetAsync(cancellationToken);
        return pricing.Quote(settings, startsAt, endsAt, category);
    }

    public async Task<Booking> CreateAsync(CreateBookingRequest request, CancellationToken cancellationToken = default)
    {
        var settings = await settingsRepository.GetAsync(cancellationToken);
        if (request.StartsAt > DateTimeOffset.UtcNow.AddDays(settings.MaximumAdvanceBookingDays))
            throw new InvalidOperationException("La date demandee depasse le delai de reservation anticipee.");

        if (await repository.HasConflictAsync(request.ResourceId, request.StartsAt, request.EndsAt, cancellationToken))
            throw new InvalidOperationException("Ce creneau n'est plus disponible.");

        var quote = pricing.Quote(settings, request.StartsAt, request.EndsAt, request.CustomerCategory);
        var booking = new Booking(request.ResourceId, request.StartsAt, request.EndsAt,
            request.CustomerName, request.Phone, request.CustomerCategory, request.ActivityType,
            quote.RentalAmount, quote.LightingAmount, quote.DepositAmount);

        await repository.AddAsync(booking, cancellationToken);
        await NotifyBookingAsync(booking, NotificationTemplate.BookingCreated, settings, cancellationToken: cancellationToken);
        await repository.SaveChangesAsync(cancellationToken);
        return booking;
    }

    public async Task<IReadOnlyList<Booking>> GetBackOfficeBookingsAsync(CancellationToken cancellationToken = default)
    {
        await permissionGuard.EnsurePermissionAsync(Permissions.BookingsView, cancellationToken);
        return await repository.GetBackOfficeAsync(cancellationToken);
    }

    public async Task ApproveBookingAsync(Guid bookingId, CancellationToken cancellationToken = default)
    {
        await permissionGuard.EnsurePermissionAsync(Permissions.BookingsApprove, cancellationToken);
        var booking = await GetRequiredBookingAsync(bookingId, cancellationToken);
        var settings = await settingsRepository.GetAsync(cancellationToken);
        var oldStatus = booking.Status;
        booking.Approve();
        await AuditBookingStatusAsync(
            AuditActions.BookingApproved,
            booking,
            oldStatus,
            "Reservation approuvee.",
            cancellationToken: cancellationToken);
        await NotifyBookingAsync(booking, NotificationTemplate.BookingApproved, settings, cancellationToken: cancellationToken);
        await repository.SaveChangesAsync(cancellationToken);
    }

    public async Task RejectBookingAsync(Guid bookingId, string reason, CancellationToken cancellationToken = default)
    {
        await permissionGuard.EnsurePermissionAsync(Permissions.BookingsReject, cancellationToken);
        var booking = await GetRequiredBookingAsync(bookingId, cancellationToken);
        var oldStatus = booking.Status;
        booking.Reject(reason);
        await AuditBookingStatusAsync(
            AuditActions.BookingRejected,
            booking,
            oldStatus,
            "Reservation rejetee.",
            new { reason = booking.RejectionReason },
            cancellationToken);
        await NotifyBookingAsync(
            booking,
            NotificationTemplate.BookingRejected,
            reason: booking.RejectionReason,
            cancellationToken: cancellationToken);
        await repository.SaveChangesAsync(cancellationToken);
    }

    public async Task MarkBookingAsPaidAsync(Guid bookingId, CancellationToken cancellationToken = default)
    {
        await permissionGuard.EnsurePermissionAsync(Permissions.PaymentsMarkPaid, cancellationToken);
        throw new InvalidOperationException("Le paiement doit etre enregistre via PaymentApplicationService.");
    }

    // Hypothese assumee : l'annulation d'une reservation par le backoffice est une
    // decision de meme nature que le refus, donc rattachee a Bookings.Reject plutot
    // qu'a une dixieme permission non prevue par la matrice DIA-016B.
    public async Task CancelBookingAsync(Guid bookingId, string reason, CancellationToken cancellationToken = default)
    {
        await permissionGuard.EnsurePermissionAsync(Permissions.BookingsReject, cancellationToken);
        var booking = await GetRequiredBookingAsync(bookingId, cancellationToken);
        var oldStatus = booking.Status;
        booking.Cancel(reason);
        await AuditBookingStatusAsync(
            AuditActions.BookingCancelled,
            booking,
            oldStatus,
            "Reservation annulee.",
            new { reason = booking.CancellationReason },
            cancellationToken);
        await NotifyBookingAsync(
            booking,
            NotificationTemplate.BookingCancelled,
            reason: booking.CancellationReason,
            cancellationToken: cancellationToken);
        await repository.SaveChangesAsync(cancellationToken);
    }

    private async Task<Booking> GetRequiredBookingAsync(Guid bookingId, CancellationToken cancellationToken)
        => await repository.GetByIdAsync(bookingId, cancellationToken)
            ?? throw new KeyNotFoundException("Reservation introuvable.");

    private async Task AuditBookingStatusAsync(
        string action,
        Booking booking,
        BookingStatus oldStatus,
        string description,
        object? metadata = null,
        CancellationToken cancellationToken = default)
        => await auditWriter.WriteAsync(new AuditWriteRequest(
            action,
            "Booking",
            booking.Id.ToString(),
            description,
            OldValues: new { status = oldStatus.ToString() },
            NewValues: new { status = booking.Status.ToString() },
            Metadata: metadata is null
                ? new { reference = booking.Reference }
                : new { reference = booking.Reference, context = metadata }),
            cancellationToken);

    private async Task NotifyBookingAsync(
        Booking booking,
        NotificationTemplate template,
        StadiumBookingSettings? settings = null,
        string? reason = null,
        CancellationToken cancellationToken = default)
    {
        var content = BuildNotificationContent(booking, template, settings, reason);
        await notificationService.NotifyAsync(new NotificationRequest(
            booking.Id,
            booking.Phone,
            NotificationChannel.Sms,
            template,
            content.Subject,
            content.Body,
            content.Metadata),
            cancellationToken);
    }

    private static NotificationContent BuildNotificationContent(
        Booking booking,
        NotificationTemplate template,
        StadiumBookingSettings? settings,
        string? reason)
        => template switch
        {
            NotificationTemplate.BookingCreated => new(
                "Demande recue",
                $"Stade Diamono : demande {booking.Reference} recue. Nous vous informerons apres validation.",
                BaseMetadata(booking)),
            NotificationTemplate.BookingApproved => new(
                "Demande approuvee",
                $"Stade Diamono : votre demande {booking.Reference} est approuvee. Paiement requis sous {PaymentDeadlineHours(settings)}h.",
                BaseMetadata(booking, PaymentDeadlineHours(settings))),
            NotificationTemplate.BookingRejected => new(
                "Demande refusee",
                $"Stade Diamono : demande {booking.Reference} refusee. Motif : {reason}.",
                BaseMetadata(booking, rejectionReason: reason)),
            NotificationTemplate.BookingMarkedPaid => new(
                "Reservation confirmee",
                $"Stade Diamono : paiement recu. Votre reservation {booking.Reference} est confirmee.",
                BaseMetadata(booking)),
            NotificationTemplate.BookingCancelled => new(
                "Reservation annulee",
                $"Stade Diamono : reservation {booking.Reference} annulee. Motif : {reason}.",
                BaseMetadata(booking, cancellationReason: reason)),
            NotificationTemplate.BookingPaymentReminder => new(
                "Rappel de paiement",
                $"Stade Diamono : rappel paiement {booking.Reference}. Paiement requis sous {PaymentDeadlineHours(settings)}h.",
                BaseMetadata(booking, PaymentDeadlineHours(settings))),
            NotificationTemplate.BookingUpcomingReminder => new(
                "Rappel de reservation",
                $"Stade Diamono : rappel reservation {booking.Reference} le {booking.StartsAt:dd/MM/yyyy} a {booking.StartsAt:HH:mm}.",
                BaseMetadata(booking)),
            _ => throw new ArgumentOutOfRangeException(nameof(template), template, "Template de notification inconnu.")
        };

    private static int PaymentDeadlineHours(StadiumBookingSettings? settings)
        => settings?.PaymentDeadlineHours
            ?? throw new InvalidOperationException("Le delai de paiement doit venir du parametrage du stade.");

    private static object BaseMetadata(
        Booking booking,
        int? paymentDeadlineHours = null,
        string? rejectionReason = null,
        string? cancellationReason = null)
        => new
        {
            reference = booking.Reference,
            status = booking.Status.ToString(),
            startsAt = booking.StartsAt,
            endsAt = booking.EndsAt,
            amount = booking.TotalAmount,
            paymentDeadlineHours,
            rejectionReason,
            cancellationReason
        };

    private sealed record NotificationContent(string Subject, string Body, object Metadata);
}
