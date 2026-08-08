using Diamono.Application.Abstractions;
using Diamono.Domain.Bookings;

namespace Diamono.Application.Tests;

internal sealed class FakeBookingRepository(params Booking[] bookings) : IBookingRepository
{
    private readonly List<Booking> bookings = [.. bookings];

    public int SaveChangesCallCount { get; private set; }

    public Task<bool> HasConflictAsync(
        Guid resourceId, DateTimeOffset startsAt, DateTimeOffset endsAt, CancellationToken cancellationToken)
        => Task.FromResult(bookings.Any(x =>
            x.ResourceId == resourceId &&
            BookingRules.BlockingStatuses.Contains(x.Status) &&
            BookingRules.Overlaps(x.StartsAt, x.EndsAt, startsAt, endsAt)));

    public Task<Booking?> GetByIdAsync(Guid bookingId, CancellationToken cancellationToken)
        => Task.FromResult(bookings.FirstOrDefault(x => x.Id == bookingId));

    public Task AddAsync(Booking booking, CancellationToken cancellationToken)
    {
        bookings.Add(booking);
        return Task.CompletedTask;
    }

    public Task<IReadOnlyList<Booking>> GetBackOfficeAsync(CancellationToken cancellationToken)
        => Task.FromResult<IReadOnlyList<Booking>>([.. bookings.OrderByDescending(x => x.CreatedAt)]);

    public Task<IReadOnlyList<Booking>> GetUpcomingAsync(CancellationToken cancellationToken)
        => Task.FromResult<IReadOnlyList<Booking>>([.. bookings]);

    public Task SaveChangesAsync(CancellationToken cancellationToken)
    {
        SaveChangesCallCount++;
        return Task.CompletedTask;
    }
}
