using Microsoft.Data.Sqlite;
using Xunit;
using Zonkey.Scaffold.Config;
using Zonkey.Scaffold.Diagnostics;
using Zonkey.Scaffold.Emit;
using Zonkey.Scaffold.Io;
using Zonkey.Scaffold.Options;
using Zonkey.Scaffold.Pipeline;
using Zonkey.Scaffold.Reporting;

public class ScaffoldPipelineTests : IAsyncLifetime
{
    private string _dbPath = "";
    private string _conn = "";
    private string _workDir = "";

    public async ValueTask InitializeAsync()
    {
        _dbPath = Path.Combine(Path.GetTempPath(), $"zpipe-{Guid.NewGuid():N}.db");
        _conn = $"Data Source={_dbPath}";
        _workDir = Path.GetTempPath();

        await using var cnxn = new SqliteConnection(_conn);
        await cnxn.OpenAsync();
        await using var cmd = cnxn.CreateCommand();
        cmd.CommandText = """
            CREATE TABLE Species (
                SpeciesId INTEGER PRIMARY KEY AUTOINCREMENT,
                Name TEXT NOT NULL,
                Classification TEXT
            );
            CREATE TABLE animals (
                AnimalId INTEGER PRIMARY KEY AUTOINCREMENT,
                Name TEXT NOT NULL,
                SpeciesId INTEGER NOT NULL,
                TenantId TEXT
            );
            CREATE TABLE audit_log (Message TEXT NOT NULL);
            """;
        await cmd.ExecuteNonQueryAsync();
    }

    public ValueTask DisposeAsync()
    {
        SqliteConnection.ClearAllPools();
        if (File.Exists(_dbPath)) File.Delete(_dbPath);
        return ValueTask.CompletedTask;
    }

    private ScaffoldOptions Options() => new()
    {
        Provider = "sqlite",
        ConnectionString = _conn,
        Namespace = "Zoo.Data",
        Wrapper = new WrapperOptions { ClassName = "ZooDatabase", ConnectionName = "Zoo" }
    };

    /// <summary>
    /// Builds against a throwaway working directory, because the plan now carries the resolved
    /// output paths: the pipeline has to answer "would these two files be the same file?", which is
    /// a question about paths and cannot be answered from the option strings.
    /// </summary>
    private Task<ScaffoldPlan> Build(ScaffoldOptions? o = null)
    {
        ScaffoldOptions options = o ?? Options();

        return ScaffoldPipeline.Build(
            options, new ProjectCapabilities(),
            OutputLayout.Resolve(options, _workDir), CancellationToken.None);
    }

    [Fact]
    public async Task Produces_one_entity_per_table()
    {
        var plan = await Build();
        Assert.Contains(plan.Entities, e => e.ClassName == "Species");
        Assert.Contains(plan.Entities, e => e.ClassName == "Animal");   // animals -> Animal
    }

    [Fact]
    public async Task Species_is_not_mangled_to_Specie()
    {
        var plan = await Build();
        Assert.Contains(plan.Entities, e => e.ClassName == "Species");
        Assert.DoesNotContain(plan.Entities, e => e.ClassName == "Specie");
    }

    [Fact]
    public async Task Key_and_identity_flow_into_the_entity_model()
    {
        var plan = await Build();
        var id = plan.Entities.Single(e => e.ClassName == "Species")
                              .Properties.Single(p => p.ColumnName == "SpeciesId");
        Assert.True(id.IsKey);
        Assert.True(id.IsIdentity);
    }

    [Fact]
    public async Task Table_without_primary_key_is_read_only_and_warns()
    {
        var plan = await Build();
        var audit = plan.Entities.Single(e => e.TableName == "audit_log");

        Assert.True(audit.IsReadOnly);
        Assert.Contains(plan.Warnings, w => w.Code == WarningCode.NoPrimaryKey);
    }

