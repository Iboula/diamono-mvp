using Diamono.Application.Abstractions;
using Diamono.Application.Notifications;
using Diamono.Domain.Security;
using Microsoft.EntityFrameworkCore;

namespace Diamono.Infrastructure.Persistence;

public sealed class NotificationReader(
    DiamonoDbContext db,
    IPermissionGuard permissionGuard) : INotificationReader
{
    public async Task<IReadOnlyList<NotificationReadModel>> GetNotificationsAsync(
        NotificationQuery query,
        CancellationToken cancellationToken = default)
    {
        await permissionGuard.EnsurePermissionAsync(Permissions.AdministrationManage, cancellationToken);

        var notifications = db.NotificationLogs.AsNoTracking();

        if (query.Date is not null)
        {
            var start = new DateTimeOffset(query.Date.Value.ToDateTime(TimeOnly.MinValue), TimeSpan.Zero);
            var end = start.AddDays(1);
            notifications = notifications.Where(x => x.CreatedAt >= start && x.CreatedAt < end);
        }

        if (query.Status is not null)
            notifications = notifications.Where(x => x.Status == query.Status.Value);

        if (query.Channel is not null)
            notifications = notifications.Where(x => x.Channel == query.Channel.Value);

        return await notifications
            .GroupJoin(
                db.Bookings.AsNoTracking(),
                notification => notification.BookingId,
                booking => booking.Id,
                (notification, bookings) => new { notification, bookings })
            .SelectMany(
                x => x.bookings.DefaultIfEmpty(),
                (x, booking) => new NotificationReadModel(
                    x.notification.Id,
                    x.notification.CreatedAt,
                    x.notification.BookingId,
                    booking == null ? "Reservation introuvable" : booking.Reference,
                    x.notification.Recipient,
                    x.notification.Channel,
                    x.notification.Template,
                    x.notification.Status,
                    x.notification.SentAt,
                    x.notification.Error,
                    x.notification.MetadataJson,
                    x.notification.Provider,
                    x.notification.ProviderMessageId,
                    x.notification.ErrorCode,
                    x.notification.ErrorMessageSafe))
            .OrderByDescending(x => x.CreatedAt)
            .ToListAsync(cancellationToken);
    }
}
