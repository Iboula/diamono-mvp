using Diamono.Application.Abstractions;
using Diamono.Domain.Settings;

namespace Diamono.Application.Tests;

internal sealed class FakeStadiumBookingSettingsRepository(
    StadiumBookingSettings? settings = null) : IStadiumBookingSettingsRepository
{
    private StadiumBookingSettings settings = settings ?? StadiumBookingSettings.MvpDefaults();

    public int SaveChangesCallCount { get; private set; }

    public Task<StadiumBookingSettings> GetAsync(CancellationToken cancellationToken)
        => Task.FromResult(settings);

    public Task SaveChangesAsync(CancellationToken cancellationToken)
    {
        SaveChangesCallCount++;
        return Task.CompletedTask;
    }
}