    [Fact]
    public async Task Wrapper_lists_every_savable_entity()
    {
        var plan = await Build();
        Assert.Contains(plan.Wrapper.Entries, e => e.EntityClassName == "Animal");
        Assert.Equal("ZooDatabase", plan.Wrapper.ClassName);
    }

    [Fact]
    public async Task Ignored_columns_are_absent_and_attributed()
    {
        var o = Options();
        o.Ignore.Columns.Add("*.TenantId");

        var plan = await Build(o);
        var animal = plan.Entities.Single(e => e.ClassName == "Animal");

        Assert.DoesNotContain(animal.Properties, p => p.ColumnName == "TenantId");
        Assert.Contains(plan.Skipped, s => s.Column == "TenantId" && s.Pattern == "*.TenantId");
    }

    [Fact]
    public async Task Every_column_yields_a_decision_with_a_reason()
    {
        var plan = await Build();
        var d = plan.Decisions.Single(x => x.Table == "main.Species" && x.Column == "Name");

        Assert.Equal("String", d.ProposedDbType);
        Assert.Equal("Name", d.ProposedProperty);
        Assert.False(string.IsNullOrWhiteSpace(d.Reason));
    }

    /// <summary>
    /// The needle is the *JSON-escaped* path, and the payload is deliberately seeded with a warning
    /// that carries the connection string verbatim. Both halves are load-bearing: a raw Windows
    /// path can never appear in JSON (every <c>\</c> is written as <c>\\</c>), so asserting on
    /// <c>_dbPath</c> itself cannot fail on this platform whatever redaction does; and no field of
    /// <see cref="ScaffoldPlan"/> carries a connection string today, so without the seeded warning
    /// there would be nothing for <c>Redact</c> to remove and the assertion would pass with
    /// redaction deleted outright.
    /// </summary>
    [Fact]
    public async Task Json_output_is_camel_cased_and_redacts_the_connection_string()
    {
        var plan = await Build();
        plan.Warnings.Add(ScaffoldWarning.For("PROBE", $"driver said: {_conn}"));

        string json = JsonOutput.Serialize(plan, _conn);

        Assert.Contains("\"entities\"", json);
        Assert.DoesNotContain(JsonEscaped(_dbPath), json);
        Assert.Contains(SecretRedactor.Marker, json);
    }

    /// <summary>
    /// A filesystem path as it appears once <see cref="System.Text.Json"/> has written it — every
    /// backslash doubled. Asserting the raw path against JSON is vacuous on Windows.
    /// </summary>
    private static string JsonEscaped(string value)
        => System.Text.Json.JsonSerializer.Serialize(value).Trim('"');

    /// <summary>
    /// The same trap one level up, in production code: <c>SecretRedactor</c> replaces ordinally
    /// and JSON doubles every backslash, so redacting a rendered payload against the *typed*
    /// connection string found nothing whenever it contained a path or a named instance — which on
    /// Windows is nearly always.
    /// </summary>
    [Fact]
    public void Json_redaction_finds_a_connection_string_that_json_escaped()
    {
        const string conn = @"Server=.\SQLEXPRESS;Database=Zoo;Password=hunter2";

        string json = JsonOutput.Serialize(new { note = $"driver said: {conn}" }, conn);

        Assert.DoesNotContain("SQLEXPRESS", json);
        Assert.DoesNotContain("hunter2", json);
        Assert.Contains(SecretRedactor.Marker, json);
    }

    [Fact]
    public async Task Console_renderer_writes_something_for_every_table()
    {
        var plan = await Build();
        var sw = new StringWriter();
        ConsoleRenderer.RenderInspect(plan, sw);

        string text = sw.ToString();
        Assert.Contains("Species", text);
        Assert.Contains("audit_log", text);
    }

