using Diamono.Application.Abstractions;
using Diamono.Domain.Bookings;

namespace Diamono.Application.Tests;

internal sealed class FakeBookingBlockRepository(params BookingBlock[] blocks) : IBookingBlockRepository
{
    private readonly List<BookingBlock> blocks = [.. blocks];

    public int SaveChangesCallCount { get; private set; }

    public Task<BookingBlock?> GetByIdAsync(Guid blockId, CancellationToken cancellationToken)
        => Task.FromResult(blocks.FirstOrDefault(x => x.Id == blockId));

    public Task<IReadOnlyList<BookingBlock>> GetActiveAsync(CancellationToken cancellationToken)
        => Task.FromResult<IReadOnlyList<BookingBlock>>([.. blocks.Where(x => x.IsActive).OrderBy(x => x.StartsAt)]);

    public Task AddAsync(BookingBlock block, CancellationToken cancellationToken)
    {
        blocks.Add(block);
        return Task.CompletedTask;
    }

    public Task SaveChangesAsync(CancellationToken cancellationToken)
    {
        SaveChangesCallCount++;
        return Task.CompletedTask;
    }
}
