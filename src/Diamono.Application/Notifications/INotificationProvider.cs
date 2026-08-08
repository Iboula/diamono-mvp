namespace Diamono.Application.Notifications;

public interface INotificationProvider
{
    Task<NotificationDeliveryResult> SendAsync(NotificationRequest request, CancellationToken cancellationToken = default);
}
