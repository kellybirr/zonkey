using Npgsql;
using Xunit;
using Zonkey.Scaffold.Schema;
using Zonkey.Scaffold.Tests.Infrastructure;

public class PostgreSqlSchemaReaderTests(PgsqlScaffoldFixture db) : IClassFixture<PgsqlScaffoldFixture>
{
    private async Task<DatabaseSchema> Read(params string[] schemas)
    {
        var reader = new PostgreSqlSchemaReader(db.ConnectionString);
        return await reader.Read(schemas.Length == 0 ? ["public"] : schemas, CancellationToken.None);
    }

    [Fact]
    public async Task Lists_only_non_system_schemas()
    {
        if (!db.IsAvailable) Assert.Skip(db.SkipReason);

        var reader = new PostgreSqlSchemaReader(db.ConnectionString);
        IReadOnlyList<string> schemas = await reader.GetNonSystemSchemas(CancellationToken.None);

        Assert.Contains("public", schemas);
        Assert.Contains("archive", schemas);
        Assert.DoesNotContain("pg_catalog", schemas);
        Assert.DoesNotContain("information_schema", schemas);
        Assert.DoesNotContain(schemas, s => s.StartsWith("pg_", StringComparison.Ordinal));
    }

    [Fact]
    public async Task Reads_tables_and_views_in_scope_only()
    {
        if (!db.IsAvailable) Assert.Skip(db.SkipReason);
        DatabaseSchema schema = await Read();

        Assert.Contains(schema.Tables, t => t.Name == "species" && t.Kind == TableKind.Table);
        Assert.Contains(schema.Tables, t => t.Name == "animal_names" && t.Kind == TableKind.View);
        Assert.DoesNotContain(schema.Tables, t => t.Schema == "archive");
    }

    [Fact]
    public async Task Reads_both_schemas_when_asked()
    {
        if (!db.IsAvailable) Assert.Skip(db.SkipReason);
        DatabaseSchema schema = await Read("public", "archive");

        Assert.Equal(2, schema.Tables.Count(t => t.Name == "animal"));
    }

    [Fact]
    public async Task Detects_serial_columns_as_identity_with_their_sequence()
    {
        if (!db.IsAvailable) Assert.Skip(db.SkipReason);
        DatabaseSchema schema = await Read();

        ColumnInfo id = schema.Tables.Single(t => t.Name == "species")
                              .Columns.Single(c => c.Name == "species_id");

        Assert.True(id.IsIdentity);
        Assert.Equal("species_species_id_seq", id.SequenceName);
    }

    [Fact]
    public async Task Distinguishes_timestamptz_from_timestamp()
    {
        if (!db.IsAvailable) Assert.Skip(db.SkipReason);
        var cols = (await Read()).Tables.Single(t => t.Name == "zookeeper").Columns;

        Assert.Equal("timestamp with time zone", cols.Single(c => c.Name == "created_utc").NativeType);
        Assert.Equal("timestamp without time zone", cols.Single(c => c.Name == "local_noted_at").NativeType);
    }

    [Fact]
    public async Task Reads_length_precision_and_scale()
    {
        if (!db.IsAvailable) Assert.Skip(db.SkipReason);
        var animal = (await Read()).Tables.Single(t => t.Name == "animal");

        Assert.Equal(100, animal.Columns.Single(c => c.Name == "name").MaxLength);
        Assert.Equal(8, animal.Columns.Single(c => c.Name == "weight_kg").Precision);
        Assert.Equal(2, animal.Columns.Single(c => c.Name == "weight_kg").Scale);
    }

    [Fact]
    public async Task Reads_composite_primary_key_in_order()
    {
        if (!db.IsAvailable) Assert.Skip(db.SkipReason);
        Assert.Equal(["animal_id", "day_of_week", "time_slot"],
            (await Read()).Tables.Single(t => t.Name == "feeding_schedule").PrimaryKey);
    }

    [Fact]
    public async Task Reads_foreign_keys_with_their_targets()
    {
        if (!db.IsAvailable) Assert.Skip(db.SkipReason);
        var fks = (await Read()).Tables.Single(t => t.Name == "animal").ForeignKeys;

        Assert.Equal(2, fks.Count);
        ForeignKeyInfo species = fks.Single(f => f.ReferencedTable == "species");
        Assert.Equal(["species_id"], species.Columns);
        Assert.Equal(["species_id"], species.ReferencedColumns);
    }

    [Fact]
    public async Task Reads_unique_constraints()
    {
        if (!db.IsAvailable) Assert.Skip(db.SkipReason);
        var uq = (await Read()).Tables.Single(t => t.Name == "species").UniqueConstraints;
        Assert.Contains(uq, u => u.Columns.SequenceEqual(new[] { "name" }));
    }

