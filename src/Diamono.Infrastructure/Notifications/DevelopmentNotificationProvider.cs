using Diamono.Application.Notifications;

namespace Diamono.Infrastructure.Notifications;

public sealed class DevelopmentNotificationProvider : INotificationProvider
{
    public Task<NotificationDeliveryResult> SendAsync(
        NotificationRequest request,
        CancellationToken cancellationToken = default)
        => Task.FromResult(NotificationDeliveryResult.Sent());
}
