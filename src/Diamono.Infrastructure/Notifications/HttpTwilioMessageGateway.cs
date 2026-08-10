using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace Diamono.Infrastructure.Notifications;

public sealed class HttpTwilioMessageGateway : ITwilioMessageGateway
{
    private static readonly HttpClient HttpClient = new()
    {
        Timeout = TimeSpan.FromSeconds(12)
    };

    public async Task<TwilioMessageResult> SendMessageAsync(
        TwilioMessageRequest request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            using var message = new HttpRequestMessage(
                HttpMethod.Post,
                $"https://api.twilio.com/2010-04-01/Accounts/{Uri.EscapeDataString(request.AccountSid)}/Messages.json");

            var authBytes = Encoding.ASCII.GetBytes($"{request.AccountSid}:{request.AuthToken}");
            message.Headers.Authorization = new AuthenticationHeaderValue("Basic", Convert.ToBase64String(authBytes));
            message.Content = new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["From"] = request.From,
                ["To"] = request.To,
                ["Body"] = request.Body
            });

            using var response = await HttpClient.SendAsync(message, cancellationToken);
            var json = await response.Content.ReadAsStringAsync(cancellationToken);

            if (!response.IsSuccessStatusCode)
                return TwilioMessageResult.Failed(ReadTwilioError(json) ?? "Echec Twilio.", ((int)response.StatusCode).ToString());

            using var document = JsonDocument.Parse(json);
            var sid = document.RootElement.TryGetProperty("sid", out var sidElement)
                ? sidElement.GetString()
                : null;

            return string.IsNullOrWhiteSpace(sid)
                ? TwilioMessageResult.Failed("Reponse Twilio sans identifiant message.")
                : TwilioMessageResult.Sent(sid);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch
        {
            return TwilioMessageResult.Failed("Echec de communication avec Twilio.");
        }
    }

    private static string? ReadTwilioError(string json)
    {
        if (string.IsNullOrWhiteSpace(json)) return null;

        try
        {
            using var document = JsonDocument.Parse(json);
            return document.RootElement.TryGetProperty("message", out var message)
                ? message.GetString()
                : null;
        }
        catch (JsonException)
        {
            return null;
        }
    }
}
