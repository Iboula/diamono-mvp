using Diamono.Application.Abstractions;
using Diamono.Domain.Bookings;

namespace Diamono.Application.Availability;

public sealed class AvailabilityApplicationService(
    IAvailabilityRepository repository,
    IStadiumBookingSettingsRepository settingsRepository)
{
    public const string BookedReason = "Reserve";

    public async Task<IReadOnlyList<AvailabilitySlot>> GetDailyAvailabilityAsync(
        Guid resourceId, DateOnly date, CancellationToken cancellationToken = default)
    {
        _ = await repository.GetResourceAsync(resourceId, cancellationToken)
            ?? throw new InvalidOperationException("Terrain introuvable.");

        var settings = await settingsRepository.GetAsync(cancellationToken);
        if (settings.ClosesAt <= settings.OpensAt) return [];

        var slotDuration = TimeSpan.FromHours(settings.MinimumDurationHours);
        var dayOpens = ToInstant(date, settings.OpensAt);
        var dayCloses = ToInstant(date, settings.ClosesAt);
        var occupied = await repository.GetOccupiedPeriodsAsync(resourceId, dayOpens, dayCloses, cancellationToken);

        var slots = new List<AvailabilitySlot>();
        for (var startsAt = dayOpens; startsAt + slotDuration <= dayCloses; startsAt += slotDuration)
        {
            var endsAt = startsAt + slotDuration;
            var blocker = FindBlocker(occupied, startsAt, endsAt);
            slots.Add(new AvailabilitySlot(startsAt, endsAt, blocker is null, blocker?.Reason));
        }

        return slots;
    }

    private static OccupiedPeriod? FindBlocker(
        IReadOnlyList<OccupiedPeriod> occupied, DateTimeOffset startsAt, DateTimeOffset endsAt)
    {
        foreach (var period in occupied)
        {
            if (BookingRules.Overlaps(period.StartsAt, period.EndsAt, startsAt, endsAt))
                return period;
        }

        return null;
    }

    private static DateTimeOffset ToInstant(DateOnly date, TimeOnly time)
        => new(date.ToDateTime(time), TimeSpan.Zero);
}
