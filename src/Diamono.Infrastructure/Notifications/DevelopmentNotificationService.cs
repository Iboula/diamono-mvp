using System.Text.Json;
using Diamono.Application.Notifications;
using Diamono.Domain.Notifications;
using Diamono.Infrastructure.Persistence;

namespace Diamono.Infrastructure.Notifications;

public sealed class DevelopmentNotificationService(
    DiamonoDbContext db,
    INotificationProvider provider) : INotificationService
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public async Task NotifyAsync(NotificationRequest request, CancellationToken cancellationToken = default)
    {
        var log = new NotificationLog(
            request.BookingId,
            request.Recipient,
            request.Channel,
            request.Template,
            request.Subject,
            request.Body,
            request.Metadata is null ? null : JsonSerializer.Serialize(request.Metadata, JsonOptions));

        await db.NotificationLogs.AddAsync(log, cancellationToken);

        var result = await provider.SendAsync(request, cancellationToken);
        if (result.Success)
        {
            log.MarkSent();
            return;
        }

        log.MarkFailed(result.Error ?? "Echec de livraison de la notification.");
    }
}
