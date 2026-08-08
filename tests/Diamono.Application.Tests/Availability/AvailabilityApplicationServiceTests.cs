using Diamono.Application.Availability;
using Diamono.Domain.Facilities;
using Xunit;

namespace Diamono.Application.Tests;

public sealed class AvailabilityApplicationServiceTests
{
    private static readonly Guid MainPitchId = Guid.Parse("7f099973-c811-4afe-beca-c9a1b8fcd001");
    private static readonly DateOnly Day = new(2026, 8, 15);

    private static Resource MainPitch() =>
        new(MainPitchId, "Terrain principal - Stade Diamono", new TimeOnly(8, 0), new TimeOnly(23, 0));

    private static DateTimeOffset At(int hour) => new(Day.ToDateTime(new TimeOnly(hour, 0)), TimeSpan.Zero);

    private static OccupiedPeriod Booking(int startHour, int endHour) =>
        new(At(startHour), At(endHour), OccupancyKind.Booking, "Réservé");

    private static OccupiedPeriod Block(int startHour, int endHour, string reason) =>
        new(At(startHour), At(endHour), OccupancyKind.Block, reason);

    private static async Task<IReadOnlyList<AvailabilitySlot>> SlotsAsync(params OccupiedPeriod[] periods)
    {
        var service = new AvailabilityApplicationService(
            new FakeAvailabilityRepository(MainPitch(), periods),
            new FakeStadiumBookingSettingsRepository());
        return await service.GetDailyAvailabilityAsync(MainPitchId, Day);
    }

    private static AvailabilitySlot SlotAt(IReadOnlyList<AvailabilitySlot> slots, int hour)
        => slots.Single(s => s.StartsAt.Hour == hour);

    [Fact]
    public async Task Grille_couvre_les_heures_douverture_par_creneaux_de_deux_heures()
    {
        var slots = await SlotsAsync();

        // 08:00–23:00 : sept créneaux de 2 h, le reliquat 22:00–23:00 n'est pas réservable (BR-002).
        Assert.Equal(7, slots.Count);
        Assert.Equal(At(8), slots[0].StartsAt);
        Assert.Equal(At(22), slots[^1].EndsAt);
        Assert.All(slots, s => Assert.Equal(TimeSpan.FromHours(2), s.EndsAt - s.StartsAt));
    }

    [Fact]
    public async Task Aucune_reservation_tous_les_creneaux_sont_disponibles()
    {
        var slots = await SlotsAsync();

        Assert.All(slots, s => Assert.True(s.IsAvailable));
        Assert.All(slots, s => Assert.Null(s.UnavailableReason));
    }

    [Fact]
    public async Task Reservation_18h_20h_rend_le_creneau_18h_20h_indisponible()
    {
        var slots = await SlotsAsync(Booking(18, 20));

        var slot = SlotAt(slots, 18);
        Assert.False(slot.IsAvailable);
        Assert.Equal("Réservé", slot.UnavailableReason);
    }

    [Fact]
    public async Task Reservation_18h_20h_laisse_le_creneau_16h_18h_disponible()
    {
        var slots = await SlotsAsync(Booking(18, 20));

        Assert.True(SlotAt(slots, 16).IsAvailable);
    }

    [Fact]
    public async Task Reservation_18h_20h_laisse_le_creneau_20h_22h_disponible()
    {
        var slots = await SlotsAsync(Booking(18, 20));

        Assert.True(SlotAt(slots, 20).IsAvailable);
    }

    [Fact]
    public async Task Chevauchement_partiel_est_detecte_sur_les_deux_creneaux_touches()
    {
        // 17:00–19:00 déborde sur 16:00–18:00 et sur 18:00–20:00.
        var slots = await SlotsAsync(Booking(17, 19));

        Assert.False(SlotAt(slots, 16).IsAvailable);
        Assert.False(SlotAt(slots, 18).IsAvailable);
        Assert.True(SlotAt(slots, 14).IsAvailable);
        Assert.True(SlotAt(slots, 20).IsAvailable);
    }

    [Fact]
    public async Task BookingBlock_rend_le_creneau_indisponible_avec_son_motif()
    {
        var slots = await SlotsAsync(Block(12, 14, "Maintenance de la pelouse"));

        var slot = SlotAt(slots, 12);
        Assert.False(slot.IsAvailable);
        Assert.Equal("Maintenance de la pelouse", slot.UnavailableReason);
        Assert.True(SlotAt(slots, 10).IsAvailable);
        Assert.True(SlotAt(slots, 14).IsAvailable);
    }

    [Fact]
    public async Task La_journee_est_chargee_en_une_seule_lecture()
    {
        var repository = new FakeAvailabilityRepository(MainPitch(), Booking(18, 20));
        var service = new AvailabilityApplicationService(repository, new FakeStadiumBookingSettingsRepository());

        await service.GetDailyAvailabilityAsync(MainPitchId, Day);

        // Pas d'appel DB par créneau.
        Assert.Equal(1, repository.GetOccupiedPeriodsCallCount);
    }

    [Fact]
    public async Task Ressource_inconnue_leve_une_erreur_explicite()
    {
        var service = new AvailabilityApplicationService(
            new FakeAvailabilityRepository(resource: null),
            new FakeStadiumBookingSettingsRepository());

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => service.GetDailyAvailabilityAsync(MainPitchId, Day));
    }
}
