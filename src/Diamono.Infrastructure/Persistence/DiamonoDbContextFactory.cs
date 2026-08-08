using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Diamono.Infrastructure.Persistence;

public sealed class DiamonoDbContextFactory : IDesignTimeDbContextFactory<DiamonoDbContext>
{
    // Cette fabrique est prioritaire sur le projet de demarrage pour les outils EF Core.
    // Elle doit donc viser la meme base que l'application, sinon `dotnet ef database update`
    // migre silencieusement une base fantome. Valeur alignee sur docker-compose.yml et
    // src/Diamono.Web/appsettings.json, surchargeable par DIAMONO_CONNECTION.
    private const string DefaultConnectionString =
        "Host=localhost;Port=5432;Database=diamono;Username=diamono;Password=diamono_dev";

    public DiamonoDbContext CreateDbContext(string[] args)
    {
        var connectionString = Environment.GetEnvironmentVariable("DIAMONO_CONNECTION")
            ?? DefaultConnectionString;

        var options = new DbContextOptionsBuilder<DiamonoDbContext>()
            .UseNpgsql(connectionString)
            .Options;

        return new DiamonoDbContext(options);
    }
}
