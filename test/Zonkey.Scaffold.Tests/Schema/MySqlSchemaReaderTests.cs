using MySqlConnector;
using Xunit;
using Zonkey.Scaffold.Schema;
using Zonkey.Scaffold.Tests.Infrastructure;

public class MySqlSchemaReaderTests(MysqlScaffoldFixture db) : IClassFixture<MysqlScaffoldFixture>
{
    private async Task<DatabaseSchema> Read()
        => await new MySqlSchemaReader(db.ConnectionString)
            .Read([db.DefaultSchema], CancellationToken.None);

    [Fact]
    public async Task Excludes_system_databases()
    {
        if (!db.IsAvailable) Assert.Skip(db.SkipReason);

        IReadOnlyList<string> schemas = await new MySqlSchemaReader(db.ConnectionString)
            .GetNonSystemSchemas(CancellationToken.None);

        Assert.DoesNotContain("mysql", schemas);
        Assert.DoesNotContain("information_schema", schemas);
        Assert.DoesNotContain("performance_schema", schemas);
        Assert.DoesNotContain("sys", schemas);
    }

    [Fact]
    public async Task Reads_auto_increment_as_identity()
    {
        if (!db.IsAvailable) Assert.Skip(db.SkipReason);
        var id = (await Read()).Tables.Single(t => t.Name == "species")
                               .Columns.Single(c => c.Name == "species_id");
        Assert.True(id.IsIdentity);
    }

    [Fact]
    public async Task Reads_composite_primary_key_in_order()
    {
        if (!db.IsAvailable) Assert.Skip(db.SkipReason);
        Assert.Equal(["animal_id", "day_of_week", "time_slot"],
            (await Read()).Tables.Single(t => t.Name == "feeding_schedule").PrimaryKey);
    }

    [Fact]
    public async Task Reads_foreign_keys()
    {
        if (!db.IsAvailable) Assert.Skip(db.SkipReason);
        var fk = (await Read()).Tables.Single(t => t.Name == "animal").ForeignKeys.Single();
        Assert.Equal("species", fk.ReferencedTable);
        Assert.Equal(["species_id"], fk.Columns);
    }

    [Fact]
    public async Task Reads_views()
    {
        if (!db.IsAvailable) Assert.Skip(db.SkipReason);
        Assert.Contains((await Read()).Tables, t => t.Name == "animal_names" && t.Kind == TableKind.View);
    }

    [Fact]
    public async Task Captures_tinyint_display_width()
    {
        if (!db.IsAvailable) Assert.Skip(db.SkipReason);
        var col = (await Read()).Tables.Single(t => t.Name == "species")
                                .Columns.Single(c => c.Name == "is_endangered");
        Assert.Equal(1, col.Precision);   // display width lands in Precision for tinyint
    }

