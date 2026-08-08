using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Diamono.Infrastructure.Persistence;

public sealed class DiamonoDbContextFactory : IDesignTimeDbContextFactory<DiamonoDbContext>
{
    public DiamonoDbContext CreateDbContext(string[] args)
    {
        var options = new DbContextOptionsBuilder<DiamonoDbContext>()
            .UseNpgsql("Host=localhost;Database=diamono_design_time;Username=postgres;Password=postgres")
            .Options;

        return new DiamonoDbContext(options);
    }
}
