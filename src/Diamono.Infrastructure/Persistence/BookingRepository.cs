using Diamono.Application.Abstractions;
using Diamono.Domain.Bookings;
using Microsoft.EntityFrameworkCore;

namespace Diamono.Infrastructure.Persistence;

public sealed class BookingRepository(DiamonoDbContext db) : IBookingRepository
{
    public async Task<bool> HasConflictAsync(Guid resourceId, DateTimeOffset startsAt, DateTimeOffset endsAt, CancellationToken cancellationToken)
    {
        var activeStatuses = new[] { BookingStatus.PendingApproval, BookingStatus.AwaitingPayment, BookingStatus.Confirmed };

        var bookingConflict = await db.Bookings.AnyAsync(x =>
            x.ResourceId == resourceId && activeStatuses.Contains(x.Status) &&
            x.StartsAt < endsAt && startsAt < x.EndsAt, cancellationToken);

        if (bookingConflict) return true;

        return await db.BookingBlocks.AnyAsync(x =>
            x.ResourceId == resourceId && x.StartsAt < endsAt && startsAt < x.EndsAt,
            cancellationToken);
    }

    public Task AddAsync(Booking booking, CancellationToken cancellationToken)
        => db.Bookings.AddAsync(booking, cancellationToken).AsTask();

    public async Task<IReadOnlyList<Booking>> GetUpcomingAsync(CancellationToken cancellationToken)
        => await db.Bookings.AsNoTracking().Where(x => x.EndsAt >= DateTimeOffset.UtcNow)
            .OrderBy(x => x.StartsAt).ToListAsync(cancellationToken);

    public async Task SaveChangesAsync(CancellationToken cancellationToken) => await db.SaveChangesAsync(cancellationToken);
}
