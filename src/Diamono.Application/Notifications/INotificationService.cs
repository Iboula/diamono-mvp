namespace Diamono.Application.Notifications;

public interface INotificationService
{
    Task NotifyAsync(NotificationRequest request, CancellationToken cancellationToken = default);
}
