using Diamono.Application.Abstractions;
using Diamono.Domain.Bookings;
using Microsoft.EntityFrameworkCore;

namespace Diamono.Infrastructure.Persistence;

public sealed class BookingBlockRepository(DiamonoDbContext db) : IBookingBlockRepository
{
    public Task<BookingBlock?> GetByIdAsync(Guid blockId, CancellationToken cancellationToken)
        => db.BookingBlocks.FirstOrDefaultAsync(x => x.Id == blockId, cancellationToken);

    public async Task<IReadOnlyList<BookingBlock>> GetActiveAsync(CancellationToken cancellationToken)
        => await db.BookingBlocks.AsNoTracking()
            .Where(x => x.CancelledAt == null)
            .OrderBy(x => x.StartsAt)
            .ToListAsync(cancellationToken);

    public Task AddAsync(BookingBlock block, CancellationToken cancellationToken)
        => db.BookingBlocks.AddAsync(block, cancellationToken).AsTask();

    public async Task SaveChangesAsync(CancellationToken cancellationToken)
        => await db.SaveChangesAsync(cancellationToken);
}
