using Diamono.Application.Abstractions;
using Diamono.Application.Availability;
using Diamono.Domain.Bookings;
using Diamono.Domain.Facilities;
using Microsoft.EntityFrameworkCore;

namespace Diamono.Infrastructure.Persistence;

public sealed class AvailabilityRepository(DiamonoDbContext db) : IAvailabilityRepository
{
    public Task<Resource?> GetResourceAsync(Guid resourceId, CancellationToken cancellationToken)
        => db.Resources.AsNoTracking().FirstOrDefaultAsync(x => x.Id == resourceId, cancellationToken);

    public async Task<IReadOnlyList<OccupiedPeriod>> GetOccupiedPeriodsAsync(
        Guid resourceId, DateTimeOffset from, DateTimeOffset to, CancellationToken cancellationToken)
    {
        // Deux requêtes fixes pour la journée entière, quel que soit le nombre de créneaux.
        var bookings = await db.Bookings.AsNoTracking()
            .Where(x => x.ResourceId == resourceId
                && BookingRules.BlockingStatuses.Contains(x.Status)
                && x.StartsAt < to && x.EndsAt > from)
            .Select(x => new { x.StartsAt, x.EndsAt })
            .ToListAsync(cancellationToken);

        var blocks = await db.BookingBlocks.AsNoTracking()
            .Where(x => x.ResourceId == resourceId && x.CancelledAt == null &&
                x.StartsAt < to && x.EndsAt > from)
            .Select(x => new { x.StartsAt, x.EndsAt, x.Reason })
            .ToListAsync(cancellationToken);

        var periods = new List<OccupiedPeriod>(bookings.Count + blocks.Count);
        periods.AddRange(bookings.Select(x => new OccupiedPeriod(
            x.StartsAt, x.EndsAt, OccupancyKind.Booking, AvailabilityApplicationService.BookedReason)));
        periods.AddRange(blocks.Select(x => new OccupiedPeriod(
            x.StartsAt, x.EndsAt, OccupancyKind.Block, x.Reason)));

        return periods;
    }
}