    /// <summary>
    /// The resolved value is written back onto <see cref="ScaffoldOptions.ConnectionString"/>
    /// deliberately: both <c>--json</c> paths redact by passing that property to
    /// <c>JsonOutput.Serialize</c> *after* Build has run, so resolving into a local would have
    /// left a connection string supplied through the named map unredacted in agent transcripts.
    /// </summary>
    [Fact]
    public async Task Named_connection_string_is_resolved_and_written_back()
    {
        var o = Options();
        o.ConnectionString = null;
        o.ConnectionStrings["zonkey"] = _conn;

        ScaffoldPlan plan = await Build(o);

        Assert.NotEmpty(plan.Entities);
        Assert.Equal(_conn, o.ConnectionString);
    }

    [Fact]
    public async Task A_named_connection_string_under_another_key_is_not_used()
    {
        var o = Options();
        o.ConnectionString = null;
        o.ConnectionStrings["Reporting"] = _conn;

        var ex = await Assert.ThrowsAsync<ScaffoldException>(() => Build(o));
        Assert.Contains("ConnectionStrings:Zonkey", ex.Message);
    }

    [Fact]
    public async Task Unknown_provider_is_a_clear_error()
    {
        var o = Options();
        o.Provider = "oracle";
        var ex = await Assert.ThrowsAsync<ScaffoldException>(() => Build(o));
        Assert.Contains("sqlite", ex.Message);   // names what IS supported
    }

    private async Task AddTables(string ddl)
    {
        await using var cnxn = new SqliteConnection(_conn);
        await cnxn.OpenAsync();
        await using var cmd = cnxn.CreateCommand();
        cmd.CommandText = ddl;
        await cmd.ExecuteNonQueryAsync();
    }

    [Fact]
    public async Task Colliding_class_names_throw_before_generating_a_plan()
    {
        // "order" and "orders" both singularize/pascal-case to "Order".
        await AddTables("""
            CREATE TABLE "order" (OrderId INTEGER PRIMARY KEY);
            CREATE TABLE orders (OrderId INTEGER PRIMARY KEY);
            """);

        var ex = await Assert.ThrowsAsync<ScaffoldException>(() => Build());

        Assert.Contains("main.order", ex.Message);
        Assert.Contains("main.orders", ex.Message);
        Assert.Contains("Order", ex.Message);
        Assert.Contains("overrides.tables", ex.Message);

        // Deliberately no longer suggests --schema-disambiguation: this release refuses that
        // option outright (OptionValidator), so naming it as a remedy would send the caller — an
        // agent, most likely — straight into a second, different refusal.
        Assert.Contains("--wrapper-class", ex.Message);
        Assert.DoesNotContain("--schema-disambiguation", ex.Message);
    }

    /// <summary>
    /// The wrapper is written into the same namespace as the entities (and, by default, the same
    /// directory), so a table whose class name equals the wrapper's is a collision in exactly the
    /// sense the check already understood — it was simply never compared. Left unchecked, the
    /// entity file was created and then overwritten by the wrapper, exit 0, no warnings.
    /// </summary>
    [Fact]
    public async Task Entity_colliding_with_the_wrapper_class_name_throws()
    {
        var o = Options();
        o.Wrapper.ClassName = "AppDatabase";

        await AddTables("""CREATE TABLE app_databases (Id INTEGER PRIMARY KEY);""");

        var ex = await Assert.ThrowsAsync<ScaffoldException>(() => Build(o));

        Assert.Contains("main.app_databases", ex.Message);
        Assert.Contains("AppDatabase", ex.Message);
        Assert.Contains("DatabaseWrapper class name", ex.Message);
        Assert.Contains("--wrapper-class", ex.Message);
    }

    [Fact]
    public async Task Wrapper_class_name_that_matches_nothing_is_fine()
    {
        var o = Options();
        o.Wrapper.ClassName = "AppDatabase";

        await AddTables("""CREATE TABLE widgets (Id INTEGER PRIMARY KEY);""");

        ScaffoldPlan plan = await Build(o);
        Assert.Contains(plan.Entities, e => e.ClassName == "Widget");
    }

