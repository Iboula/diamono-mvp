using System.Security.Cryptography;
using System.Text;
using Diamono.Application.Abstractions;
using Diamono.Application.Notifications;
using Diamono.Application.Security;
using Diamono.Domain.Bookings;
using Diamono.Domain.Notifications;
using Diamono.Domain.Security;
using Diamono.Infrastructure.Notifications;
using Diamono.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Diamono.Web.Tests;

public sealed class TwilioNotificationTests
{
    private static readonly Guid ResourceId = Guid.Parse("7f099973-c811-4afe-beca-c9a1b8fcd001");

    [Fact]
    public async Task Provider_success_persiste_sent_et_provider_message_id()
    {
        var gateway = new FakeTwilioGateway(_ => TwilioMessageResult.Sent("SM123"));
        await using var provider = BuildProvider(Options(), gateway);
        var db = provider.GetRequiredService<DiamonoDbContext>();
        var booking = NewBooking("771234567");
        db.Bookings.Add(booking);

        await provider.GetRequiredService<INotificationService>().NotifyAsync(Request(booking, NotificationTemplate.BookingCreated));
        await db.SaveChangesAsync();

        var log = Assert.Single(db.NotificationLogs);
        Assert.Equal(NotificationStatus.Sent, log.Status);
        Assert.Equal(NotificationChannel.WhatsApp, log.Channel);
        Assert.Equal("Twilio", log.Provider);
        Assert.Equal("SM123", log.ProviderMessageId);
    }

    [Fact]
    public async Task Provider_failure_persiste_failed_sans_crasher()
    {
        var gateway = new FakeTwilioGateway(_ => TwilioMessageResult.Failed("Erreur Twilio safe.", "30001"));
        await using var provider = BuildProvider(Options(whatsAppFrom: null), gateway);
        var db = provider.GetRequiredService<DiamonoDbContext>();
        var booking = NewBooking("771234567");
        db.Bookings.Add(booking);

        await provider.GetRequiredService<INotificationService>().NotifyAsync(Request(booking, NotificationTemplate.BookingCreated));
        await db.SaveChangesAsync();

        var log = Assert.Single(db.NotificationLogs);
        Assert.Equal(NotificationStatus.Failed, log.Status);
        Assert.Equal(NotificationChannel.Sms, log.Channel);
        Assert.Equal("Twilio", log.Provider);
        Assert.Equal("30001", log.ErrorCode);
        Assert.Equal("Erreur Twilio safe.", log.ErrorMessageSafe);
    }

    [Fact]
    public async Task Fallback_whatsapp_vers_sms_fonctionne()
    {
        var gateway = new FakeTwilioGateway(request =>
            request.To.StartsWith("whatsapp:", StringComparison.OrdinalIgnoreCase)
                ? TwilioMessageResult.Failed("WhatsApp indisponible.", "63016")
                : TwilioMessageResult.Sent("SM456"));
        await using var provider = BuildProvider(Options(), gateway);
        var db = provider.GetRequiredService<DiamonoDbContext>();
        var booking = NewBooking("+221771234567");
        db.Bookings.Add(booking);

        await provider.GetRequiredService<INotificationService>().NotifyAsync(Request(booking, NotificationTemplate.BookingCreated));
        await db.SaveChangesAsync();

        var log = Assert.Single(db.NotificationLogs);
        Assert.Equal(NotificationStatus.Sent, log.Status);
        Assert.Equal(NotificationChannel.Sms, log.Channel);
        Assert.Equal("SM456", log.ProviderMessageId);
        Assert.Equal(2, gateway.Requests.Count);
    }

    [Fact]
    public async Task Whatsapp_booking_confirmed_genere_message()
    {
        var gateway = new FakeTwilioGateway(_ => TwilioMessageResult.Sent("WA123"));
        await using var provider = BuildProvider(Options(), gateway);
        var booking = NewBooking("771234567");

        await provider.GetRequiredService<INotificationService>().NotifyAsync(Request(booking, NotificationTemplate.BookingMarkedPaid));

        var request = Assert.Single(gateway.Requests);
        Assert.Equal("whatsapp:+221771234567", request.To);
        Assert.Contains("paiement recu", request.Body);
        Assert.Contains("Merci de votre confiance.", request.Body);
    }

    [Fact]
    public async Task Callback_status_est_idempotent()
    {
        await using var provider = BuildProvider(Options(), new FakeTwilioGateway(_ => TwilioMessageResult.Sent("SM123")));
        var db = provider.GetRequiredService<DiamonoDbContext>();
        var log = new NotificationLog(
            Guid.NewGuid(),
            "+221771234567",
            NotificationChannel.Sms,
            NotificationTemplate.BookingCreated,
            "Demande recue",
            "Body");
        log.MarkSent(NotificationChannel.Sms, "Twilio", "SM123");
        db.NotificationLogs.Add(log);
        await db.SaveChangesAsync();

        var handler = provider.GetRequiredService<TwilioStatusCallbackHandler>();
        var form = new Dictionary<string, string>
        {
            ["MessageSid"] = "SM123",
            ["MessageStatus"] = "delivered"
        };
        var signature = Signature("https://diamono.example/api/notifications/twilio/status", form, "secret-token");

        var first = await handler.HandleAsync("https://diamono.example/api/notifications/twilio/status", form, signature);
        var second = await handler.HandleAsync("https://diamono.example/api/notifications/twilio/status", form, signature);

        Assert.True(first.Accepted);
        Assert.True(second.Accepted);
        Assert.Equal(NotificationStatus.Delivered, Assert.Single(db.NotificationLogs).Status);
    }

