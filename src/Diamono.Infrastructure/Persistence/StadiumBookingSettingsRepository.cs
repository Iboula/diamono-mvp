using Diamono.Application.Abstractions;
using Diamono.Domain.Settings;
using Microsoft.EntityFrameworkCore;

namespace Diamono.Infrastructure.Persistence;

public sealed class StadiumBookingSettingsRepository(DiamonoDbContext db) : IStadiumBookingSettingsRepository
{
    public async Task<StadiumBookingSettings> GetAsync(CancellationToken cancellationToken)
    {
        var settings = await db.StadiumBookingSettings
            .FirstOrDefaultAsync(x => x.Id == StadiumBookingSettings.SingletonId, cancellationToken);

        if (settings is not null) return settings;

        settings = StadiumBookingSettings.MvpDefaults();
        db.StadiumBookingSettings.Add(settings);
        await db.SaveChangesAsync(cancellationToken);
        return settings;
    }

    public async Task SaveChangesAsync(CancellationToken cancellationToken)
        => await db.SaveChangesAsync(cancellationToken);
}