    // ---- identifiers that do not travel through NamingEngine ---------------------------
    //
    // Fix 5 centralized keyword escaping in the naming layer, but the wrapper's class name, the
    // namespace, and the wrapper's own adapter property names are all identifiers that reach an
    // emitter without passing through ClassNameFor/PropertyNameFor.

    [Fact]
    public async Task Wrapper_class_name_that_is_a_keyword_is_escaped()
    {
        var o = Options();
        o.Wrapper.ClassName = "lock";

        ScaffoldPlan plan = await Build(o);
        Assert.Equal("@lock", plan.Wrapper.ClassName);
    }

    /// <summary>
    /// The collision check must compare the same value the wrapper is finally written with.
    /// Comparing the raw <c>--wrapper-class</c> against escaped entity names would silently
    /// reopen the hole it was added to close: 'lock' never equals '@lock'.
    /// </summary>
    [Fact]
    public async Task Wrapper_collision_is_detected_against_the_escaped_name()
    {
        var o = Options();
        o.Wrapper.ClassName = "lock";
        o.Naming.Style = "preserve";
        o.Naming.Singularize = false;

        await AddTables("""CREATE TABLE "lock" (Id INTEGER PRIMARY KEY);""");

        var ex = await Assert.ThrowsAsync<ScaffoldException>(() => Build(o));
        Assert.Contains("main.lock", ex.Message);
        Assert.Contains("DatabaseWrapper class name", ex.Message);
    }

    [Fact]
    public async Task Wrapper_class_name_that_is_not_an_identifier_is_refused()
    {
        var o = Options();
        o.Wrapper.ClassName = "App Database";

        var ex = await Assert.ThrowsAsync<ScaffoldException>(() => Build(o));
        Assert.Contains("--wrapper-class", ex.Message);
    }

    [Fact]
    public async Task Namespace_segments_are_escaped()
    {
        var o = Options();
        o.Namespace = "Zoo.lock.Data";

        ScaffoldPlan plan = await Build(o);
        Assert.Equal("Zoo.@lock.Data", plan.Namespace);
        Assert.Equal("Zoo.@lock.Data", plan.Wrapper.Namespace);
    }

    /// <summary>
    /// The wrapper's property name is the pluralized class name, which is a different string from
    /// the class name and so can be a keyword when the class name is not: preserve mode over a
    /// table named 'param' yields class 'param' and property 'params'.
    /// </summary>
    [Fact]
    public async Task Wrapper_property_name_that_pluralizes_into_a_keyword_is_escaped()
    {
        var o = Options();
        o.Naming.Style = "preserve";
        o.Naming.Singularize = false;

        await AddTables("""CREATE TABLE param (Id INTEGER PRIMARY KEY);""");

        ScaffoldPlan plan = await Build(o);
        Assert.Contains(plan.Wrapper.Entries, e => e.PropertyName == "@params");
    }

    // ---- overrides.tables.<t>.columns.<c>.dbType ---------------------------------------

    [Fact]
    public async Task Column_dbType_override_is_honoured()
    {
        var o = Options();
        o.Overrides.Tables["Species"] = new TableOverride
        {
            Columns = { ["Name"] = new ColumnOverride { DbType = "AnsiString" } }
        };

        ScaffoldPlan plan = await Build(o);

        Assert.Equal("AnsiString", plan.Entities.Single(e => e.ClassName == "Species")
            .Properties.Single(p => p.ColumnName == "Name").DbType);

        // `inspect` must report what `generate` will emit, or the preview lies.
        Assert.Equal("AnsiString", plan.Decisions
            .Single(d => d.Table == "main.Species" && d.Column == "Name").ProposedDbType);
    }

