using System.Security.Cryptography;
using System.Text;
using Diamono.Domain.Notifications;
using Diamono.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Diamono.Infrastructure.Notifications;

public sealed class TwilioStatusCallbackHandler(
    TwilioNotificationOptions options,
    DiamonoDbContext db,
    ILogger<TwilioStatusCallbackHandler> logger)
{
    public async Task<TwilioStatusCallbackResult> HandleAsync(
        string callbackUrl,
        IReadOnlyDictionary<string, string> form,
        string? signature,
        CancellationToken cancellationToken = default)
    {
        if (!ValidateSignature(callbackUrl, form, signature))
            return TwilioStatusCallbackResult.Rejected();

        var providerMessageId = GetValue(form, "MessageSid") ?? GetValue(form, "SmsSid");
        if (string.IsNullOrWhiteSpace(providerMessageId))
            return TwilioStatusCallbackResult.Ok();

        var log = await db.NotificationLogs
            .FirstOrDefaultAsync(x => x.ProviderMessageId == providerMessageId, cancellationToken);
        if (log is null)
        {
            logger.LogInformation("Twilio status callback ignored for unknown message id.");
            return TwilioStatusCallbackResult.Ok();
        }

        var status = MapStatus(GetValue(form, "MessageStatus") ?? GetValue(form, "SmsStatus"));
        if (status is null)
            return TwilioStatusCallbackResult.Ok();

        log.MarkDeliveryStatus(
            status.Value,
            GetValue(form, "ErrorCode"),
            SafeErrorMessage(GetValue(form, "ErrorMessage")));
        await db.SaveChangesAsync(cancellationToken);
        return TwilioStatusCallbackResult.Ok();
    }

    private bool ValidateSignature(
        string callbackUrl,
        IReadOnlyDictionary<string, string> form,
        string? signature)
    {
        if (string.IsNullOrWhiteSpace(options.AuthToken) || string.IsNullOrWhiteSpace(signature))
            return false;

        var payload = new StringBuilder(callbackUrl);
        foreach (var key in form.Keys.Order(StringComparer.Ordinal))
            payload.Append(key).Append(form[key]);

        using var hmac = new HMACSHA1(Encoding.UTF8.GetBytes(options.AuthToken));
        var expected = Convert.ToBase64String(hmac.ComputeHash(Encoding.UTF8.GetBytes(payload.ToString())));
        return CryptographicOperations.FixedTimeEquals(
            Encoding.UTF8.GetBytes(expected),
            Encoding.UTF8.GetBytes(signature));
    }

    private static NotificationStatus? MapStatus(string? status)
        => status?.ToLowerInvariant() switch
        {
            "queued" => NotificationStatus.Pending,
            "sent" => NotificationStatus.Sent,
            "delivered" => NotificationStatus.Delivered,
            "failed" => NotificationStatus.Failed,
            "undelivered" => NotificationStatus.Failed,
            _ => null
        };

    private static string? GetValue(IReadOnlyDictionary<string, string> values, string key)
        => values.TryGetValue(key, out var value) ? value : null;

    private static string? SafeErrorMessage(string? value)
        => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
