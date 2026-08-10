namespace Diamono.Infrastructure.Notifications;

public sealed record TwilioStatusCallbackResult(bool Accepted, bool SignatureValid)
{
    public static TwilioStatusCallbackResult Rejected() => new(false, false);
    public static TwilioStatusCallbackResult Ok() => new(true, true);
}