    /// <summary>
    /// The mapper warns before the pipeline applies the override, so a caller who did exactly what
    /// the warning told them to still saw the same warning on the next run — the same
    /// advice-that-does-nothing problem the warning text was just fixed for, one layer along, and
    /// a good way to make an agent loop. The warning is not deleted (the declared type really is
    /// unrecognized, and the CLR property type really is still the fallback), it is downgraded to
    /// info and reworded to say what is now true.
    /// </summary>
    [Fact]
    public async Task A_dbType_override_answers_the_unmappable_type_warning()
    {
        await AddTables("""CREATE TABLE gadgets (Id INTEGER PRIMARY KEY, Shape GEOGRAPHY);""");

        var o = Options();
        o.Overrides.Tables["gadgets"] = new TableOverride
        {
            Columns = { ["Shape"] = new ColumnOverride { DbType = "AnsiString" } }
        };

        ScaffoldPlan plan = await Build(o);
        ScaffoldWarning w = plan.Warnings.Single(x => x.Column == "Shape");

        Assert.Equal(WarningCode.UnmappableType, w.Code);
        Assert.Equal(WarningLevel.Info, w.Level);
        Assert.Contains("dbType", w.Message);
        Assert.Contains("string", w.Message);   // the CLR type is still the fallback
    }

    [Fact]
    public async Task An_unanswered_unmappable_type_still_warns()
    {
        await AddTables("""CREATE TABLE gizmos (Id INTEGER PRIMARY KEY, Shape GEOGRAPHY);""");

        ScaffoldWarning w = (await Build()).Warnings.Single(x => x.Column == "Shape");

        Assert.Equal(WarningCode.UnmappableType, w.Code);
        Assert.Equal(WarningLevel.Warning, w.Level);
    }

    [Fact]
    public async Task Column_dbType_override_that_is_not_a_DbType_is_refused()
    {
        var o = Options();
        o.Overrides.Tables["Species"] = new TableOverride
        {
            Columns = { ["Name"] = new ColumnOverride { DbType = "Str1ng" } }
        };

        var ex = await Assert.ThrowsAsync<ScaffoldException>(() => Build(o));
        Assert.Contains("Str1ng", ex.Message);
        Assert.Contains("Name", ex.Message);
        Assert.Contains("System.Data.DbType", ex.Message);
    }

    [Fact]
    public async Task Three_way_collision_is_reported_in_one_message()
    {
        var o = Options();
        o.Overrides.Tables["Species"] = new TableOverride { ClassName = "Critter" };

        await AddTables("""
            CREATE TABLE critter_a (Id INTEGER PRIMARY KEY);
            CREATE TABLE critter_b (Id INTEGER PRIMARY KEY);
            """);
        o.Overrides.Tables["critter_a"] = new TableOverride { ClassName = "Critter" };
        o.Overrides.Tables["critter_b"] = new TableOverride { ClassName = "Critter" };

        var ex = await Assert.ThrowsAsync<ScaffoldException>(() => Build(o));

        Assert.Contains("main.Species", ex.Message);
        Assert.Contains("main.critter_a", ex.Message);
        Assert.Contains("main.critter_b", ex.Message);
        Assert.Contains("Critter", ex.Message);
        Assert.Contains("overrides.tables", ex.Message);
    }

    [Fact]
    public async Task Independent_collisions_are_all_reported_together()
    {
        var o = Options();

        // Collision group 1: natural inflection ("order" / "orders" -> "Order").
        // Collision group 2: forced via overrides onto the same class name.
        await AddTables("""
            CREATE TABLE "order" (OrderId INTEGER PRIMARY KEY);
            CREATE TABLE orders (OrderId INTEGER PRIMARY KEY);
            CREATE TABLE cust1 (Id INTEGER PRIMARY KEY);
            CREATE TABLE cust2 (Id INTEGER PRIMARY KEY);
            """);
        o.Overrides.Tables["cust1"] = new TableOverride { ClassName = "Customer" };
        o.Overrides.Tables["cust2"] = new TableOverride { ClassName = "Customer" };

        var ex = await Assert.ThrowsAsync<ScaffoldException>(() => Build(o));

        Assert.Contains("main.order", ex.Message);
        Assert.Contains("main.orders", ex.Message);
        Assert.Contains("Order", ex.Message);
        Assert.Contains("main.cust1", ex.Message);
        Assert.Contains("main.cust2", ex.Message);
        Assert.Contains("Customer", ex.Message);
    }

