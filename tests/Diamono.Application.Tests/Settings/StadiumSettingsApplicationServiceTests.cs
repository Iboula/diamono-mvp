using Diamono.Application.Bookings;
using Diamono.Application.Settings;
using Diamono.Domain.Bookings;
using Diamono.Domain.Pricing;
using Diamono.Domain.Settings;
using Xunit;

namespace Diamono.Application.Tests;

public sealed class StadiumSettingsApplicationServiceTests
{
    private static DateTimeOffset At(int hour) => new(2026, 8, 15, hour, 0, 0, TimeSpan.Zero);

    private static UpdateStadiumBookingSettingsRequest ValidUpdate() =>
        new(
            new TimeOnly(7, 0),
            new TimeOnly(22, 0),
            MinimumDurationHours: 1,
            MaximumDurationHours: 5,
            StandardHourlyRate: 30_000m,
            LocalAscHourlyRate: 12_000m,
            LightingHourlyRate: 7_500m,
            LightingStartsAt: new TimeOnly(18, 0),
            DepositAmount: 20_000m,
            MaximumAdvanceBookingDays: 45,
            PaymentDeadlineHours: 12,
            ApprovalRequired: true);

    [Fact]
    public async Task GetSettingsAsync_recupere_les_parametres()
    {
        var service = new StadiumSettingsApplicationService(
            new FakeStadiumBookingSettingsRepository(), FakePermissionGuard.AllowAll(), new FakeAuditWriter());

        var settings = await service.GetSettingsAsync();

        Assert.Equal(new TimeOnly(8, 0), settings.OpensAt);
        Assert.Equal(25_000m, settings.StandardHourlyRate);
    }

    [Fact]
    public async Task UpdateSettingsAsync_accepte_une_mise_a_jour_valide()
    {
        var repository = new FakeStadiumBookingSettingsRepository();
        var service = new StadiumSettingsApplicationService(repository, FakePermissionGuard.AllowAll(), new FakeAuditWriter());

        var settings = await service.UpdateSettingsAsync(ValidUpdate());

        Assert.Equal(new TimeOnly(7, 0), settings.OpensAt);
        Assert.Equal(30_000m, settings.StandardHourlyRate);
        Assert.Equal(1, repository.SaveChangesCallCount);
    }

    [Fact]
    public async Task UpdateSettingsAsync_refuse_une_fermeture_avant_ou_egale_a_ouverture()
    {
        var service = new StadiumSettingsApplicationService(
            new FakeStadiumBookingSettingsRepository(), FakePermissionGuard.AllowAll(), new FakeAuditWriter());
        var request = ValidUpdate() with { OpensAt = new TimeOnly(22, 0), ClosesAt = new TimeOnly(22, 0) };

        await Assert.ThrowsAsync<ArgumentException>(() => service.UpdateSettingsAsync(request));
    }

    [Fact]
    public async Task UpdateSettingsAsync_refuse_une_duree_max_inferieure_a_min()
    {
        var service = new StadiumSettingsApplicationService(
            new FakeStadiumBookingSettingsRepository(), FakePermissionGuard.AllowAll(), new FakeAuditWriter());
        var request = ValidUpdate() with { MinimumDurationHours = 4, MaximumDurationHours = 2 };

        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() => service.UpdateSettingsAsync(request));
    }

    [Fact]
    public async Task UpdateSettingsAsync_refuse_un_tarif_negatif()
    {
        var service = new StadiumSettingsApplicationService(
            new FakeStadiumBookingSettingsRepository(), FakePermissionGuard.AllowAll(), new FakeAuditWriter());
        var request = ValidUpdate() with { StandardHourlyRate = -1m };

        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() => service.UpdateSettingsAsync(request));
    }

    [Fact]
    public async Task QuoteAsync_utilise_le_tarif_standard_configure()
    {
        var service = BookingServiceWith(StadiumBookingSettings.MvpDefaults());

        var quote = await service.QuoteAsync(At(16), At(18), CustomerCategory.Individual);

        Assert.Equal(50_000m, quote.RentalAmount);
    }

    [Fact]
    public async Task QuoteAsync_utilise_le_tarif_asc_configure()
    {
        var service = BookingServiceWith(StadiumBookingSettings.MvpDefaults());

        var quote = await service.QuoteAsync(At(16), At(18), CustomerCategory.LocalAsc);

        Assert.Equal(30_000m, quote.RentalAmount);
    }

    [Fact]
    public async Task QuoteAsync_utilise_lheure_et_le_tarif_eclairage_configures()
    {
        var settings = StadiumBookingSettings.MvpDefaults();
        settings.Update(
            settings.OpensAt,
            settings.ClosesAt,
            settings.MinimumDurationHours,
            settings.MaximumDurationHours,
            settings.StandardHourlyRate,
            settings.LocalAscHourlyRate,
            lightingHourlyRate: 10_000m,
            lightingStartsAt: new TimeOnly(18, 0),
            settings.DepositAmount,
            settings.MaximumAdvanceBookingDays,
            settings.PaymentDeadlineHours,
            settings.ApprovalRequired);
        var service = BookingServiceWith(settings);

        var quote = await service.QuoteAsync(At(17), At(20), CustomerCategory.Individual);

        Assert.Equal(20_000m, quote.LightingAmount);
    }

    [Fact]
    public async Task QuoteAsync_utilise_la_caution_configuree()
    {
        var settings = StadiumBookingSettings.MvpDefaults();
        settings.Update(
            settings.OpensAt,
            settings.ClosesAt,
            settings.MinimumDurationHours,
            settings.MaximumDurationHours,
            settings.StandardHourlyRate,
            settings.LocalAscHourlyRate,
            settings.LightingHourlyRate,
            settings.LightingStartsAt,
            depositAmount: 40_000m,
            settings.MaximumAdvanceBookingDays,
            settings.PaymentDeadlineHours,
            settings.ApprovalRequired);
        var service = BookingServiceWith(settings);

        var quote = await service.QuoteAsync(At(16), At(18), CustomerCategory.Individual);

        Assert.Equal(40_000m, quote.DepositAmount);
    }

    [Fact]
    public async Task Modifier_les_parametres_modifie_le_devis_sans_changement_de_code()
    {
        var settings = StadiumBookingSettings.MvpDefaults();
        var repository = new FakeStadiumBookingSettingsRepository(settings);
        var bookingService = new BookingApplicationService(
            new FakeBookingRepository(), repository, new MvpPricingPolicy(),
            FakePermissionGuard.AllowAll(), new FakeAuditWriter(), new FakeNotificationService());
        var settingsService = new StadiumSettingsApplicationService(
            repository, FakePermissionGuard.AllowAll(), new FakeAuditWriter());

        var before = await bookingService.QuoteAsync(At(16), At(18), CustomerCategory.Individual);
        await settingsService.UpdateSettingsAsync(ValidUpdate());
        var after = await bookingService.QuoteAsync(At(16), At(18), CustomerCategory.Individual);

        Assert.Equal(50_000m, before.RentalAmount);
        Assert.Equal(60_000m, after.RentalAmount);
    }

    private static BookingApplicationService BookingServiceWith(StadiumBookingSettings settings)
        => new(
            new FakeBookingRepository(),
            new FakeStadiumBookingSettingsRepository(settings),
            new MvpPricingPolicy(),
            FakePermissionGuard.AllowAll(),
            new FakeAuditWriter(),
            new FakeNotificationService());
}
