namespace Diamono.Infrastructure.Notifications;

public sealed record TwilioMessageResult(
    bool Success,
    string? ProviderMessageId = null,
    string? ErrorCode = null,
    string? ErrorMessageSafe = null)
{
    public static TwilioMessageResult Sent(string providerMessageId) => new(true, providerMessageId);

    public static TwilioMessageResult Failed(string errorMessageSafe, string? errorCode = null)
        => new(false, ErrorCode: errorCode, ErrorMessageSafe: errorMessageSafe);
}
