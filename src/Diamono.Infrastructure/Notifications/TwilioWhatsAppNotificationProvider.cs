using Diamono.Application.Notifications;
using Diamono.Domain.Notifications;
using Microsoft.Extensions.Logging;

namespace Diamono.Infrastructure.Notifications;

public sealed class TwilioWhatsAppNotificationProvider(
    TwilioNotificationOptions options,
    IPhoneNumberNormalizer normalizer,
    ITwilioMessageGateway gateway,
    ILogger<TwilioWhatsAppNotificationProvider> logger)
{
    private static readonly HashSet<NotificationTemplate> SupportedTemplates =
    [
        NotificationTemplate.BookingCreated,
        NotificationTemplate.BookingApproved,
        NotificationTemplate.BookingMarkedPaid,
        NotificationTemplate.BookingCancelled
    ];

    public bool IsConfigured => options.HasWhatsAppCredentials;

    public bool Supports(NotificationTemplate template) => SupportedTemplates.Contains(template);

    public async Task<NotificationDeliveryResult> SendAsync(
        NotificationRequest request,
        CancellationToken cancellationToken = default)
    {
        if (!Supports(request.Template))
            return NotificationDeliveryResult.Failed(
                "Template WhatsApp non configure pour ce message.",
                NotificationChannel.WhatsApp,
                "Twilio",
                "UnsupportedTemplate");

        if (!options.HasWhatsAppCredentials)
            return NotificationDeliveryResult.Failed(
                "Twilio WhatsApp n'est pas configure.",
                NotificationChannel.WhatsApp,
                "Twilio",
                "MissingConfiguration");

        string to;
        try
        {
            to = "whatsapp:" + normalizer.NormalizeToE164(request.Recipient, options.DefaultCountryCallingCode);
        }
        catch (ArgumentException ex)
        {
            return NotificationDeliveryResult.Failed(ex.Message, NotificationChannel.WhatsApp, "Twilio", "InvalidPhoneNumber");
        }

        var result = await gateway.SendMessageAsync(new TwilioMessageRequest(
            options.AccountSid!,
            options.AuthToken!,
            options.WhatsAppFrom!,
            to,
            BuildWhatsAppBody(request)),
            cancellationToken);

        if (result.Success)
        {
            logger.LogInformation("Twilio WhatsApp sent to {Recipient}.", MaskPhone(to));
            return NotificationDeliveryResult.Sent(NotificationChannel.WhatsApp, "Twilio", result.ProviderMessageId);
        }

        logger.LogWarning(
            "Twilio WhatsApp failed for {Recipient}: {ErrorCode} {ErrorMessageSafe}.",
            MaskPhone(to),
            result.ErrorCode,
            result.ErrorMessageSafe);
        return NotificationDeliveryResult.Failed(
            result.ErrorMessageSafe ?? "Echec Twilio WhatsApp.",
            NotificationChannel.WhatsApp,
            "Twilio",
            result.ErrorCode);
    }

    private static string BuildWhatsAppBody(NotificationRequest request)
        => request.Template switch
        {
            NotificationTemplate.BookingMarkedPaid => $"{request.Body}\nMerci de votre confiance.",
            _ => request.Body
        };

    private static string MaskPhone(string phone)
    {
        var value = phone.StartsWith("whatsapp:", StringComparison.OrdinalIgnoreCase)
            ? phone["whatsapp:".Length..]
            : phone;
        return value.Length <= 6 ? "******" : $"{value[..Math.Min(6, value.Length)]}*****{value[^2..]}";
    }
}
