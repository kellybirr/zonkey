using System.Text.Json;
using Microsoft.Data.Sqlite;
using Xunit;
using Zonkey.Scaffold;
using Zonkey.Scaffold.Commands;
using Zonkey.Scaffold.Config;
using Zonkey.Scaffold.Options;

/// <summary>
/// Groups every test class that touches the real, process-wide <see cref="Console.Out"/> /
/// <see cref="Console.Error"/> — directly (<see cref="CommandTests"/>'s
/// <c>Console.SetOut</c>/<c>SetError</c> captures) or indirectly (<see cref="CliSmokeTests"/>
/// drives <c>Program.Main</c>, whose <c>--help</c> and parse-error paths write straight to
/// <c>Console.Out</c>/<c>Console.Error</c> rather than an injected <see cref="TextWriter"/>).
/// <c>DisableParallelization</c> only serializes tests *within* this collection — it grants no
/// exclusivity against the ~260 other tests in the assembly, which is exactly the point: every
/// other class in this project writes through an injected <see cref="TextWriter"/> instead of
/// touching the Console statics, so they cannot corrupt a capture here and are free to keep
/// running in parallel with each other and with this collection.
/// </summary>
[CollectionDefinition("ScaffoldConsole", DisableParallelization = true)]
public class ScaffoldConsoleCollection;

[Collection("ScaffoldConsole")]
public class CommandTests : IAsyncLifetime
{
    private string _dir = "";
    private string _dbPath = "";
    private string _conn = "";

    public async ValueTask InitializeAsync()
    {
        _dir = Directory.CreateTempSubdirectory("zcli").FullName;
        _dbPath = Path.Combine(_dir, "zoo.db");
        _conn = $"Data Source={_dbPath}";

        await using var cnxn = new SqliteConnection(_conn);
        await cnxn.OpenAsync();
        await using var cmd = cnxn.CreateCommand();
        cmd.CommandText = """
            CREATE TABLE Species (
                SpeciesId INTEGER PRIMARY KEY AUTOINCREMENT,
                Name TEXT NOT NULL
            );
            """;
        await cmd.ExecuteNonQueryAsync();
    }

    public ValueTask DisposeAsync()
    {
        SqliteConnection.ClearAllPools();
        Directory.Delete(_dir, recursive: true);
        return ValueTask.CompletedTask;
    }

    private string[] BaseArgs(string verb) =>
    [
        verb,
        "--provider", "sqlite",
        "--connection", _conn,
        "--namespace", "Zoo.Data",
        "--out", Path.Combine(_dir, "Entities"),
        "--config-file", Path.Combine(_dir, "zonkey.scaffold.config.json")
    ];

    private string ConfigPath => Path.Combine(_dir, "zonkey.scaffold.config.json");

    /// <summary>Same as <see cref="BaseArgs"/> but without <c>--connection</c>.</summary>
    private string[] NoConnectionArgs(string verb) =>
    [
        verb,
        "--provider", "sqlite",
        "--namespace", "Zoo.Data",
        "--out", Path.Combine(_dir, "Entities"),
        "--config-file", ConfigPath
    ];

    private void WriteNamedConnectionConfig(string key, string value)
        => File.WriteAllText(ConfigPath,
            "{\"connectionStrings\":{" +
            $"{JsonSerializer.Serialize(key)}:{JsonSerializer.Serialize(value)}" +
            "}}");

    [Fact]
    public async Task Inspect_exits_zero()
        => Assert.Equal(0, await Program.Main(BaseArgs("inspect")));

    // ---- ConnectionStrings:Zonkey ------------------------------------------------------
    //
    // The design spec honours the named map as well as the direct value, "because .NET
    // developers already have the muscle memory" — it is the shape `dotnet user-secrets` and
    // appsettings.Development.json already hold. It was bound but never read, so a caller who
    // followed the spec got "No connection string" and no clue why.

    [Theory]
    [InlineData("Zonkey")]
    [InlineData("zonkey")]
    [InlineData("ZONKEY")]
    public async Task Connection_string_can_come_from_the_named_map(string key)
    {
        WriteNamedConnectionConfig(key, _conn);

        Assert.Equal(0, await Program.Main(NoConnectionArgs("inspect")));
        Assert.Equal(0, await Program.Main(NoConnectionArgs("generate")));
        Assert.True(File.Exists(Path.Combine(_dir, "Entities", "Species.g.cs")));
    }

