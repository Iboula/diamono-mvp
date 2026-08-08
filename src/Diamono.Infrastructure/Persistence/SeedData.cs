using Diamono.Domain.Facilities;
using Diamono.Domain.Settings;
using Microsoft.EntityFrameworkCore;

namespace Diamono.Infrastructure.Persistence;

public static class SeedData
{
    public static readonly Guid MainPitchId = Guid.Parse("7f099973-c811-4afe-beca-c9a1b8fcd001");

    public static async Task InitializeAsync(DiamonoDbContext db, CancellationToken cancellationToken = default)
    {
        await db.Database.EnsureCreatedAsync(cancellationToken);
        var settings = await db.StadiumBookingSettings
            .FirstOrDefaultAsync(x => x.Id == StadiumBookingSettings.SingletonId, cancellationToken);
        if (settings is null)
        {
            settings = StadiumBookingSettings.MvpDefaults();
            db.StadiumBookingSettings.Add(settings);
        }

        if (!await db.Resources.AnyAsync(cancellationToken))
        {
            db.Resources.Add(new Resource(MainPitchId, "Terrain principal - Stade Diamono", settings.OpensAt, settings.ClosesAt));
        }

        await db.SaveChangesAsync(cancellationToken);
    }
}