    /// <summary>
    /// <c>Enum.TryParse</c> returns true for any numeric string, defined member or not, and for a
    /// comma-separated list even on a non-<c>[Flags]</c> enum. Both slipped straight through the
    /// eager validation and reached the emitted source as <c>DbType.99</c>, which fails to compile
    /// in the caller's project — the exact outcome that validation exists to prevent.
    /// </summary>
    [Theory]
    [InlineData("99")]
    [InlineData("-1")]
    [InlineData("String,Int32")]
    public async Task Column_dbType_override_that_is_not_a_named_member_is_refused(string dbType)
    {
        var o = Options();
        o.Overrides.Tables["Species"] = new TableOverride
        {
            Columns = { ["Name"] = new ColumnOverride { DbType = dbType } }
        };

        var ex = await Assert.ThrowsAsync<ScaffoldException>(() => Build(o));
        Assert.Contains(dbType, ex.Message);
        Assert.Contains("Name", ex.Message);
        Assert.Contains("System.Data.DbType", ex.Message);
    }

    [Fact]
    public async Task Unknown_naming_style_is_refused_rather_than_silently_pascal_cased()
    {
        var o = Options();
        o.Naming.Style = "camel";

        var ex = await Assert.ThrowsAsync<ScaffoldException>(() => Build(o));
        Assert.Contains("camel", ex.Message);
        Assert.Contains("--naming-style", ex.Message);
        Assert.Contains("preserve", ex.Message);
    }

    // ---- identifiers that must be unique inside one compilation unit -------------------
    //
    // DetectCollisions checked entity class names and the wrapper class name. Every other
    // generated identifier that shares a declaration space with a sibling was unchecked, and each
    // one produces source that does not compile at exit 0 with no warning.

    /// <summary>
    /// Two distinct class names can pluralize to one string, so the wrapper declares the same
    /// property twice (CS0102). Under the default <c>--singularize true</c> the two tables usually
    /// collapse to one class name and the class-name check catches it, which is why this stayed
    /// hidden.
    /// </summary>
    [Fact]
    public async Task Two_class_names_that_pluralize_to_one_wrapper_property_are_refused()
    {
        var o = Options();
        o.Naming.Singularize = false;

        await AddTables("""
            CREATE TABLE status (Id INTEGER PRIMARY KEY);
            CREATE TABLE statuses (Id INTEGER PRIMARY KEY);
            """);

        var ex = await Assert.ThrowsAsync<ScaffoldException>(() => Build(o));

        Assert.Contains("main.status", ex.Message);
        Assert.Contains("main.statuses", ex.Message);
        Assert.Contains("Statuses", ex.Message);
        Assert.Contains("overrides.tables", ex.Message);
    }

    /// <summary>
    /// Two columns can PascalCase to one property name — <c>first_name</c> and a quoted
    /// <c>"first name"</c> both give <c>FirstName</c> — which is CS0102 inside the entity.
    /// </summary>
    [Fact]
    public async Task Two_columns_that_map_to_one_property_name_are_refused()
    {
        await AddTables("""
            CREATE TABLE people (Id INTEGER PRIMARY KEY, first_name TEXT, "first name" TEXT);
            """);

        var ex = await Assert.ThrowsAsync<ScaffoldException>(() => Build());

        Assert.Contains("main.people", ex.Message);
        Assert.Contains("FirstName", ex.Message);
        Assert.Contains("first_name", ex.Message);
        Assert.Contains("first name", ex.Message);
    }