    /// <summary>
    /// The direct value is the preferred route and must win. The map here points at a database
    /// that does not exist: SQLite would create it empty on open, so if the map won there would
    /// be no tables and no Species.g.cs — which makes this assertion a real discriminator rather
    /// than one that passes either way.
    /// </summary>
    [Fact]
    public async Task Direct_connection_string_wins_over_the_named_map()
    {
        WriteNamedConnectionConfig("Zonkey", $"Data Source={Path.Combine(_dir, "empty.db")}");

        Assert.Equal(0, await Program.Main(BaseArgs("generate")));
        Assert.True(File.Exists(Path.Combine(_dir, "Entities", "Species.g.cs")));
    }

    /// <summary>
    /// End-to-end cover for the named-map route reaching <c>--json</c> at all: it exits 0 and the
    /// payload carries no trace of the database path.
    /// </summary>
    /// <remarks>
    /// This is deliberately <em>not</em> the guard on the connection-string writeback in
    /// <c>ScaffoldPipeline.ResolveConnectionString</c>, despite once being cited as such. No field
    /// of <c>ScaffoldPlan</c> carries a connection string, so reverting the writeback to a local
    /// would leave this test green; it fails before that fix only on the exit code. The writeback
    /// is pinned by <c>ScaffoldPipelineTests.Named_connection_string_is_resolved_and_written_back</c>
    /// (which asserts the property directly) and redaction by
    /// <c>ScaffoldPipelineTests.Json_output_is_camel_cased_and_redacts_the_connection_string</c>
    /// (which seeds a payload that actually contains the string).
    /// <para>
    /// The needle is the JSON-escaped path. <see cref="JsonSerializer"/> writes every <c>\</c> as
    /// <c>\\</c>, so asserting the raw Windows path could not fail here whatever the tool did.
    /// </para>
    /// </remarks>
    [Fact]
    public async Task Connection_string_from_the_named_map_is_still_redacted_in_json()
    {
        WriteNamedConnectionConfig("Zonkey", _conn);

        var stdout = new StringWriter();
        Console.SetOut(stdout);
        try
        {
            Assert.Equal(0, await Program.Main([.. NoConnectionArgs("inspect"), "--json"]));
        }
        finally
        {
            Console.SetOut(new StreamWriter(Console.OpenStandardOutput()) { AutoFlush = true });
        }

        Assert.DoesNotContain(JsonEscaped(_dbPath), stdout.ToString());
    }

    /// <summary>
    /// A filesystem path as it appears once <see cref="JsonSerializer"/> has written it — every
    /// backslash doubled. Asserting a raw Windows path against JSON output is vacuous: the needle
    /// cannot occur whether or not the value was serialized.
    /// </summary>
    private static string JsonEscaped(string value) => JsonSerializer.Serialize(value).Trim('"');

    [Fact]
    public async Task Inspect_json_is_valid_json()
    {
        var stdout = new StringWriter();
        Console.SetOut(stdout);
        try
        {
            await Program.Main([.. BaseArgs("inspect"), "--json"]);
        }
        finally
        {
            Console.SetOut(new StreamWriter(Console.OpenStandardOutput()) { AutoFlush = true });
        }

        using JsonDocument doc = JsonDocument.Parse(stdout.ToString());
        Assert.True(doc.RootElement.TryGetProperty("entities", out _));
    }

    [Fact]
    public async Task Generate_writes_entity_and_wrapper_files()
    {
        Assert.Equal(0, await Program.Main(BaseArgs("generate")));

        Assert.True(File.Exists(Path.Combine(_dir, "Entities", "Species.g.cs")));
        Assert.Contains(
            Directory.EnumerateFiles(_dir, "*.g.cs", SearchOption.AllDirectories),
            f => Path.GetFileName(f).EndsWith("Database.g.cs", StringComparison.Ordinal));
    }

    [Fact]
    public async Task Generate_dry_run_writes_nothing()
    {
        Assert.Equal(0, await Program.Main([.. BaseArgs("generate"), "--dry-run"]));
        Assert.False(Directory.Exists(Path.Combine(_dir, "Entities")));
    }

    [Fact]
    public async Task Generate_is_idempotent()
    {
        await Program.Main(BaseArgs("generate"));
        string first = File.ReadAllText(Path.Combine(_dir, "Entities", "Species.g.cs"));

        await Program.Main(BaseArgs("generate"));
        string second = File.ReadAllText(Path.Combine(_dir, "Entities", "Species.g.cs"));

        Assert.Equal(first, second);
    }

