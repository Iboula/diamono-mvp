using Diamono.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Diamono.Web.Startup;

public interface IDatabaseMigrationRunner
{
    bool IsRelational { get; }

    Task<IReadOnlyList<string>> GetPendingMigrationsAsync(CancellationToken cancellationToken = default);

    Task MigrateAsync(CancellationToken cancellationToken = default);
}

public interface ISeedDataRunner
{
    Task InitializeAsync(CancellationToken cancellationToken = default);
}

public sealed class DatabaseStartupCoordinator
{
    private readonly IDatabaseMigrationRunner migrationRunner;
    private readonly ISeedDataRunner seedDataRunner;
    private readonly ILogger<DatabaseStartupCoordinator> logger;

    public DatabaseStartupCoordinator(
        IDatabaseMigrationRunner migrationRunner,
        ISeedDataRunner seedDataRunner,
        ILogger<DatabaseStartupCoordinator> logger)
    {
        this.migrationRunner = migrationRunner;
        this.seedDataRunner = seedDataRunner;
        this.logger = logger;
    }

    public async Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            if (migrationRunner.IsRelational)
            {
                var pendingMigrations = await migrationRunner.GetPendingMigrationsAsync(cancellationToken);
                logger.LogInformation(
                    "Applying database migrations. Pending migrations: {PendingMigrationCount}.",
                    pendingMigrations.Count);
            }
            else
            {
                logger.LogInformation("Applying database migrations for non-relational test provider.");
            }

            await migrationRunner.MigrateAsync(cancellationToken);
            logger.LogInformation("Database migrations applied successfully.");
        }
        catch (Exception ex)
        {
            logger.LogCritical(ex, "Database migration failed during startup.");
            throw;
        }

        logger.LogInformation("Initializing seed data.");
        await seedDataRunner.InitializeAsync(cancellationToken);
        logger.LogInformation("Seed data initialization completed.");
    }
}

public sealed class EfDatabaseMigrationRunner : IDatabaseMigrationRunner
{
    private readonly DiamonoDbContext db;

    public EfDatabaseMigrationRunner(DiamonoDbContext db)
    {
        this.db = db;
    }

    public bool IsRelational => db.Database.IsRelational();

    public async Task<IReadOnlyList<string>> GetPendingMigrationsAsync(CancellationToken cancellationToken = default)
        => (await db.Database.GetPendingMigrationsAsync(cancellationToken)).ToList();

    public async Task MigrateAsync(CancellationToken cancellationToken = default)
    {
        if (db.Database.IsRelational())
        {
            await db.Database.MigrateAsync(cancellationToken);
        }
    }
}

public sealed class SeedDataRunner : ISeedDataRunner
{
    private readonly DiamonoDbContext db;

    public SeedDataRunner(DiamonoDbContext db)
    {
        this.db = db;
    }

    public Task InitializeAsync(CancellationToken cancellationToken = default)
        => SeedData.InitializeAsync(db, cancellationToken);
}
