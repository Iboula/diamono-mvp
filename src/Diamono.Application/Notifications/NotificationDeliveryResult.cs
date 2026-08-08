namespace Diamono.Application.Notifications;

public sealed record NotificationDeliveryResult(bool Success, string? Error = null)
{
    public static NotificationDeliveryResult Sent() => new(true);
    public static NotificationDeliveryResult Failed(string error) => new(false, error);
}