    [Fact]
    public async Task Generate_refuses_to_clobber_a_handwritten_file()
    {
        Directory.CreateDirectory(Path.Combine(_dir, "Entities"));
        string path = Path.Combine(_dir, "Entities", "Species.g.cs");
        File.WriteAllText(path, "// mine\nclass Species { }\n");

        Assert.NotEqual(0, await Program.Main(BaseArgs("generate")));
        Assert.Equal("// mine\nclass Species { }\n", File.ReadAllText(path));
    }

    [Fact]
    public async Task No_g_suffix_when_generated_suffix_false()
    {
        await Program.Main([.. BaseArgs("generate"), "--generated-suffix", "false"]);
        Assert.True(File.Exists(Path.Combine(_dir, "Entities", "Species.cs")));
    }

    /// <summary>
    /// Secret hygiene covers *both* routes into a connection string. The named map is seeded from
    /// a pre-existing config file here (which `init` loads and rewrites), so that half is not
    /// vacuous: without the <c>ConnectionStrings.Clear()</c> in InitCommand it fails on the
    /// <c>"Zonkey"</c> needle.
    /// <para>
    /// The direct-value half asserts the *JSON-escaped* path. <c>_dbPath</c> is a Windows path and
    /// <see cref="JsonSerializer"/> writes every <c>\</c> as <c>\\</c>, so the raw needle could
    /// never appear in the file however <c>init</c> behaved — the assertion that was supposed to
    /// cover the original secret-hygiene rule was the one that could not fail.
    /// </para>
    /// </summary>
    [Fact]
    public async Task Init_writes_config_without_either_connection_string()
    {
        WriteNamedConnectionConfig("Zonkey", _conn);

        Assert.Equal(0, await Program.Main(BaseArgs("init")));

        string json = File.ReadAllText(ConfigPath);

        Assert.Contains("\"provider\"", json);
        Assert.DoesNotContain(JsonEscaped(_dbPath), json);   // --connection
        Assert.DoesNotContain("\"Zonkey\"", json);           // connectionStrings:Zonkey
        Assert.Contains("ZONKEY_SCAFFOLD_ConnectionString", json);

        // Deserializing proves it independently of any escaping question: the value is absent
        // from the object, not merely from the text.
        using JsonDocument doc = JsonDocument.Parse(json);
        Assert.False(doc.RootElement.TryGetProperty("connectionString", out _));
    }

    /// <summary>
    /// Both routes are named, because a caller who followed the spec and used the named map is
    /// precisely the caller most likely to hit this error.
    /// </summary>
    [Fact]
    public async Task Missing_connection_string_fails_with_actionable_message()
    {
        var stderr = new StringWriter();
        Console.SetError(stderr);
        try
        {
            int exit = await Program.Main(["inspect", "--provider", "sqlite"]);
            Assert.NotEqual(0, exit);
            Assert.Contains("ZONKEY_SCAFFOLD_ConnectionString", stderr.ToString());
            Assert.Contains("ConnectionStrings:Zonkey", stderr.ToString());
        }
        finally
        {
            Console.SetError(new StreamWriter(Console.OpenStandardError()) { AutoFlush = true });
        }
    }

    [Fact]
    public async Task Boolean_flag_works_bare_and_with_a_value()
    {
        Assert.Equal(0, await Program.Main([.. BaseArgs("generate"), "--dry-run"]));
        Assert.Equal(0, await Program.Main([.. BaseArgs("generate"), "--dry-run", "true"]));
    }

    /// <summary>
    /// `init --dry-run` must behave like every other command's --dry-run: report what would
    /// happen, write nothing. Before the fix, init accepted --dry-run (it's on every command's
    /// option surface) and silently ignored it — the config file was written regardless, which is
    /// exactly the failure mode this tool exists to avoid (an option the caller explicitly typed
    /// having no effect).
    /// </summary>
    [Fact]
    public async Task Init_dry_run_writes_nothing()
    {
        Assert.Equal(0, await Program.Main([.. BaseArgs("init"), "--dry-run"]));

        string configPath = Path.Combine(_dir, "zonkey.scaffold.config.json");
        Assert.False(File.Exists(configPath));
    }

