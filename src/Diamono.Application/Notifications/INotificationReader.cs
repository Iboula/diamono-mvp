namespace Diamono.Application.Notifications;

public interface INotificationReader
{
    Task<IReadOnlyList<NotificationReadModel>> GetNotificationsAsync(
        NotificationQuery query,
        CancellationToken cancellationToken = default);
}
