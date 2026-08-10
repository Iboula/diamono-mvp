namespace Diamono.Infrastructure.Notifications;

public sealed record TwilioMessageRequest(
    string AccountSid,
    string AuthToken,
    string From,
    string To,
    string Body);
