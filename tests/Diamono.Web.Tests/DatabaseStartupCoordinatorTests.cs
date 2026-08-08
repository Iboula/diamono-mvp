using Diamono.Web.Startup;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Diamono.Web.Tests;

public sealed class DatabaseStartupCoordinatorTests
{
    [Fact]
    public async Task Fresh_db_applique_les_migrations_avant_seed()
    {
        var recorder = new StartupRecorder();
        var migrations = new FakeMigrationRunner(recorder, pendingMigrations: ["InitialCreate"]);
        var seed = new FakeSeedDataRunner(recorder);
        var coordinator = Coordinator(migrations, seed);

        await coordinator.InitializeAsync();

        Assert.Equal(
            ["get-pending", "migrate", "seed"],
            recorder.Events);
    }

    [Fact]
    public async Task Db_deja_migree_ne_casse_pas_le_startup()
    {
        var recorder = new StartupRecorder();
        var migrations = new FakeMigrationRunner(recorder, pendingMigrations: []);
        var seed = new FakeSeedDataRunner(recorder);
        var coordinator = Coordinator(migrations, seed);

        await coordinator.InitializeAsync();

        Assert.True(migrations.MigrateCalled);
        Assert.True(seed.SeedCalled);
    }

    [Fact]
    public async Task Seed_est_execute_apres_migrations()
    {
        var recorder = new StartupRecorder();
        var migrations = new FakeMigrationRunner(recorder, pendingMigrations: ["A", "B"]);
        var seed = new FakeSeedDataRunner(recorder);
        var coordinator = Coordinator(migrations, seed);

        await coordinator.InitializeAsync();

        Assert.True(recorder.Events.IndexOf("migrate") < recorder.Events.IndexOf("seed"));
    }

    [Fact]
    public async Task Echec_migration_arrete_le_startup_et_ne_seed_pas()
    {
        var recorder = new StartupRecorder();
        var migrations = new FakeMigrationRunner(recorder, pendingMigrations: ["InitialCreate"])
        {
            ThrowOnMigrate = true
        };
        var seed = new FakeSeedDataRunner(recorder);
        var coordinator = Coordinator(migrations, seed);

        await Assert.ThrowsAsync<InvalidOperationException>(() => coordinator.InitializeAsync());

        Assert.False(seed.SeedCalled);
        Assert.DoesNotContain("seed", recorder.Events);
    }

    [Fact]
    public void Aucun_ensure_created_restant_dans_le_runner_postgresql()
    {
        var source = File.ReadAllText(Path.Combine(
            AppContext.BaseDirectory,
            "..",
            "..",
            "..",
            "..",
            "..",
            "src",
            "Diamono.Web",
            "Startup",
            "DatabaseStartupCoordinator.cs"));

        Assert.DoesNotContain("EnsureCreated", source, StringComparison.Ordinal);
        Assert.DoesNotContain("EnsureDeleted", source, StringComparison.Ordinal);
        Assert.Contains("MigrateAsync", source, StringComparison.Ordinal);
    }

    private static DatabaseStartupCoordinator Coordinator(
        IDatabaseMigrationRunner migrations,
        ISeedDataRunner seed)
        => new(migrations, seed, NullLogger<DatabaseStartupCoordinator>.Instance);

    private sealed class StartupRecorder
    {
        public List<string> Events { get; } = [];
    }

    private sealed class FakeMigrationRunner : IDatabaseMigrationRunner
    {
        private readonly StartupRecorder recorder;
        private readonly IReadOnlyList<string> pendingMigrations;

        public FakeMigrationRunner(StartupRecorder recorder, IReadOnlyList<string> pendingMigrations)
        {
            this.recorder = recorder;
            this.pendingMigrations = pendingMigrations;
        }

        public bool IsRelational => true;

        public bool MigrateCalled { get; private set; }

        public bool ThrowOnMigrate { get; init; }

        public Task<IReadOnlyList<string>> GetPendingMigrationsAsync(CancellationToken cancellationToken = default)
        {
            recorder.Events.Add("get-pending");
            return Task.FromResult(pendingMigrations);
        }

        public Task MigrateAsync(CancellationToken cancellationToken = default)
        {
            recorder.Events.Add("migrate");
            MigrateCalled = true;
            if (ThrowOnMigrate)
                throw new InvalidOperationException("Migration failed.");

            return Task.CompletedTask;
        }
    }

    private sealed class FakeSeedDataRunner : ISeedDataRunner
    {
        private readonly StartupRecorder recorder;

        public FakeSeedDataRunner(StartupRecorder recorder)
        {
            this.recorder = recorder;
        }

        public bool SeedCalled { get; private set; }

        public Task InitializeAsync(CancellationToken cancellationToken = default)
        {
            recorder.Events.Add("seed");
            SeedCalled = true;
            return Task.CompletedTask;
        }
    }
}
