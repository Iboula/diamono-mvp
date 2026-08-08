using Diamono.Domain.Notifications;

namespace Diamono.Application.Notifications;

public sealed record NotificationRequest(
    Guid BookingId,
    string Recipient,
    NotificationChannel Channel,
    NotificationTemplate Template,
    string Subject,
    string Body,
    object? Metadata = null);
