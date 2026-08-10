using Diamono.Domain.Notifications;

namespace Diamono.Application.Notifications;

public sealed record NotificationDeliveryResult(
    bool Success,
    string? Error = null,
    NotificationChannel? Channel = null,
    string? Provider = null,
    string? ProviderMessageId = null,
    string? ErrorCode = null)
{
    public static NotificationDeliveryResult Sent(
        NotificationChannel? channel = null,
        string? provider = null,
        string? providerMessageId = null)
        => new(true, Channel: channel, Provider: provider, ProviderMessageId: providerMessageId);

    public static NotificationDeliveryResult Failed(
        string error,
        NotificationChannel? channel = null,
        string? provider = null,
        string? errorCode = null)
        => new(false, error, channel, provider, ErrorCode: errorCode);
}
