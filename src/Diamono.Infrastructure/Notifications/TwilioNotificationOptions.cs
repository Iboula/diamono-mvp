using Microsoft.Extensions.Configuration;

namespace Diamono.Infrastructure.Notifications;

public sealed class TwilioNotificationOptions
{
    public TwilioNotificationMode Mode { get; init; } = TwilioNotificationMode.Development;
    public string? AccountSid { get; init; }
    public string? AuthToken { get; init; }
    public string? SmsFrom { get; init; }
    public string? WhatsAppFrom { get; init; }
    public string DefaultCountryCallingCode { get; init; } = "+221";

    public bool HasSmsCredentials =>
        !string.IsNullOrWhiteSpace(AccountSid)
        && !string.IsNullOrWhiteSpace(AuthToken)
        && !string.IsNullOrWhiteSpace(SmsFrom);

    public bool HasWhatsAppCredentials =>
        !string.IsNullOrWhiteSpace(AccountSid)
        && !string.IsNullOrWhiteSpace(AuthToken)
        && !string.IsNullOrWhiteSpace(WhatsAppFrom);

    public static TwilioNotificationOptions FromConfiguration(IConfiguration configuration)
    {
        var modeValue = configuration["DIAMONO_NOTIFICATION_MODE"]
            ?? configuration["TWILIO_NOTIFICATION_MODE"]
            ?? (configuration["ASPNETCORE_ENVIRONMENT"] == "Production" ? "Production" : "Development");

        var mode = Enum.TryParse<TwilioNotificationMode>(modeValue, ignoreCase: true, out var parsed)
            ? parsed
            : TwilioNotificationMode.Development;

        return new TwilioNotificationOptions
        {
            Mode = mode,
            AccountSid = configuration["TWILIO_ACCOUNT_SID"],
            AuthToken = configuration["TWILIO_AUTH_TOKEN"],
            SmsFrom = configuration["TWILIO_SMS_FROM"],
            WhatsAppFrom = configuration["TWILIO_WHATSAPP_FROM"],
            DefaultCountryCallingCode = configuration["DIAMONO_DEFAULT_COUNTRY_CALLING_CODE"] ?? "+221"
        };
    }
}