    /// <summary>
    /// An entity whose class name equals the wrapper's used to destroy the entity file silently:
    /// the entity was written, then the wrapper — same directory, same namespace, same file name —
    /// was written over the top of it, and the run exited 0 with no warning. The overwrite guard
    /// cannot help, because both files legitimately carry the &lt;auto-generated&gt; marker. The
    /// surviving file was the wrapper, containing an adapter over a class that no longer existed;
    /// <c>DataClassAdapter&lt;T&gt;</c> is constrained only <c>where T : class</c>, so it compiled
    /// and failed at runtime instead.
    /// </summary>
    [Fact]
    public async Task Wrapper_class_name_colliding_with_an_entity_is_refused()
    {
        var stderr = new StringWriter();
        Console.SetError(stderr);
        try
        {
            int exit = await Program.Main([.. BaseArgs("generate"), "--wrapper-class", "Species"]);

            Assert.Equal(1, exit);
            Assert.Contains("--wrapper-class", stderr.ToString());
        }
        finally
        {
            Console.SetError(new StreamWriter(Console.OpenStandardError()) { AutoFlush = true });
        }

        // The refusal happens while the plan is built, so nothing was written at all — not even
        // the entity file that used to be created and then eaten.
        Assert.False(File.Exists(Path.Combine(_dir, "Entities", "Species.g.cs")));
    }

    [Fact]
    public async Task Wrapper_class_name_that_does_not_collide_is_fine()
        => Assert.Equal(0, await Program.Main([.. BaseArgs("generate"), "--wrapper-class", "ZooDatabase"]));

    // ---- options this release cannot honour ------------------------------------------
    //
    // Every one of these was bound, persisted into the config file by `init`, and read by
    // nothing: `generate --language vb` exited 0 and wrote C#. The spec's rule is that an
    // explicitly specified option the tool cannot honour is an error, not a no-op, precisely
    // because the tool's audience is agents and an exit-0 no-op gives them no signal at all.
    // They are not removed from the surface — later releases implement them — so the guard has
    // to be a refusal, and it has to keep the defaults silent.

    [Theory]
    [InlineData("--language", "vb")]
    [InlineData("--schema-disambiguation", "prefix")]
    [InlineData("--collections", "generic")]
    [InlineData("--typed-adapters", "true")]
    [InlineData("--relations", "true")]
    public async Task Unimplemented_option_is_refused_rather_than_ignored(string option, string value)
    {
        var stderr = new StringWriter();
        Console.SetError(stderr);
        try
        {
            int exit = await Program.Main([.. BaseArgs("generate"), option, value]);

            // 1 = refused (a ScaffoldException), not 2 = crashed.
            Assert.Equal(1, exit);
            Assert.Contains(option, stderr.ToString());
        }
        finally
        {
            Console.SetError(new StreamWriter(Console.OpenStandardError()) { AutoFlush = true });
        }

        Assert.False(Directory.Exists(Path.Combine(_dir, "Entities")),
            "a refused run must not have written entity files.");
    }

    [Fact]
    public async Task Unimplemented_option_is_refused_by_inspect_too()
    {
        Assert.Equal(1, await Program.Main([.. BaseArgs("inspect"), "--relations"]));
    }

    /// <summary>
    /// The other half of the rule, and the reason this cannot simply reject the options outright:
    /// an option sitting at its default is silently irrelevant, so `--relations false` and
    /// `--language csharp` must still succeed. Erroring on an untouched default would make the
    /// config file `init` writes unloadable by the very tool that wrote it.
    /// </summary>
    [Theory]
    [InlineData("--language", "csharp")]
    [InlineData("--schema-disambiguation", "none")]
    [InlineData("--collections", "none")]
    [InlineData("--typed-adapters", "false")]
    [InlineData("--relations", "false")]
    public async Task Explicit_default_value_is_still_accepted(string option, string value)
        => Assert.Equal(0, await Program.Main([.. BaseArgs("generate"), option, value]));

    /// <summary>
    /// `init` writes every one of these keys into the config file. Round-tripping that file back
    /// through the tool must work, or the tool's own output would be rejected on the next run.
    /// </summary>
    [Fact]
    public async Task Config_file_written_by_init_round_trips_through_generate()
    {
        Assert.Equal(0, await Program.Main(BaseArgs("init")));
        Assert.Equal(0, await Program.Main(BaseArgs("generate")));
    }

