using Diamono.Domain.Settings;

namespace Diamono.Application.Abstractions;

public interface IStadiumBookingSettingsRepository
{
    Task<StadiumBookingSettings> GetAsync(CancellationToken cancellationToken);
    Task SaveChangesAsync(CancellationToken cancellationToken);
}
