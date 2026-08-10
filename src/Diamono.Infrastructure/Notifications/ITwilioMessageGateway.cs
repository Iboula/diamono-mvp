namespace Diamono.Infrastructure.Notifications;

public interface ITwilioMessageGateway
{
    Task<TwilioMessageResult> SendMessageAsync(
        TwilioMessageRequest request,
        CancellationToken cancellationToken = default);
}
