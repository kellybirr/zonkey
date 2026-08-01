using Xunit;
using Zonkey.Scaffold.Diagnostics;
using Zonkey.Scaffold.Options;

namespace Zonkey.Scaffold.Tests.Options;

/// <summary>
/// CommandTests proves the refusal end to end through exit codes; these pin the rule itself, so
/// a regression fails next to the decision instead of as a mystery exit code. The important pair
/// is <see cref="Untouched_defaults_are_accepted"/> against the rejection cases: the rule is
/// "explicit-but-unhonourable is an error, default is silent", and losing either half is a bug.
/// </summary>
public class OptionValidatorTests
{
    private static ScaffoldOptions Options() => new();

    [Fact]
    public void Untouched_defaults_are_accepted()
        => OptionValidator.Validate(Options());

    [Fact]
    public void Explicitly_stated_default_values_are_accepted()
    {
        var o = Options();
        o.Language = "csharp";
        o.SchemaDisambiguation = "none";
        o.Emit.Collections = "none";
        o.Emit.TypedAdapters = false;
        o.Emit.Relations = false;

        OptionValidator.Validate(o);
    }

    [Fact]
    public void Case_and_whitespace_do_not_defeat_the_default_check()
    {
        var o = Options();
        o.Language = " CSharp ";
        o.Emit.Collections = "NONE";

        OptionValidator.Validate(o);
    }

    [Fact]
    public void Unsupported_language_is_refused_and_names_the_switch()
    {
        var o = Options();
        o.Language = "vb";

        var ex = Assert.Throws<ScaffoldException>(() => OptionValidator.Validate(o));
        Assert.Contains("--language", ex.Message);
        Assert.Contains("C# only", ex.Message);
    }

    [Theory]
    [InlineData("prefix")]
    [InlineData("namespace")]
    public void Unsupported_schema_disambiguation_is_refused(string value)
    {
        var o = Options();
        o.SchemaDisambiguation = value;

        Assert.Contains("--schema-disambiguation",
            Assert.Throws<ScaffoldException>(() => OptionValidator.Validate(o)).Message);
    }

    [Theory]
    [InlineData("generic")]
    [InlineData("dataclass")]
    [InlineData("bindable")]
    public void Unsupported_collections_mode_is_refused(string value)
    {
        var o = Options();
        o.Emit.Collections = value;

        Assert.Contains("--collections",
            Assert.Throws<ScaffoldException>(() => OptionValidator.Validate(o)).Message);
    }

    [Fact]
    public void Typed_adapters_are_refused()
    {
        var o = Options();
        o.Emit.TypedAdapters = true;

        Assert.Contains("--typed-adapters",
            Assert.Throws<ScaffoldException>(() => OptionValidator.Validate(o)).Message);
    }

    [Fact]
    public void Relations_are_refused()
    {
        var o = Options();
        o.Emit.Relations = true;

        Assert.Contains("--relations",
            Assert.Throws<ScaffoldException>(() => OptionValidator.Validate(o)).Message);
    }

    /// <summary>
    /// The SQLite reader filters <c>sqlite_%</c> unconditionally, so <c>--system-tables true</c>
    /// changes nothing at all — the same bound-and-ignored failure as the five above, and it was
    /// missed the first time round because the refusal list was written from the review's
    /// examples rather than from a sweep of every setting.
    /// </summary>
    [Fact]
    public void System_tables_are_refused()
    {
        var o = Options();
        o.Emit.SystemTables = true;

        Assert.Contains("--system-tables",
            Assert.Throws<ScaffoldException>(() => OptionValidator.Validate(o)).Message);
    }

    /// <summary>
    /// <c>connectionStrings</c> is a specified feature (the design spec honours
    /// <c>ConnectionStrings:Zonkey</c> "because .NET developers already have the muscle memory"),
    /// not a dead setting — the remedy for its not being read was to implement it, in
    /// <c>ScaffoldPipeline.Build</c>, not to refuse the config that uses it. Refusing it would
    /// have rejected a file whose only fault was carrying the value the tool needs to run.
    /// </summary>
    [Fact]
    public void A_named_connection_string_map_is_not_an_unimplemented_option()
    {
        var o = Options();
        o.ConnectionStrings["Zonkey"] = "Data Source=x.db";

        OptionValidator.Validate(o);
    }

    /// <summary>
    /// A caller fixing a config file should see every offending key at once, the way
    /// class-name collisions are reported, rather than discovering them one run at a time.
    /// </summary>
    [Fact]
    public void Every_violation_is_reported_in_one_message()
    {
        var o = Options();
        o.Language = "vb";
        o.Emit.Relations = true;
        o.Emit.TypedAdapters = true;

        var ex = Assert.Throws<ScaffoldException>(() => OptionValidator.Validate(o));

        Assert.Contains("3 option(s)", ex.Message);
        Assert.Contains("--language", ex.Message);
        Assert.Contains("--relations", ex.Message);
        Assert.Contains("--typed-adapters", ex.Message);
    }
}
