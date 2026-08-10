namespace Diamono.Domain.Notifications;

public sealed class NotificationLog
{
    private NotificationLog() { }

    public NotificationLog(
        Guid bookingId,
        string recipient,
        NotificationChannel channel,
        NotificationTemplate template,
        string subject,
        string body,
        string? metadataJson = null)
    {
        if (bookingId == Guid.Empty) throw new ArgumentException("Booking id is required.", nameof(bookingId));
        if (string.IsNullOrWhiteSpace(recipient)) throw new ArgumentException("Recipient is required.", nameof(recipient));
        if (string.IsNullOrWhiteSpace(subject)) throw new ArgumentException("Subject is required.", nameof(subject));
        if (string.IsNullOrWhiteSpace(body)) throw new ArgumentException("Body is required.", nameof(body));

        Id = Guid.NewGuid();
        BookingId = bookingId;
        Recipient = recipient.Trim();
        Channel = channel;
        Template = template;
        Subject = subject.Trim();
        Body = body.Trim();
        MetadataJson = string.IsNullOrWhiteSpace(metadataJson) ? null : metadataJson;
        Status = NotificationStatus.Pending;
        CreatedAt = DateTimeOffset.UtcNow;
    }

    public Guid Id { get; private set; }
    public Guid BookingId { get; private set; }
    public string Recipient { get; private set; } = string.Empty;
    public NotificationChannel Channel { get; private set; }
    public NotificationTemplate Template { get; private set; }
    public NotificationStatus Status { get; private set; }
    public string Subject { get; private set; } = string.Empty;
    public string Body { get; private set; } = string.Empty;
    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset? SentAt { get; private set; }
    public string? Error { get; private set; }
    public string? MetadataJson { get; private set; }
    public string? Provider { get; private set; }
    public string? ProviderMessageId { get; private set; }
    public string? ErrorCode { get; private set; }
    public string? ErrorMessageSafe { get; private set; }

    public void MarkSent(
        NotificationChannel? channel = null,
        string? provider = null,
        string? providerMessageId = null)
    {
        if (channel is not null) Channel = channel.Value;
        Provider = NormalizeOptional(provider);
        ProviderMessageId = NormalizeOptional(providerMessageId);
        Status = NotificationStatus.Sent;
        SentAt = DateTimeOffset.UtcNow;
        Error = null;
        ErrorCode = null;
        ErrorMessageSafe = null;
    }

    public void MarkFailed(
        string error,
        NotificationChannel? channel = null,
        string? provider = null,
        string? errorCode = null)
    {
        if (string.IsNullOrWhiteSpace(error)) throw new ArgumentException("Error is required.", nameof(error));

        if (channel is not null) Channel = channel.Value;
        Provider = NormalizeOptional(provider);
        Status = NotificationStatus.Failed;
        Error = error.Trim();
        ErrorMessageSafe = error.Trim();
        ErrorCode = NormalizeOptional(errorCode);
    }

    public void MarkDeliveryStatus(NotificationStatus status, string? errorCode = null, string? errorMessageSafe = null)
    {
        if (status is not NotificationStatus.Pending and not NotificationStatus.Sent and not NotificationStatus.Delivered and not NotificationStatus.Failed)
            throw new ArgumentOutOfRangeException(nameof(status), status, "Status callback unsupported.");

        Status = status;
        if (status == NotificationStatus.Pending)
            return;

        if (status is NotificationStatus.Sent or NotificationStatus.Delivered)
        {
            SentAt ??= DateTimeOffset.UtcNow;
            Error = null;
            ErrorCode = null;
            ErrorMessageSafe = null;
            return;
        }

        ErrorCode = NormalizeOptional(errorCode);
        ErrorMessageSafe = NormalizeOptional(errorMessageSafe) ?? "Echec de livraison Twilio.";
        Error = ErrorMessageSafe;
    }

    private static string? NormalizeOptional(string? value)
        => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