    [Fact]
    public async Task Callback_signature_invalide_est_rejete()
    {
        await using var provider = BuildProvider(Options(), new FakeTwilioGateway(_ => TwilioMessageResult.Sent("SM123")));
        var handler = provider.GetRequiredService<TwilioStatusCallbackHandler>();

        var result = await handler.HandleAsync(
            "https://diamono.example/api/notifications/twilio/status",
            new Dictionary<string, string> { ["MessageSid"] = "SM123", ["MessageStatus"] = "failed" },
            "invalid-signature");

        Assert.False(result.Accepted);
        Assert.False(result.SignatureValid);
    }

    private static NotificationRequest Request(Booking booking, NotificationTemplate template)
        => new(
            booking.Id,
            booking.Phone,
            NotificationChannel.Sms,
            template,
            "Reservation",
            template == NotificationTemplate.BookingMarkedPaid
                ? $"Stade Diamono : paiement recu. Votre reservation {booking.Reference} est confirmee."
                : $"Stade Diamono : demande {booking.Reference} recue. Nous vous informerons apres validation.",
            new { reference = booking.Reference });

    private static Booking NewBooking(string phone)
    {
        var day = DateTimeOffset.UtcNow.Date.AddDays(7);
        return new Booking(ResourceId, new DateTimeOffset(day.AddHours(18), TimeSpan.Zero),
            new DateTimeOffset(day.AddHours(20), TimeSpan.Zero),
            "Awa Diop", phone, CustomerCategory.Individual,
            "Football", 50_000m, 5_000m, 25_000m);
    }

    private static TwilioNotificationOptions Options(string? whatsAppFrom = "whatsapp:+14155238886")
        => new()
        {
            Mode = TwilioNotificationMode.Production,
            AccountSid = "AC00000000000000000000000000000000",
            AuthToken = "secret-token",
            SmsFrom = "+15551234567",
            WhatsAppFrom = whatsAppFrom
        };

    private static ServiceProvider BuildProvider(TwilioNotificationOptions options, ITwilioMessageGateway gateway)
    {
        var services = new ServiceCollection();
        services.AddDbContext<DiamonoDbContext>(db => db.UseInMemoryDatabase(Guid.NewGuid().ToString()));
        services.AddSingleton<IPermissionGuard>(new AllowAllPermissionGuard());
        services.AddSingleton(options);
        services.AddSingleton<IPhoneNumberNormalizer, PhoneNumberNormalizer>();
        services.AddSingleton(gateway);
        services.AddScoped<DevelopmentNotificationProvider>();
        services.AddScoped<TwilioSmsNotificationProvider>(sp => new TwilioSmsNotificationProvider(
            sp.GetRequiredService<TwilioNotificationOptions>(),
            sp.GetRequiredService<IPhoneNumberNormalizer>(),
            sp.GetRequiredService<ITwilioMessageGateway>(),
            NullLogger<TwilioSmsNotificationProvider>.Instance));
        services.AddScoped<TwilioWhatsAppNotificationProvider>(sp => new TwilioWhatsAppNotificationProvider(
            sp.GetRequiredService<TwilioNotificationOptions>(),
            sp.GetRequiredService<IPhoneNumberNormalizer>(),
            sp.GetRequiredService<ITwilioMessageGateway>(),
            NullLogger<TwilioWhatsAppNotificationProvider>.Instance));
        services.AddScoped<INotificationProvider, TwilioFallbackNotificationProvider>();
        services.AddScoped<INotificationService, DevelopmentNotificationService>();
        services.AddScoped<TwilioStatusCallbackHandler>(sp => new TwilioStatusCallbackHandler(
            sp.GetRequiredService<TwilioNotificationOptions>(),
            sp.GetRequiredService<DiamonoDbContext>(),
            NullLogger<TwilioStatusCallbackHandler>.Instance));
        return services.BuildServiceProvider();
    }

    private static string Signature(string url, IReadOnlyDictionary<string, string> form, string token)
    {
        var payload = new StringBuilder(url);
        foreach (var key in form.Keys.Order(StringComparer.Ordinal))
            payload.Append(key).Append(form[key]);

        using var hmac = new HMACSHA1(Encoding.UTF8.GetBytes(token));
        return Convert.ToBase64String(hmac.ComputeHash(Encoding.UTF8.GetBytes(payload.ToString())));
    }

    private sealed class FakeTwilioGateway(Func<TwilioMessageRequest, TwilioMessageResult> send) : ITwilioMessageGateway
    {
        public List<TwilioMessageRequest> Requests { get; } = [];

        public Task<TwilioMessageResult> SendMessageAsync(TwilioMessageRequest request, CancellationToken cancellationToken = default)
        {
            Requests.Add(request);
            return Task.FromResult(send(request));
        }
    }

    private sealed class AllowAllPermissionGuard : IPermissionGuard
    {
        public Task<bool> HasPermissionAsync(string permission, CancellationToken cancellationToken = default)
            => Task.FromResult(true);

        public Task EnsurePermissionAsync(string permission, CancellationToken cancellationToken = default)
            => Task.CompletedTask;
    }
}
