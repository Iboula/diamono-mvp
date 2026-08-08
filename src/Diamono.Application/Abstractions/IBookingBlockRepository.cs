using Diamono.Domain.Bookings;

namespace Diamono.Application.Abstractions;

public interface IBookingBlockRepository
{
    Task<BookingBlock?> GetByIdAsync(Guid blockId, CancellationToken cancellationToken);
    Task<IReadOnlyList<BookingBlock>> GetActiveAsync(CancellationToken cancellationToken);
    Task AddAsync(BookingBlock block, CancellationToken cancellationToken);
    Task SaveChangesAsync(CancellationToken cancellationToken);
}
