using MySqlConnector;

namespace Zonkey.Scaffold.Tests.Infrastructure;

public sealed class MysqlScaffoldFixture : IScaffoldFixture
{
    private readonly string _base = TestConfiguration.MysqlConnectionString;
    private readonly string _databaseName = $"zscaffold_{Guid.NewGuid():N}";

    // Tracks "CREATE DATABASE succeeded" independently of IsAvailable, which only flips true at
    // the very end of InitializeAsync. If the seed step throws after the database was created,
    // IsAvailable stays false but the database still exists and must still be dropped.
    private bool _databaseCreated;

    public bool IsAvailable { get; private set; }
    public string SkipReason { get; private set; } = "";
    public string ConnectionString => $"{_base};Database={_databaseName}";
    public string Provider => "mysql";

    // In MySQL a "schema" is a database, so the per-run database created below is itself the
    // one schema a reader needs to be pointed at.
    public string DefaultSchema => _databaseName;

    public async ValueTask InitializeAsync()
    {
        try
        {
            // No default database on the admin connection: CREATE DATABASE needs none, and MySQL
            // has no separate "postgres"/"master" catalog to connect to first.
            await using (var admin = new MySqlConnection(_base))
            {
                await admin.OpenAsync();
                await using var create = admin.CreateCommand();
                create.CommandText = $"CREATE DATABASE `{_databaseName}`";
                await create.ExecuteNonQueryAsync();
            }

            _databaseCreated = true;

            await using var cnxn = new MySqlConnection(ConnectionString);
            await cnxn.OpenAsync();
            await using var seed = cnxn.CreateCommand();
            seed.CommandText = File.ReadAllText(
                Path.Combine(AppContext.BaseDirectory, "Seed", "mysql-scaffold-seed.sql"));
            await seed.ExecuteNonQueryAsync();

            IsAvailable = true;
        }
        catch (Exception ex)
        {
            if (TestConfiguration.RequireDatabase)
                throw new InvalidOperationException(
                    $"MySQL is required (ZONKEY_REQUIRE_DB is set) but setup failed: {ex.Message}", ex);

            IsAvailable = false;
            SkipReason = $"MySQL not available: {ex.Message}. " +
                         "Set ZONKEY_TEST_MYSQL or run 'docker compose up -d --wait'.";
        }
    }

    public async ValueTask DisposeAsync()
    {
        if (!_databaseCreated) return;

        try
        {
            MySqlConnection.ClearAllPools();
            await using var admin = new MySqlConnection(_base);
            await admin.OpenAsync();
            await using var drop = admin.CreateCommand();
            drop.CommandText = $"DROP DATABASE IF EXISTS `{_databaseName}`";
            await drop.ExecuteNonQueryAsync();
        }
        catch
        {
            // Best-effort cleanup, matching test/Zonkey.Tests.
        }
    }
}
