using Xunit;
using Zonkey.Scaffold.Cli;
using Zonkey.Scaffold.Config;

namespace Zonkey.Scaffold.Tests.Cli;

/// <summary>
/// Directly exercises the contract <see cref="OptionDefinitions.ToConfigurationValues"/> promises:
/// only options the caller actually typed may enter the configuration stack. CommandTests proves
/// this end to end through exit codes and file contents; these tests pin the dictionary itself so
/// a regression here fails close to the cause instead of showing up as a mysterious masked config
/// value three layers away.
/// </summary>
public class OptionDefinitionsTests
{
    private static System.CommandLine.ParseResult Parse(params string[] args)
        => CommandFactory.Create().Parse(args);

    [Fact]
    public void Unspecified_boolean_flag_is_absent_from_the_dictionary()
    {
        var result = Parse("generate");
        var values = OptionDefinitions.ToConfigurationValues(result);

        Assert.False(values.ContainsKey("Emit:Relations"));
        Assert.False(values.ContainsKey("Emit:PartialClasses"));
    }

    [Fact]
    public void Unspecified_string_option_is_absent_from_the_dictionary()
    {
        var result = Parse("generate");
        var values = OptionDefinitions.ToConfigurationValues(result);

        Assert.False(values.ContainsKey("Provider"));
        Assert.False(values.ContainsKey("ConnectionString"));
    }

    [Fact]
    public void Bare_boolean_flag_maps_to_true()
    {
        var result = Parse("generate", "--relations");
        var values = OptionDefinitions.ToConfigurationValues(result);

        Assert.Equal("true", values["Emit:Relations"]);
    }

    [Fact]
    public void Boolean_flag_with_explicit_false_maps_to_false()
    {
        var result = Parse("generate", "--relations", "false");
        var values = OptionDefinitions.ToConfigurationValues(result);

        Assert.Equal("false", values["Emit:Relations"]);
    }

    [Fact]
    public void List_option_travels_on_its_own_channel_not_as_indexed_keys()
    {
        var result = Parse("generate", "--table", "Species", "--table", "Zookeeper");

        Assert.Equal(
            new[] { "Species", "Zookeeper" },
            OptionDefinitions.ToListValues(result)["Include:Tables"]);

        // Indexed keys in the flat dictionary are what caused the union-instead-of-replace bug;
        // nothing may put a list back onto that channel.
        var values = OptionDefinitions.ToConfigurationValues(result);
        Assert.DoesNotContain(values.Keys, k => k.StartsWith("Include:Tables", StringComparison.Ordinal));
    }

    [Fact]
    public void Unspecified_list_option_is_absent_from_the_list_channel()
        => Assert.Empty(OptionDefinitions.ToListValues(Parse("generate")));

    // ---- CLI list replaces the config file's list, element for element ----------------
    //
    // IConfiguration resolves an array as the union of child indices across providers, so the
    // old indexed-key route merged a CLI list into the JSON one instead of replacing it:
    // `--table delta` against `"tables": ["alpha","beta"]` generated delta *and* beta, and
    // `--ignore-table onlythis` against a three-element ignore list silently un-ignored the
    // element at index 0. Every one of these tests supplies a longer list in JSON than on the
    // command line, because a shorter CLI list is precisely what leaves config entries exposed.

    [Theory]
    [InlineData("--schema")]
    [InlineData("--table")]
    [InlineData("--ignore-table")]
    [InlineData("--ignore-column")]
    public void Cli_list_replaces_the_config_list_entirely(string option)
    {
        string dir = Directory.CreateTempSubdirectory("zlist").FullName;
        try
        {
            File.WriteAllText(
                Path.Combine(dir, "zonkey.scaffold.config.json"),
                """
                {
                  "schemas": ["alpha", "beta", "gamma"],
                  "include": { "tables": ["alpha", "beta", "gamma"] },
                  "ignore":  { "tables": ["alpha", "beta", "gamma"],
                               "columns": ["alpha", "beta", "gamma"] }
                }
                """);

            var result = Parse("generate", option, "onlythis");
            var options = ConfigurationLoader.Load(
                null,
                OptionDefinitions.ToConfigurationValues(result),
                dir,
                OptionDefinitions.ToListValues(result));

            List<string> actual = option switch
            {
                "--schema"        => options.Schemas,
                "--table"         => options.Include.Tables,
                "--ignore-table"  => options.Ignore.Tables,
                _                 => options.Ignore.Columns
            };

            Assert.Equal(new[] { "onlythis" }, actual);
        }
        finally
        {
            Directory.Delete(dir, recursive: true);
        }
    }

    [Fact]
    public void Cli_list_leaves_the_other_config_lists_alone()
    {
        string dir = Directory.CreateTempSubdirectory("zlist").FullName;
        try
        {
            File.WriteAllText(
                Path.Combine(dir, "zonkey.scaffold.config.json"),
                """{ "include": { "tables": ["a","b"] }, "ignore": { "tables": ["x","y"] } }""");

            var result = Parse("generate", "--table", "delta");
            var options = ConfigurationLoader.Load(
                null,
                OptionDefinitions.ToConfigurationValues(result),
                dir,
                OptionDefinitions.ToListValues(result));

            Assert.Equal(new[] { "delta" }, options.Include.Tables);
            Assert.Equal(new[] { "x", "y" }, options.Ignore.Tables);
        }
        finally
        {
            Directory.Delete(dir, recursive: true);
        }
    }

    /// <summary>
    /// A list option the caller never typed must leave the config file's list intact — the
    /// replace-don't-merge fix must not turn into clobber-always.
    /// </summary>
    [Fact]
    public void Untyped_list_option_does_not_clear_the_config_list()
    {
        string dir = Directory.CreateTempSubdirectory("zlist").FullName;
        try
        {
            File.WriteAllText(
                Path.Combine(dir, "zonkey.scaffold.config.json"),
                """{ "ignore": { "tables": ["alpha","beta"] } }""");

            var result = Parse("generate");
            var options = ConfigurationLoader.Load(
                null,
                OptionDefinitions.ToConfigurationValues(result),
                dir,
                OptionDefinitions.ToListValues(result));

            Assert.Equal(new[] { "alpha", "beta" }, options.Ignore.Tables);
        }
        finally
        {
            Directory.Delete(dir, recursive: true);
        }
    }

    /// <summary>
    /// The end-to-end version of the masking bug this contract exists to prevent: a JSON config
    /// value must survive a run whose command line simply never mentions that option, not just
    /// when queried in isolation via <see cref="ToConfigurationValues"/> directly.
    /// </summary>
    [Fact]
    public void Unspecified_cli_option_does_not_mask_a_json_config_value()
    {
        string dir = Directory.CreateTempSubdirectory("zopt").FullName;
        try
        {
            File.WriteAllText(
                Path.Combine(dir, "zonkey.scaffold.config.json"),
                """{ "emit": { "relations": true } }""");

            var result = Parse("generate");
            var cliValues = OptionDefinitions.ToConfigurationValues(result);

            var options = ConfigurationLoader.Load(null, cliValues, dir);

            Assert.True(options.Emit.Relations);
        }
        finally
        {
            Directory.Delete(dir, recursive: true);
        }
    }
}
