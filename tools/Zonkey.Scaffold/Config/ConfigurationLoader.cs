using Microsoft.Extensions.Configuration;
using Zonkey.Scaffold.Options;

namespace Zonkey.Scaffold.Config;

/// <summary>
/// Config file, then environment, then command line — plain IConfiguration, no argument parser.
/// Anything in <see cref="ScaffoldOptions"/> is settable as <c>--Section:Key value</c>.
/// </summary>
public static class ConfigurationLoader
{
    public const string DefaultFileName = "zonkey.scaffold.json";
    public const string EnvPrefix = "ZONKEY_SCAFFOLD_";

    /// <summary>Short aliases for the options people actually type.</summary>
    private static readonly Dictionary<string, string> Aliases = new()
    {
        ["-p"] = "Provider",
        ["--provider"] = "Provider",
        ["-c"] = "ConnectionString",
        ["--connection"] = "ConnectionString",
        ["-n"] = "Namespace",
        ["--namespace"] = "Namespace",
        ["-o"] = "Output:Entities",
        ["--out"] = "Output:Entities",
        ["--schema"] = "Schemas:0",
        ["--wrapper-class"] = "Wrapper:ClassName",
        ["--dry-run"] = "DryRun",
    };

    public static ScaffoldOptions Load(string[] args, string workingDirectory)
    {
        IConfigurationRoot config = new ConfigurationBuilder()
            .AddJsonFile(Path.Combine(workingDirectory, DefaultFileName), optional: true)
            .AddEnvironmentVariables(EnvPrefix)
            .AddCommandLine(args, Aliases)
            .Build();

        var options = config.Get<ScaffoldOptions>() ?? new ScaffoldOptions();

        // IConfiguration binds arrays from indexed keys (Schemas:0), which is unusable in a CI
        // environment block and tedious on a command line. Accept a delimited scalar too.
        Split(config["Schemas"], options.Schemas);
        Split(config["IgnoreTables"], options.IgnoreTables);

        return options;
    }

    private static void Split(string? scalar, List<string> target)
    {
        if (string.IsNullOrWhiteSpace(scalar)) return;

        target.Clear();
        target.AddRange(scalar.Split([';', ','],
            StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries));
    }
}
