using Diamono.Application.Notifications;

namespace Diamono.Application.Tests;

internal sealed class FakeNotificationService : INotificationService
{
    public List<NotificationRequest> Requests { get; } = [];

    public Task NotifyAsync(NotificationRequest request, CancellationToken cancellationToken = default)
    {
        Requests.Add(request);
        return Task.CompletedTask;
    }
}