    // ---- unsigned integer types ---------------------------------------------------------------
    // Created and dropped inline rather than added to the shared seed, matching the precedent set
    // by PostgreSqlSchemaReaderTests.Detects_quoted_and_schema_qualified_sequence_defaults: the
    // seed's table/column set is depended on by other Plan 2 tasks, and this scenario doesn't need
    // to be part of that shared fixture to be tested honestly end-to-end against a live server.
    [Fact]
    public async Task Detects_unsigned_integer_columns_and_the_unrecoverable_tinyint_width()
    {
        if (!db.IsAvailable) Assert.Skip(db.SkipReason);

        await using var setup = new MySqlConnection(db.ConnectionString);
        await setup.OpenAsync();

        await using (var create = setup.CreateCommand())
        {
            create.CommandText = """
                CREATE TABLE zzz_unsigned_variants (
                    signed_flag        TINYINT(1) NOT NULL,
                    unsigned_flag_1    TINYINT(1) UNSIGNED NOT NULL,
                    unsigned_flag_bare TINYINT UNSIGNED NOT NULL,
                    unsigned_flag_4    TINYINT(4) UNSIGNED NOT NULL,
                    signed_small       SMALLINT NOT NULL,
                    unsigned_small     SMALLINT UNSIGNED NOT NULL,
                    signed_int         INT NOT NULL,
                    unsigned_int       INT UNSIGNED NOT NULL,
                    unsigned_bigint    BIGINT UNSIGNED NOT NULL
                );
                """;
            await create.ExecuteNonQueryAsync();
        }

        try
        {
            var columns = (await Read()).Tables.Single(t => t.Name == "zzz_unsigned_variants").Columns;
            ColumnInfo Col(string name) => columns.Single(c => c.Name == name);

            // The signed baseline: unaffected by IsUnsigned's introduction.
            Assert.False(Col("signed_flag").IsUnsigned);
            Assert.Equal(1, Col("signed_flag").Precision);

            // TINYINT(1) UNSIGNED, plain TINYINT UNSIGNED, and TINYINT(4) UNSIGNED are all
            // reported identically by MySQL (column_type "tinyint unsigned", no display width) --
            // the width really is unrecoverable for the unsigned form, for any of them.
            Assert.True(Col("unsigned_flag_1").IsUnsigned);
            Assert.Null(Col("unsigned_flag_1").Precision);
            Assert.True(Col("unsigned_flag_bare").IsUnsigned);
            Assert.Null(Col("unsigned_flag_bare").Precision);
            Assert.True(Col("unsigned_flag_4").IsUnsigned);
            Assert.Null(Col("unsigned_flag_4").Precision);

            Assert.False(Col("signed_small").IsUnsigned);
            Assert.True(Col("unsigned_small").IsUnsigned);
            Assert.False(Col("signed_int").IsUnsigned);
            Assert.True(Col("unsigned_int").IsUnsigned);
            Assert.True(Col("unsigned_bigint").IsUnsigned);
        }
        finally
        {
            await using var cleanup = setup.CreateCommand();
            cleanup.CommandText = "DROP TABLE IF EXISTS zzz_unsigned_variants";
            await cleanup.ExecuteNonQueryAsync();
        }
    }

    // Proves the mapper's chosen CLR types (uint/ulong) actually hold live data above the signed
    // maximum, rather than merely compiling: inserts values only representable in the unsigned
    // range, reads them back through the real driver, and asserts both the runtime CLR type and
    // the value survive intact. If MySqlTypeMapper's arms picked int/long instead of uint/ulong for
    // these columns, the equivalent read through a generated entity would throw or truncate.
    [Fact]
    public async Task Unsigned_values_above_the_signed_maximum_round_trip_through_the_driver()
    {
        if (!db.IsAvailable) Assert.Skip(db.SkipReason);

        await using var setup = new MySqlConnection(db.ConnectionString);
        await setup.OpenAsync();

        const uint intValue = 4_000_000_000;      // > int.MaxValue (2,147,483,647)
        const ulong bigintValue = 18_000_000_000_000_000_000; // > long.MaxValue (~9.22e18)

        await using (var create = setup.CreateCommand())
        {
            create.CommandText = """
                CREATE TABLE zzz_unsigned_values (
                    unsigned_int    INT UNSIGNED NOT NULL,
                    unsigned_bigint BIGINT UNSIGNED NOT NULL
                );
                """;
            await create.ExecuteNonQueryAsync();

            await using var insert = setup.CreateCommand();
            insert.CommandText =
                "INSERT INTO zzz_unsigned_values VALUES (@i, @b)";
            insert.Parameters.AddWithValue("@i", intValue);
            insert.Parameters.AddWithValue("@b", bigintValue);
            await insert.ExecuteNonQueryAsync();
        }

        try
        {
            await using var select = setup.CreateCommand();
            select.CommandText = "SELECT unsigned_int, unsigned_bigint FROM zzz_unsigned_values";
            await using MySqlDataReader reader = await select.ExecuteReaderAsync();
            await reader.ReadAsync();

            Assert.Equal(typeof(uint), reader.GetValue(0).GetType());
            Assert.Equal(intValue, reader.GetFieldValue<uint>(0));

            Assert.Equal(typeof(ulong), reader.GetValue(1).GetType());
            Assert.Equal(bigintValue, reader.GetFieldValue<ulong>(1));
        }
        finally
        {
            await using var cleanup = setup.CreateCommand();
            cleanup.CommandText = "DROP TABLE IF EXISTS zzz_unsigned_values";
            await cleanup.ExecuteNonQueryAsync();
        }
    }
}
