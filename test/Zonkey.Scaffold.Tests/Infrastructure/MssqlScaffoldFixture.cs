using System.Text.RegularExpressions;
using Microsoft.Data.SqlClient;

namespace Zonkey.Scaffold.Tests.Infrastructure;

public sealed partial class MssqlScaffoldFixture : IScaffoldFixture
{
    private readonly string _base = TestConfiguration.MssqlConnectionString;
    private readonly string _databaseName = $"zscaffold_{Guid.NewGuid():N}";

    // Tracks "CREATE DATABASE succeeded" independently of IsAvailable, which only flips true at
    // the very end of InitializeAsync. If the seed step throws after the database was created,
    // IsAvailable stays false but the database still exists and must still be dropped.
    private bool _databaseCreated;

    public bool IsAvailable { get; private set; }
    public string SkipReason { get; private set; } = "";
    public string ConnectionString => $"{_base};Database={_databaseName}";
    public string Provider => "sqlserver";
    public string DefaultSchema => "dbo";

    [GeneratedRegex(@"^\s*GO\s*$", RegexOptions.Multiline | RegexOptions.IgnoreCase)]
    private static partial Regex GoSeparator();

    public async ValueTask InitializeAsync()
    {
        try
        {
            await using (var admin = new SqlConnection($"{_base};Database=master"))
            {
                await admin.OpenAsync();
                await using var create = admin.CreateCommand();
                create.CommandText = $"CREATE DATABASE [{_databaseName}]";
                await create.ExecuteNonQueryAsync();
            }

            _databaseCreated = true;

            // The seed file uses "GO", a sqlcmd batch separator rather than T-SQL, so it must be
            // split into individual batches before executing — ExecuteNonQueryAsync rejects "GO".
            string sql = File.ReadAllText(
                Path.Combine(AppContext.BaseDirectory, "Seed", "mssql-scaffold-seed.sql"));

            await using var cnxn = new SqlConnection(ConnectionString);
            await cnxn.OpenAsync();

            foreach (string batch in GoSeparator().Split(sql))
            {
                string trimmed = batch.Trim();
                if (trimmed.Length == 0) continue;

                await using var cmd = cnxn.CreateCommand();
                cmd.CommandText = trimmed;
                cmd.CommandTimeout = 30;
                await cmd.ExecuteNonQueryAsync();
            }

            IsAvailable = true;
        }
        catch (Exception ex)
        {
            if (TestConfiguration.RequireDatabase)
                throw new InvalidOperationException(
                    $"SQL Server is required (ZONKEY_REQUIRE_DB is set) but setup failed: {ex.Message}", ex);

            IsAvailable = false;
            SkipReason = $"SQL Server not available: {ex.Message}. " +
                         "Set ZONKEY_TEST_MSSQL or run 'docker compose up -d --wait'.";
        }
    }

    public async ValueTask DisposeAsync()
    {
        if (!_databaseCreated) return;

        try
        {
            SqlConnection.ClearAllPools();
            await using var admin = new SqlConnection($"{_base};Database=master");
            await admin.OpenAsync();
            await using var drop = admin.CreateCommand();
            drop.CommandText = $@"
                IF DB_ID('{_databaseName}') IS NOT NULL
                BEGIN
                    ALTER DATABASE [{_databaseName}] SET SINGLE_USER WITH ROLLBACK IMMEDIATE;
                    DROP DATABASE [{_databaseName}];
                END";
            await drop.ExecuteNonQueryAsync();
        }
        catch
        {
            // Best-effort cleanup, matching test/Zonkey.Tests.
        }
    }
}
