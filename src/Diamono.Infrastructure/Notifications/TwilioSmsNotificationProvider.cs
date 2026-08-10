using Diamono.Application.Notifications;
using Diamono.Domain.Notifications;
using Microsoft.Extensions.Logging;

namespace Diamono.Infrastructure.Notifications;

public sealed class TwilioSmsNotificationProvider(
    TwilioNotificationOptions options,
    IPhoneNumberNormalizer normalizer,
    ITwilioMessageGateway gateway,
    ILogger<TwilioSmsNotificationProvider> logger)
{
    public bool IsConfigured => options.HasSmsCredentials;

    public async Task<NotificationDeliveryResult> SendAsync(
        NotificationRequest request,
        CancellationToken cancellationToken = default)
    {
        if (!options.HasSmsCredentials)
            return NotificationDeliveryResult.Failed(
                "Twilio SMS n'est pas configure.",
                NotificationChannel.Sms,
                "Twilio",
                "MissingConfiguration");

        string to;
        try
        {
            to = normalizer.NormalizeToE164(request.Recipient, options.DefaultCountryCallingCode);
        }
        catch (ArgumentException ex)
        {
            return NotificationDeliveryResult.Failed(ex.Message, NotificationChannel.Sms, "Twilio", "InvalidPhoneNumber");
        }

        var result = await gateway.SendMessageAsync(new TwilioMessageRequest(
            options.AccountSid!,
            options.AuthToken!,
            options.SmsFrom!,
            to,
            request.Body),
            cancellationToken);

        if (result.Success)
        {
            logger.LogInformation("Twilio SMS sent to {Recipient}.", MaskPhone(to));
            return NotificationDeliveryResult.Sent(NotificationChannel.Sms, "Twilio", result.ProviderMessageId);
        }

        logger.LogWarning(
            "Twilio SMS failed for {Recipient}: {ErrorCode} {ErrorMessageSafe}.",
            MaskPhone(to),
            result.ErrorCode,
            result.ErrorMessageSafe);
        return NotificationDeliveryResult.Failed(
            result.ErrorMessageSafe ?? "Echec Twilio SMS.",
            NotificationChannel.Sms,
            "Twilio",
            result.ErrorCode);
    }

    private static string MaskPhone(string phone)
        => phone.Length <= 6 ? "******" : $"{phone[..Math.Min(6, phone.Length)]}*****{phone[^2..]}";
}
