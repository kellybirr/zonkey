using Microsoft.Data.SqlClient;
using MySqlConnector;
using Npgsql;
using Xunit;
using Zonkey.Scaffold.Tests.Infrastructure;

// Each fixture creates and drops its own uniquely named per-run database (see the GUID-suffixed
// names in Pgsql/Mysql/MssqlScaffoldFixture), so cross-test interference between the three
// providers, or between multiple instances of the same provider, isn't possible the way it is
// for the Console-capturing tests in ScaffoldConsole (see the comment on ScaffoldConsoleCollection
// in Cli/CommandTests.cs) -- there is no shared mutable state here to serialize around. These
// tests are deliberately left free to run in parallel with the rest of the assembly.

public class PgsqlFixtureSmokeTests(PgsqlScaffoldFixture db) : IClassFixture<PgsqlScaffoldFixture>
{
    [Fact]
    public async Task Fixture_either_connects_or_explains_why_not()
    {
        var ct = TestContext.Current.CancellationToken;

        if (!db.IsAvailable)
        {
            Assert.False(string.IsNullOrWhiteSpace(db.SkipReason));
            Assert.Skip(db.SkipReason);
        }

        Assert.Equal("postgresql", db.Provider);
        Assert.Equal("public", db.DefaultSchema);
        Assert.False(string.IsNullOrWhiteSpace(db.ConnectionString));

        // Confirm the seed actually applied, not just that CREATE DATABASE succeeded.
        await using var conn = new NpgsqlConnection(db.ConnectionString);
        await conn.OpenAsync(ct);
        await using var cmd = conn.CreateCommand();
        cmd.CommandText =
            "SELECT table_schema, table_name FROM information_schema.tables " +
            "WHERE (table_schema = 'public' AND table_name IN " +
            "  ('species', 'zookeeper', 'animal', 'feeding_schedule', 'animal_names'))" +
            "   OR (table_schema = 'archive' AND table_name = 'animal')";

        var found = new HashSet<string>();
        await using (var reader = await cmd.ExecuteReaderAsync(ct))
        {
            while (await reader.ReadAsync(ct))
                found.Add($"{reader.GetString(0)}.{reader.GetString(1)}");
        }

        Assert.Equal(
            new[]
            {
                "archive.animal", "public.animal", "public.animal_names",
                "public.feeding_schedule", "public.species", "public.zookeeper"
            },
            found.OrderBy(x => x, StringComparer.Ordinal));
    }
}

public class MysqlFixtureSmokeTests(MysqlScaffoldFixture db) : IClassFixture<MysqlScaffoldFixture>
{
    [Fact]
    public async Task Fixture_either_connects_or_explains_why_not()
    {
        var ct = TestContext.Current.CancellationToken;

        if (!db.IsAvailable)
        {
            Assert.False(string.IsNullOrWhiteSpace(db.SkipReason));
            Assert.Skip(db.SkipReason);
        }

        Assert.Equal("mysql", db.Provider);
        Assert.False(string.IsNullOrWhiteSpace(db.DefaultSchema));
        Assert.False(string.IsNullOrWhiteSpace(db.ConnectionString));

        // In MySQL a "schema" is a database, so the fixture's own database is the schema to check.
        await using var conn = new MySqlConnection(db.ConnectionString);
        await conn.OpenAsync(ct);
        await using var cmd = conn.CreateCommand();
        cmd.CommandText =
            "SELECT table_name FROM information_schema.tables " +
            "WHERE table_schema = @schema AND table_name IN " +
            "  ('species', 'animal', 'feeding_schedule', 'animal_names')";
        cmd.Parameters.AddWithValue("schema", db.DefaultSchema);

        var found = new HashSet<string>();
        await using (var reader = await cmd.ExecuteReaderAsync(ct))
        {
            while (await reader.ReadAsync(ct))
                found.Add(reader.GetString(0));
        }

        Assert.Equal(
            new[] { "animal", "animal_names", "feeding_schedule", "species" },
            found.OrderBy(x => x, StringComparer.Ordinal));
    }
}

public class MssqlFixtureSmokeTests(MssqlScaffoldFixture db) : IClassFixture<MssqlScaffoldFixture>
{
    [Fact]
    public async Task Fixture_either_connects_or_explains_why_not()
    {
        var ct = TestContext.Current.CancellationToken;

        if (!db.IsAvailable)
        {
            Assert.False(string.IsNullOrWhiteSpace(db.SkipReason));
            Assert.Skip(db.SkipReason);
        }

        Assert.Equal("sqlserver", db.Provider);
        Assert.Equal("dbo", db.DefaultSchema);
        Assert.False(string.IsNullOrWhiteSpace(db.ConnectionString));

        // Confirm the GO-batched seed actually applied all the way through, including the
        // trailing CREATE SCHEMA/CREATE TABLE batches after the view.
        await using var conn = new SqlConnection(db.ConnectionString);
        await conn.OpenAsync(ct);
        await using var cmd = conn.CreateCommand();
        cmd.CommandText =
            "SELECT table_schema, table_name FROM information_schema.tables " +
            "WHERE (table_schema = 'dbo' AND table_name IN " +
            "  ('Species', 'Animal', 'FeedingSchedule', 'AnimalNames'))" +
            "   OR (table_schema = 'archive' AND table_name = 'Animal')";

        var found = new HashSet<string>();
        await using (var reader = await cmd.ExecuteReaderAsync(ct))
        {
            while (await reader.ReadAsync(ct))
                found.Add($"{reader.GetString(0)}.{reader.GetString(1)}");
        }

        Assert.Equal(
            new[] { "archive.Animal", "dbo.Animal", "dbo.AnimalNames", "dbo.FeedingSchedule", "dbo.Species" },
            found.OrderBy(x => x, StringComparer.Ordinal));
    }
}