    /// <summary>
    /// Pins the actual defect the reviewer flagged: with System.CommandLine's default exception
    /// handler enabled, it catches every exception *before* Program.Main's own try/catch ever
    /// runs, so SecretRedactor.Redact is never invoked on the error path and a raw secret value
    /// would reach stderr verbatim. This forces a real (unmodified) production code path —
    /// SchemaScopeResolver's "schema not found" message echoes the requested schema name verbatim
    /// — to embed a password-shaped value in a ScaffoldException's message, then asserts it never
    /// reaches stderr. A --schema value is used as the vehicle rather than the --connection value
    /// itself because Microsoft.Data.Sqlite's own exceptions were verified (manually, against the
    /// built exe) to never echo connection-string values back — this reaches the identical
    /// Program.Main catch-and-redact path deterministically instead of depending on that.
    /// </summary>
    [Fact]
    public async Task Unexpected_error_redacts_secret_values_from_stderr()
    {
        const string secret = "Sup3rSecret123";

        var stderr = new StringWriter();
        Console.SetError(stderr);
        try
        {
            int exit = await Program.Main([
                "inspect",
                "--provider", "sqlite",
                "--connection", _conn,
                "--schema", $"Password={secret}"
            ]);

            Assert.NotEqual(0, exit);

            string text = stderr.ToString();
            Assert.DoesNotContain(secret, text);
            Assert.Contains(SecretRedactor.Marker, text);
        }
        finally
        {
            Console.SetError(new StreamWriter(Console.OpenStandardError()) { AutoFlush = true });
        }
    }

    // ---- the warning count in `generate`'s summary -------------------------------------
    //
    // `inspect` filters info-level entries out of its Warnings list; `generate` counted them, so
    // an UNMAPPABLE_TYPE that a dbType override had already answered still printed "1 warning(s)"
    // — the one number an agent keys off, still telling it to act on something it had just fixed.

    private async Task AddTables(string ddl)
    {
        await using var cnxn = new SqliteConnection(_conn);
        await cnxn.OpenAsync();
        await using var cmd = cnxn.CreateCommand();
        cmd.CommandText = ddl;
        await cmd.ExecuteNonQueryAsync();
    }

    private ScaffoldOptions GadgetOptions(string? forcedDbType)
    {
        var o = new ScaffoldOptions
        {
            Provider = "sqlite",
            ConnectionString = _conn,
            Namespace = "Zoo.Data",
            Output = { Entities = Path.Combine(_dir, "Entities") }
        };

        if (forcedDbType is not null)
        {
            o.Overrides.Tables["gadgets"] = new TableOverride
            {
                Columns = { ["Shape"] = new ColumnOverride { DbType = forcedDbType } }
            };
        }

        return o;
    }

    [Fact]
    public async Task Generate_summary_does_not_count_info_level_warnings()
    {
        await AddTables("""CREATE TABLE gadgets (Id INTEGER PRIMARY KEY, Shape GEOGRAPHY);""");

        var sw = new StringWriter();
        Assert.Equal(0, await GenerateCommand.Run(
            GadgetOptions("AnsiString"), dryRun: true, json: false, _dir, sw, CancellationToken.None));

        Assert.Contains("0 warning(s)", sw.ToString());
    }

    /// <summary>The control: an unanswered warning must still be counted.</summary>
    [Fact]
    public async Task Generate_summary_still_counts_real_warnings()
    {
        await AddTables("""CREATE TABLE gadgets (Id INTEGER PRIMARY KEY, Shape GEOGRAPHY);""");

        var sw = new StringWriter();
        Assert.Equal(0, await GenerateCommand.Run(
            GadgetOptions(null), dryRun: true, json: false, _dir, sw, CancellationToken.None));

        Assert.Contains("1 warning(s)", sw.ToString());
    }

    /// <summary>
    /// Info-level entries are only hidden from the human-readable *count*; the --json payload still
    /// carries every warning, so nothing that wants the detail loses it.
    /// </summary>
    [Fact]
    public async Task Info_level_warnings_are_still_present_in_json_output()
    {
        await AddTables("""CREATE TABLE gadgets (Id INTEGER PRIMARY KEY, Shape GEOGRAPHY);""");

        var sw = new StringWriter();
        Assert.Equal(0, await GenerateCommand.Run(
            GadgetOptions("AnsiString"), dryRun: true, json: true, _dir, sw, CancellationToken.None));

        Assert.Contains("UNMAPPABLE_TYPE", sw.ToString());
        Assert.Contains("\"info\"", sw.ToString());
    }
}
