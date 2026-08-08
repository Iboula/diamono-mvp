using Diamono.Domain.Facilities;
using Microsoft.EntityFrameworkCore;

namespace Diamono.Infrastructure.Persistence;

public static class SeedData
{
    public static readonly Guid MainPitchId = Guid.Parse("7f099973-c811-4afe-beca-c9a1b8fcd001");

    public static async Task InitializeAsync(DiamonoDbContext db, CancellationToken cancellationToken = default)
    {
        await db.Database.EnsureCreatedAsync(cancellationToken);
        if (await db.Resources.AnyAsync(cancellationToken)) return;

        db.Resources.Add(new Resource(MainPitchId, "Terrain principal - Stade Diamono", new TimeOnly(8, 0), new TimeOnly(23, 0)));
        await db.SaveChangesAsync(cancellationToken);
    }
}
