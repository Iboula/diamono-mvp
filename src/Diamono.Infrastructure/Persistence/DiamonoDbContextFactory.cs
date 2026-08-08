using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Diamono.Infrastructure.Persistence;

public sealed class DiamonoDbContextFactory : IDesignTimeDbContextFactory<DiamonoDbContext>
{
    // Cette fabrique est prioritaire sur le projet de demarrage pour les outils EF Core.
    // Elle doit donc utiliser explicitement la meme base que l'application.
    // Aucun secret n'est embarque : DIAMONO_CONNECTION doit etre fourni par l'environnement.
    private const string ConnectionStringEnvironmentVariable = "DIAMONO_CONNECTION";

    public DiamonoDbContext CreateDbContext(string[] args)
    {
        var connectionString = Environment.GetEnvironmentVariable(ConnectionStringEnvironmentVariable)
            ?? throw new InvalidOperationException($"{ConnectionStringEnvironmentVariable} is required for EF Core design-time operations.");

        var options = new DbContextOptionsBuilder<DiamonoDbContext>()
            .UseNpgsql(connectionString)
            .Options;

        return new DiamonoDbContext(options);
    }
}
