using Diamono.Application.Abstractions;
using Diamono.Application.Availability;
using Diamono.Domain.Facilities;

namespace Diamono.Application.Tests;

/// <summary>
/// Double en mémoire : reproduit le contrat de lecture (filtrage sur la fenêtre demandée)
/// sans dépendre d'EF Core.
/// </summary>
internal sealed class FakeAvailabilityRepository(Resource? resource, params OccupiedPeriod[] periods)
    : IAvailabilityRepository
{
    public int GetOccupiedPeriodsCallCount { get; private set; }

    public Task<Resource?> GetResourceAsync(Guid resourceId, CancellationToken cancellationToken)
        => Task.FromResult(resource);

    public Task<IReadOnlyList<OccupiedPeriod>> GetOccupiedPeriodsAsync(
        Guid resourceId, DateTimeOffset from, DateTimeOffset to, CancellationToken cancellationToken)
    {
        GetOccupiedPeriodsCallCount++;
        IReadOnlyList<OccupiedPeriod> result =
            [.. periods.Where(p => p.StartsAt < to && p.EndsAt > from)];
        return Task.FromResult(result);
    }
}
