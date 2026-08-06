using Microsoft.Data.SqlClient;
using Xunit;
using Zonkey.Scaffold.Schema;
using Zonkey.Scaffold.Tests.Infrastructure;

public class SqlServerSchemaReaderTests(MssqlScaffoldFixture db) : IClassFixture<MssqlScaffoldFixture>
{
    private async Task<DatabaseSchema> Read(params string[] schemas)
        => await new SqlServerSchemaReader(db.ConnectionString)
            .Read(schemas.Length == 0 ? ["dbo"] : schemas, CancellationToken.None);

    [Fact]
    public async Task Excludes_system_schemas_and_fixed_roles()
    {
        if (!db.IsAvailable) Assert.Skip(db.SkipReason);

        IReadOnlyList<string> schemas = await new SqlServerSchemaReader(db.ConnectionString)
            .GetNonSystemSchemas(CancellationToken.None);

        Assert.Contains("dbo", schemas);
        Assert.Contains("archive", schemas);
        Assert.DoesNotContain("sys", schemas);
        Assert.DoesNotContain("guest", schemas);
        Assert.DoesNotContain(schemas, s => s.StartsWith("db_", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task Reads_identity_columns()
    {
        if (!db.IsAvailable) Assert.Skip(db.SkipReason);
        Assert.True((await Read()).Tables.Single(t => t.Name == "Species")
                                  .Columns.Single(c => c.Name == "SpeciesId").IsIdentity);
    }

    [Fact]
    public async Task Reads_rowversion_flag()
    {
        if (!db.IsAvailable) Assert.Skip(db.SkipReason);
        Assert.True((await Read()).Tables.Single(t => t.Name == "Animal")
                                  .Columns.Single(c => c.Name == "Version").IsRowVersion);
    }

    [Fact]
    public async Task Max_length_columns_report_null_not_negative_one()
    {
        if (!db.IsAvailable) Assert.Skip(db.SkipReason);
        var notes = (await Read()).Tables.Single(t => t.Name == "Animal")
                                  .Columns.Single(c => c.Name == "Notes");
        Assert.Null(notes.MaxLength);
    }

    [Fact]
    public async Task Nvarchar_length_is_in_characters_not_bytes()
    {
        if (!db.IsAvailable) Assert.Skip(db.SkipReason);
        var name = (await Read()).Tables.Single(t => t.Name == "Species")
                                 .Columns.Single(c => c.Name == "Name");
        Assert.Equal(100, name.MaxLength);   // sys.columns reports 200 bytes for nvarchar(100)
    }

    [Fact]
    public async Task Reads_composite_primary_key_in_order()
    {
        if (!db.IsAvailable) Assert.Skip(db.SkipReason);
        Assert.Equal(["AnimalId", "DayOfWeek", "TimeSlot"],
            (await Read()).Tables.Single(t => t.Name == "FeedingSchedule").PrimaryKey);
    }

    [Fact]
    public async Task Reads_foreign_keys_and_views()
    {
        if (!db.IsAvailable) Assert.Skip(db.SkipReason);
        DatabaseSchema schema = await Read();

        Assert.Equal("Species",
            schema.Tables.Single(t => t.Name == "Animal").ForeignKeys.Single().ReferencedTable);
        Assert.Contains(schema.Tables, t => t.Name == "AnimalNames" && t.Kind == TableKind.View);
    }

    [Fact]
    public async Task Reads_views_only_in_scope()
    {
        if (!db.IsAvailable) Assert.Skip(db.SkipReason);
        DatabaseSchema schema = await Read();
        Assert.DoesNotContain(schema.Tables, t => t.Schema == "archive");
    }

    [Fact]
    public async Task Reads_both_schemas_when_asked()
    {
        if (!db.IsAvailable) Assert.Skip(db.SkipReason);
        DatabaseSchema schema = await Read("dbo", "archive");
        Assert.Equal(2, schema.Tables.Count(t => t.Name == "Animal"));
    }

    [Fact]
    public async Task Reads_length_precision_and_scale()
    {
        if (!db.IsAvailable) Assert.Skip(db.SkipReason);
        var animal = (await Read()).Tables.Single(t => t.Name == "Animal");

        Assert.Equal(8, animal.Columns.Single(c => c.Name == "WeightKg").Precision);
        Assert.Equal(2, animal.Columns.Single(c => c.Name == "WeightKg").Scale);
    }

    [Fact]
    public async Task Varbinary_max_reports_null_length()
    {
        if (!db.IsAvailable) Assert.Skip(db.SkipReason);
        var photo = (await Read()).Tables.Single(t => t.Name == "Animal")
                                  .Columns.Single(c => c.Name == "Photo");
        Assert.Null(photo.MaxLength);
    }

    [Fact]
    public async Task Reads_unique_constraints()
    {
        if (!db.IsAvailable) Assert.Skip(db.SkipReason);
        var uq = (await Read()).Tables.Single(t => t.Name == "Species").UniqueConstraints;
        Assert.Contains(uq, u => u.Columns.SequenceEqual(new[] { "Name" }));
    }

    [Fact]
    public async Task Ordering_is_deterministic()
    {
        if (!db.IsAvailable) Assert.Skip(db.SkipReason);
        DatabaseSchema a = await Read();
        DatabaseSchema b = await Read();

        Assert.Equal(a.Tables.Select(t => t.QualifiedName), b.Tables.Select(t => t.QualifiedName));
    }

    // rowversion's true type name (per sys.types, confirmed against a live SQL Server 2022
    // container) is "timestamp" -- the two are the same underlying type, and a table cannot have
    // more than one. That is what the mapper's dual "rowversion" or "timestamp" arm is defending
    // against being wrong about at the mapping layer; here the reader is asserted directly.
    [Fact]
    public async Task Rowversion_column_reports_timestamp_as_its_native_type()
    {
        if (!db.IsAvailable) Assert.Skip(db.SkipReason);
        var version = (await Read()).Tables.Single(t => t.Name == "Animal")
                                    .Columns.Single(c => c.Name == "Version");
        Assert.Equal("timestamp", version.NativeType);
    }

    // TEXT/NTEXT/IMAGE (the pre-(MAX) legacy LOB types) report max_length = 16 -- the size of the
    // in-row LOB pointer stub, not the data -- rather than -1 the way varchar(max)/nvarchar(max)/
    // varbinary(max) do. Before the fix this reader class received (see the comment on
    // SqlServerSchemaReader.ReadColumns), that 16 flowed straight through as a real MaxLength, and
    // NTEXT additionally got halved by the Unicode-length correction into an equally bogus 8 --
    // both values would have reached the emitted [DataField] as "Length = 16" / "Length = 8" and
    // from there sized the ADO.NET parameter too small, truncating or throwing on any write longer
    // than that stub value. Created and dropped inline rather than added to the shared seed,
    // matching the precedent set by PostgreSqlSchemaReaderTests.
    // Detects_quoted_and_schema_qualified_sequence_defaults and
    // MySqlSchemaReaderTests.Detects_unsigned_integer_columns_...: this scenario doesn't need to be
    // part of the shared fixture to be tested honestly end-to-end against a live server.
    [Fact]
    public async Task Legacy_lob_types_report_null_length_not_their_pointer_stub_size()
    {
        if (!db.IsAvailable) Assert.Skip(db.SkipReason);

        await using var setup = new SqlConnection(db.ConnectionString);
        await setup.OpenAsync();

        await using (var create = setup.CreateCommand())
        {
            create.CommandText = """
                CREATE TABLE zzz_lob_variants (
                    plain_text  TEXT NULL,
                    wide_text   NTEXT NULL,
                    blob        IMAGE NULL
                );
                """;
            await create.ExecuteNonQueryAsync();
        }

        try
        {
            var columns = (await Read()).Tables.Single(t => t.Name == "zzz_lob_variants").Columns;
            ColumnInfo Col(string name) => columns.Single(c => c.Name == name);

            // Before the fix: plain_text.MaxLength == 16, wide_text.MaxLength == 8 (16 halved),
            // blob.MaxLength == 16 -- all bogus stub-derived values, none of them null.
            Assert.Null(Col("plain_text").MaxLength);
            Assert.Null(Col("wide_text").MaxLength);
            Assert.Null(Col("blob").MaxLength);
        }
        finally
        {
            await using var cleanup = setup.CreateCommand();
            cleanup.CommandText = "DROP TABLE IF EXISTS zzz_lob_variants";
            await cleanup.ExecuteNonQueryAsync();
        }
    }
}
