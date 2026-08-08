using Diamono.Domain.Notifications;

namespace Diamono.Application.Notifications;

public sealed record NotificationQuery(
    DateOnly? Date = null,
    NotificationStatus? Status = null,
    NotificationChannel? Channel = null);
