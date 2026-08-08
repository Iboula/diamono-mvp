using Diamono.Application.Abstractions;
using Diamono.Domain.Bookings;
using Microsoft.EntityFrameworkCore;

namespace Diamono.Infrastructure.Persistence;

public sealed class BookingRepository(DiamonoDbContext db) : IBookingRepository
{
    public async Task<bool> HasConflictAsync(Guid resourceId, DateTimeOffset startsAt, DateTimeOffset endsAt, CancellationToken cancellationToken)
    {
        var activeStatuses = BookingRules.BlockingStatuses;

        var bookingConflict = await db.Bookings.AnyAsync(x =>
            x.ResourceId == resourceId && activeStatuses.Contains(x.Status) &&
            x.StartsAt < endsAt && startsAt < x.EndsAt, cancellationToken);

        if (bookingConflict) return true;

        return await db.BookingBlocks.AnyAsync(x =>
            x.ResourceId == resourceId && x.CancelledAt == null &&
            x.StartsAt < endsAt && startsAt < x.EndsAt,
            cancellationToken);
    }

    public Task AddAsync(Booking booking, CancellationToken cancellationToken)
        => db.Bookings.AddAsync(booking, cancellationToken).AsTask();

    public Task<Booking?> GetByIdAsync(Guid bookingId, CancellationToken cancellationToken)
        => db.Bookings.FirstOrDefaultAsync(x => x.Id == bookingId, cancellationToken);

    public Task<Booking?> GetByPublicAccessTokenAsync(string publicAccessToken, CancellationToken cancellationToken)
        => db.Bookings.FirstOrDefaultAsync(x => x.PublicAccessToken == publicAccessToken, cancellationToken);

    public async Task<IReadOnlyList<Booking>> GetBackOfficeAsync(CancellationToken cancellationToken)
        => await db.Bookings.AsNoTracking()
            .OrderByDescending(x => x.CreatedAt)
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<Booking>> GetUpcomingAsync(CancellationToken cancellationToken)
        => await db.Bookings.AsNoTracking().Where(x => x.EndsAt >= DateTimeOffset.UtcNow)
            .OrderBy(x => x.StartsAt).ToListAsync(cancellationToken);

    public async Task SaveChangesAsync(CancellationToken cancellationToken) => await db.SaveChangesAsync(cancellationToken);
}
