using Diamono.Domain.Bookings;

namespace Diamono.Application.Abstractions;

public interface IBookingRepository
{
    Task<bool> HasConflictAsync(Guid resourceId, DateTimeOffset startsAt, DateTimeOffset endsAt, CancellationToken cancellationToken);
    Task<Booking?> GetByIdAsync(Guid bookingId, CancellationToken cancellationToken);
    Task<Booking?> GetByPublicAccessTokenAsync(string publicAccessToken, CancellationToken cancellationToken);
    Task AddAsync(Booking booking, CancellationToken cancellationToken);
    Task<IReadOnlyList<Booking>> GetBackOfficeAsync(CancellationToken cancellationToken);
    Task<IReadOnlyList<Booking>> GetUpcomingAsync(CancellationToken cancellationToken);
    Task SaveChangesAsync(CancellationToken cancellationToken);
}