    [Fact]
    public async Task Ordering_is_deterministic()
    {
        if (!db.IsAvailable) Assert.Skip(db.SkipReason);
        DatabaseSchema a = await Read();
        DatabaseSchema b = await Read();

        Assert.Equal(a.Tables.Select(t => t.QualifiedName), b.Tables.Select(t => t.QualifiedName));
    }

    // Regression coverage for a real reader bug: the nextval(...) extraction originally used a
    // character class that excluded '"', so it could not match a doubly-quoted schema-qualified
    // regclass literal such as nextval('"MixedSchema"."MixedSeq"'::regclass) — exactly what
    // Postgres emits for a serial column whose owning schema needs quoting and isn't on the
    // search path. On a miss the column silently fell through to IsIdentity = false. These
    // objects are created and dropped inside the test rather than added to the shared seed, since
    // the seed's exact table/column set is depended on by other Plan 2 tasks (golden output,
    // other providers) and this scenario doesn't need to be part of that shared fixture to be
    // tested honestly end-to-end against a live server.
    [Fact]
    public async Task Detects_quoted_and_schema_qualified_sequence_defaults()
    {
        if (!db.IsAvailable) Assert.Skip(db.SkipReason);

        await using var setup = new NpgsqlConnection(db.ConnectionString);
        await setup.OpenAsync();

        await using (var create = setup.CreateCommand())
        {
            create.CommandText = """
                CREATE SEQUENCE public.zzz_case1_seq;
                CREATE SEQUENCE public.zzz_case2_seq;
                CREATE SEQUENCE public."MixedCase_seq";
                CREATE SCHEMA "MixedSchema";
                CREATE SEQUENCE "MixedSchema"."MixedSeq";

                CREATE TABLE public.zzz_nextval_variants (
                    unqualified_id      bigint DEFAULT nextval('zzz_case1_seq'::regclass),
                    schema_qualified_id bigint DEFAULT nextval('public.zzz_case2_seq'::regclass),
                    quoted_id           bigint DEFAULT nextval('"MixedCase_seq"'::regclass),
                    quoted_schema_id    bigint DEFAULT nextval('"MixedSchema"."MixedSeq"'::regclass),
                    plain_default_at    timestamp DEFAULT now(),
                    plain_default_text  text DEFAULT 'literal'::text
                );
                """;
            await create.ExecuteNonQueryAsync();
        }

        try
        {
            var columns = (await Read()).Tables.Single(t => t.Name == "zzz_nextval_variants").Columns;

            ColumnInfo Col(string name) => columns.Single(c => c.Name == name);

            // nextval('name'::regclass) — bare, no schema (already covered by the species table,
            // repeated here so all five forms are asserted from the same table).
            Assert.True(Col("unqualified_id").IsIdentity);
            Assert.Equal("zzz_case1_seq", Col("unqualified_id").SequenceName);

            // nextval('public.name'::regclass) — schema-qualified, neither part quoted.
            Assert.True(Col("schema_qualified_id").IsIdentity);
            Assert.Equal("zzz_case2_seq", Col("schema_qualified_id").SequenceName);

            // nextval('"Name"'::regclass) — quoted, no schema.
            Assert.True(Col("quoted_id").IsIdentity);
            Assert.Equal("MixedCase_seq", Col("quoted_id").SequenceName);

            // nextval('"Schema"."Name"'::regclass) — the previously-missed case: both parts quoted.
            Assert.True(Col("quoted_schema_id").IsIdentity);
            Assert.Equal("MixedSeq", Col("quoted_schema_id").SequenceName);

            // Negative: an unrelated default must not be mistaken for a sequence default, and the
            // fix must not make the match looser than the real nextval(...)::regclass shape.
            Assert.False(Col("plain_default_at").IsIdentity);
            Assert.Null(Col("plain_default_at").SequenceName);
            Assert.False(Col("plain_default_text").IsIdentity);
            Assert.Null(Col("plain_default_text").SequenceName);
        }
        finally
        {
            await using var cleanup = setup.CreateCommand();
            cleanup.CommandText = """
                DROP TABLE IF EXISTS public.zzz_nextval_variants;
                DROP SEQUENCE IF EXISTS public.zzz_case1_seq;
                DROP SEQUENCE IF EXISTS public.zzz_case2_seq;
                DROP SEQUENCE IF EXISTS public."MixedCase_seq";
                DROP SCHEMA IF EXISTS "MixedSchema" CASCADE;
                """;
            await cleanup.ExecuteNonQueryAsync();
        }
    }
}