    /// <summary>
    /// Backing fields are derived by lower-casing the property's first character, so two properties
    /// that differ only there share one field (CS0102 on the field, not the property).
    /// </summary>
    [Fact]
    public async Task Two_properties_that_back_onto_one_field_are_refused()
    {
        await AddTables("""CREATE TABLE gizmos (Id INTEGER PRIMARY KEY, a TEXT, b TEXT);""");

        var o = Options();
        o.Overrides.Tables["gizmos"] = new TableOverride
        {
            Columns =
            {
                ["a"] = new ColumnOverride { Property = "Label" },
                ["b"] = new ColumnOverride { Property = "label" }
            }
        };

        var ex = await Assert.ThrowsAsync<ScaffoldException>(() => Build(o));

        Assert.Contains("_label", ex.Message);
        Assert.Contains("Label", ex.Message);
    }

    /// <summary>
    /// The control for the test above: in <c>field</c>-keyword mode the emitter declares no backing
    /// fields at all, so two properties differing only in leading case are perfectly legal and must
    /// not be refused.
    /// </summary>
    [Fact]
    public async Task Field_keyword_mode_allows_properties_that_would_share_a_backing_field()
    {
        await AddTables("""CREATE TABLE gizmos (Id INTEGER PRIMARY KEY, a TEXT, b TEXT);""");

        var o = Options();
        o.Emit.FieldKeyword = "true";
        o.Overrides.Tables["gizmos"] = new TableOverride
        {
            Columns =
            {
                ["a"] = new ColumnOverride { Property = "Label" },
                ["b"] = new ColumnOverride { Property = "label" }
            }
        };

        ScaffoldPlan plan = await Build(o);
        Assert.Contains(plan.Entities, e => e.ClassName == "Gizmo");
    }

    /// <summary>
    /// A member may not share its enclosing type's name (CS0542), so a column named after its own
    /// table is a collision too — a shape <c>naming.stripClassName</c> deliberately produces.
    /// </summary>
    [Fact]
    public async Task A_property_named_after_its_own_class_is_refused()
    {
        await AddTables("""CREATE TABLE zebra (Id INTEGER PRIMARY KEY, Zebra TEXT);""");

        var ex = await Assert.ThrowsAsync<ScaffoldException>(() => Build());

        Assert.Contains("main.zebra", ex.Message);
        Assert.Contains("Zebra", ex.Message);
    }

    /// <summary>
    /// Two class names that differ only in case are distinct C# types and compile, but they are one
    /// file on Windows and macOS — so the second entity destroys the first, which is exactly the
    /// silent overwrite the class-name check exists to prevent, reached through the file system
    /// instead of through the type system.
    /// </summary>
    [Fact]
    public async Task Class_names_differing_only_in_case_are_refused()
    {
        var o = Options();
        o.Overrides.Tables["Species"] = new TableOverride { ClassName = "Beast" };
        o.Overrides.Tables["animals"] = new TableOverride { ClassName = "beast" };

        var ex = await Assert.ThrowsAsync<ScaffoldException>(() => Build(o));

        Assert.Contains("'Beast'", ex.Message);
        Assert.Contains("'beast'", ex.Message);
        Assert.Contains("case", ex.Message);
    }

    /// <summary>
    /// The wrapper joins that check only when it shares the entities' output directory; given one
    /// of its own, its file cannot compete with theirs.
    /// </summary>
    [Fact]
    public async Task Wrapper_file_name_differing_only_in_case_is_refused_when_it_shares_the_directory()
    {
        var o = Options();
        o.Wrapper.ClassName = "Beast";
        o.Overrides.Tables["Species"] = new TableOverride { ClassName = "beast" };

        var ex = await Assert.ThrowsAsync<ScaffoldException>(() => Build(o));
        Assert.Contains("case", ex.Message);

        o.Output.Wrapper = "./Wrapper";
        ScaffoldPlan plan = await Build(o);
        Assert.Contains(plan.Entities, e => e.ClassName == "beast");
    }

