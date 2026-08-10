using Diamono.Domain.Notifications;

namespace Diamono.Application.Notifications;

public sealed record NotificationReadModel(
    Guid Id,
    DateTimeOffset CreatedAt,
    Guid BookingId,
    string BookingReference,
    string Recipient,
    NotificationChannel Channel,
    NotificationTemplate Template,
    NotificationStatus Status,
    DateTimeOffset? SentAt,
    string? Error,
    string? MetadataJson,
    string? Provider,
    string? ProviderMessageId,
    string? ErrorCode,
    string? ErrorMessageSafe);
