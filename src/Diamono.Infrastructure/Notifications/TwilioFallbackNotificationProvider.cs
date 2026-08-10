using Diamono.Application.Notifications;

namespace Diamono.Infrastructure.Notifications;

public sealed class TwilioFallbackNotificationProvider(
    TwilioNotificationOptions options,
    TwilioWhatsAppNotificationProvider whatsAppProvider,
    TwilioSmsNotificationProvider smsProvider,
    DevelopmentNotificationProvider developmentProvider) : INotificationProvider
{
    public async Task<NotificationDeliveryResult> SendAsync(
        NotificationRequest request,
        CancellationToken cancellationToken = default)
    {
        if (options.Mode == TwilioNotificationMode.Development
            && !options.HasSmsCredentials
            && !options.HasWhatsAppCredentials)
        {
            return await developmentProvider.SendAsync(request, cancellationToken);
        }

        if (whatsAppProvider.IsConfigured && whatsAppProvider.Supports(request.Template))
        {
            var whatsAppResult = await whatsAppProvider.SendAsync(request, cancellationToken);
            if (whatsAppResult.Success)
                return whatsAppResult;

            if (smsProvider.IsConfigured)
                return await smsProvider.SendAsync(request, cancellationToken);

            return whatsAppResult;
        }

        if (smsProvider.IsConfigured)
            return await smsProvider.SendAsync(request, cancellationToken);

        if (options.Mode == TwilioNotificationMode.Development)
            return await developmentProvider.SendAsync(request, cancellationToken);

        return NotificationDeliveryResult.Failed(
            "Aucun canal Twilio n'est configure.",
            request.Channel,
            "Twilio",
            "MissingConfiguration");
    }
}
