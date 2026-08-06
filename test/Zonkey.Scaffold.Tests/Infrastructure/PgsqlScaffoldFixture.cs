using Npgsql;

namespace Zonkey.Scaffold.Tests.Infrastructure;

public sealed class PgsqlScaffoldFixture : IScaffoldFixture
{
    private readonly string _base = TestConfiguration.PgsqlConnectionString;
    private readonly string _databaseName = $"zscaffold_{Guid.NewGuid():N}";

    // Tracks "CREATE DATABASE succeeded" independently of IsAvailable, which only flips true at
    // the very end of InitializeAsync. If the seed step throws after the database was created,
    // IsAvailable stays false but the database still exists and must still be dropped.
    private bool _databaseCreated;

    public bool IsAvailable { get; private set; }
    public string SkipReason { get; private set; } = "";
    public string ConnectionString => $"{_base};Database={_databaseName}";
    public string Provider => "postgresql";
    public string DefaultSchema => "public";

    public async ValueTask InitializeAsync()
    {
        try
        {
            await using (var admin = new NpgsqlConnection($"{_base};Database=postgres"))
            {
                await admin.OpenAsync();
                await using var create = admin.CreateCommand();
                create.CommandText = $"CREATE DATABASE \"{_databaseName}\"";
                await create.ExecuteNonQueryAsync();
            }

            _databaseCreated = true;

            await using var cnxn = new NpgsqlConnection(ConnectionString);
            await cnxn.OpenAsync();
            await using var seed = cnxn.CreateCommand();
            seed.CommandText = File.ReadAllText(
                Path.Combine(AppContext.BaseDirectory, "Seed", "pgsql-scaffold-seed.sql"));
            await seed.ExecuteNonQueryAsync();

            IsAvailable = true;
        }
        catch (Exception ex)
        {
            if (TestConfiguration.RequireDatabase)
                throw new InvalidOperationException(
                    $"PostgreSQL is required (ZONKEY_REQUIRE_DB is set) but setup failed: {ex.Message}", ex);

            IsAvailable = false;
            SkipReason = $"PostgreSQL not available: {ex.Message}. " +
                         "Set ZONKEY_TEST_PGSQL or run 'docker compose up -d --wait'.";
        }
    }

    public async ValueTask DisposeAsync()
    {
        if (!_databaseCreated) return;

        try
        {
            NpgsqlConnection.ClearAllPools();
            await using var admin = new NpgsqlConnection($"{_base};Database=postgres");
            await admin.OpenAsync();

            await using var terminate = admin.CreateCommand();
            terminate.CommandText =
                "SELECT pg_terminate_backend(pid) FROM pg_stat_activity " +
                "WHERE datname = @db AND pid <> pg_backend_pid()";
            terminate.Parameters.AddWithValue("db", _databaseName);
            await terminate.ExecuteNonQueryAsync();

            await using var drop = admin.CreateCommand();
            drop.CommandText = $"DROP DATABASE IF EXISTS \"{_databaseName}\"";
            await drop.ExecuteNonQueryAsync();
        }
        catch
        {
            // Best-effort cleanup, matching test/Zonkey.Tests.
        }
    }
}