    /// <summary>
    /// A property and *another* property's backing field share one declaration space too. Backing
    /// fields are prefixed with <c>_</c>, so a column that already starts with one collides with
    /// the field of the column that does not: <c>_label</c> and <c>Label</c> are two legal, distinct
    /// SQLite columns, and under <c>--naming-style preserve</c> they need no overrides at all.
    /// </summary>
    [Fact]
    public async Task A_property_and_another_propertys_backing_field_that_share_a_name_are_refused()
    {
        await AddTables(
            """CREATE TABLE widgets (Id INTEGER PRIMARY KEY, _label TEXT, Label TEXT);""");

        var o = Options();
        o.Naming.Style = "preserve";

        var ex = await Assert.ThrowsAsync<ScaffoldException>(() => Build(o));

        Assert.Contains("main.widgets", ex.Message);
        Assert.Contains("_label", ex.Message);
        Assert.Contains("Label", ex.Message);
    }

    /// <summary>
    /// The emitter turns <c>field</c>-keyword mode off <em>per class</em> — any class with a
    /// property named <c>field</c> drops to explicit backing fields, because <c>field</c> binds to
    /// the synthesized backing field inside every accessor in that class (CS9258). A check gated on
    /// the run-wide flag is therefore switched off in exactly the classes that do declare backing
    /// fields, which is the same "decide it somewhere other than where it is decided" defect the
    /// collision enumeration exists to close.
    /// </summary>
    [Fact]
    public async Task Backing_field_collisions_are_refused_in_a_class_that_lost_field_keyword_mode()
    {
        await AddTables(
            """CREATE TABLE gizmos (Id INTEGER PRIMARY KEY, field TEXT, a TEXT, b TEXT);""");

        var o = Options();
        o.Emit.FieldKeyword = "true";
        // preserve, so the column named `field` reaches the emitter as a property named `field` —
        // the shape that costs the class its `field`-keyword mode (PascalCasing hides it as `Field`).
        o.Naming.Style = "preserve";
        o.Overrides.Tables["gizmos"] = new TableOverride
        {
            Columns =
            {
                ["a"] = new ColumnOverride { Property = "Label" },
                ["b"] = new ColumnOverride { Property = "label" }
            }
        };

        var ex = await Assert.ThrowsAsync<ScaffoldException>(() => Build(o));

        Assert.Contains("_label", ex.Message);
        Assert.Contains("Label", ex.Message);
    }

    /// <summary>
    /// Whether the wrapper's file competes with an entity's is a question about resolved paths, not
    /// about whether an option string was left empty: <c>--out-wrapper</c> pointed at the entities'
    /// own directory (in any spelling) puts the two files side by side exactly as the default does.
    /// </summary>
    [Fact]
    public async Task Wrapper_pointed_at_the_entities_directory_still_joins_the_file_name_check()
    {
        var o = Options();
        o.Output.Entities = "Entities";
        o.Output.Wrapper = "./Entities/";          // the same directory, spelled differently
        o.Wrapper.ClassName = "Beast";
        o.Overrides.Tables["Species"] = new TableOverride { ClassName = "beast" };

        var ex = await Assert.ThrowsAsync<ScaffoldException>(() => Build(o));
        Assert.Contains("case", ex.Message);
    }

    /// <summary>
    /// Every kind of collision is gathered into the one message, for the same reason class-name
    /// collisions already were: a caller fixing overrides should see the whole list once.
    /// </summary>
    [Fact]
    public async Task Collisions_of_different_kinds_are_reported_together()
    {
        var o = Options();
        o.Naming.Singularize = false;

        await AddTables("""
            CREATE TABLE status (Id INTEGER PRIMARY KEY);
            CREATE TABLE statuses (Id INTEGER PRIMARY KEY);
            CREATE TABLE people (Id INTEGER PRIMARY KEY, first_name TEXT, "first name" TEXT);
            """);

        var ex = await Assert.ThrowsAsync<ScaffoldException>(() => Build(o));

        Assert.Contains("Statuses", ex.Message);
        Assert.Contains("FirstName", ex.Message);
    }
}
