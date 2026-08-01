using Xunit;
using Zonkey.Scaffold.Config;

public class ConfigurationLoaderTests : IDisposable
{
    private readonly string _dir = Directory.CreateTempSubdirectory("zscaffold").FullName;
    public void Dispose() => Directory.Delete(_dir, recursive: true);

    private void WriteConfig(string json)
        => File.WriteAllText(Path.Combine(_dir, "zonkey.scaffold.config.json"), json);

    [Fact]
    public void Json_supplies_baseline()
    {
        WriteConfig("""{ "provider": "sqlite", "namespace": "A.B" }""");
        var o = ConfigurationLoader.Load(null, new Dictionary<string, string?>(), _dir);
        Assert.Equal("sqlite", o.Provider);
        Assert.Equal("A.B", o.Namespace);
    }

    [Fact]
    public void CommandLine_beats_json()
    {
        WriteConfig("""{ "provider": "sqlite", "namespace": "A.B" }""");
        var cli = new Dictionary<string, string?> { ["Namespace"] = "Cli.Wins" };
        var o = ConfigurationLoader.Load(null, cli, _dir);
        Assert.Equal("Cli.Wins", o.Namespace);
        Assert.Equal("sqlite", o.Provider);   // untouched key still shows through
    }

    [Fact]
    public void Environment_beats_json_and_loses_to_commandline()
    {
        WriteConfig("""{ "namespace": "FromJson" }""");
        Environment.SetEnvironmentVariable("ZONKEY_SCAFFOLD_Namespace", "FromEnv");
        try
        {
            var envOnly = ConfigurationLoader.Load(null, new Dictionary<string, string?>(), _dir);
            Assert.Equal("FromEnv", envOnly.Namespace);

            var cli = new Dictionary<string, string?> { ["Namespace"] = "FromCli" };
            Assert.Equal("FromCli", ConfigurationLoader.Load(null, cli, _dir).Namespace);
        }
        finally
        {
            Environment.SetEnvironmentVariable("ZONKEY_SCAFFOLD_Namespace", null);
        }
    }

    [Fact]
    public void ConnectionString_binds_from_environment()
    {
        Environment.SetEnvironmentVariable("ZONKEY_SCAFFOLD_ConnectionString", "Data Source=x.db");
        try
        {
            var o = ConfigurationLoader.Load(null, new Dictionary<string, string?>(), _dir);
            Assert.Equal("Data Source=x.db", o.ConnectionString);
        }
        finally
        {
            Environment.SetEnvironmentVariable("ZONKEY_SCAFFOLD_ConnectionString", null);
        }
    }

    [Fact]
    public void Delimited_scalar_expands_into_list()
    {
        Environment.SetEnvironmentVariable("ZONKEY_SCAFFOLD_Ignore__Tables", "__*;aspnet_*");
        try
        {
            var o = ConfigurationLoader.Load(null, new Dictionary<string, string?>(), _dir);
            Assert.Equal(new[] { "__*", "aspnet_*" }, o.Ignore.Tables);
        }
        finally
        {
            Environment.SetEnvironmentVariable("ZONKEY_SCAFFOLD_Ignore__Tables", null);
        }
    }

    [Fact]
    public void Missing_config_file_is_not_an_error()
    {
        var o = ConfigurationLoader.Load(null, new Dictionary<string, string?>(), _dir);
        Assert.Null(o.Provider);
        Assert.True(o.Emit.PartialClasses);      // defaults survive
    }

    [Fact]
    public void Json_array_survives_expand_delimited()
    {
        // A real JSON array binds directly onto the list. ExpandDelimited must not see a scalar
        // at "Ignore:Tables" here (IConfiguration exposes indexed children instead) and must
        // therefore leave the bound list untouched rather than clearing it.
        WriteConfig("""{ "ignore": { "tables": ["foo", "bar", "baz"] } }""");
        var o = ConfigurationLoader.Load(null, new Dictionary<string, string?>(), _dir);
        Assert.Equal(new[] { "foo", "bar", "baz" }, o.Ignore.Tables);
    }
}
