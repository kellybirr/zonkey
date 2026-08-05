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
        // Maps to the scalar key, not "Schemas:0". An indexed key makes IConfiguration expose
        // Schemas as having children, so config["Schemas"] returns null and the Split below never
        // runs — `--schema public;archive` then became one schema literally named "public;archive".
        // The indexed form is still available directly as --Schemas:0 / --Schemas:1.
        ["--schema"] = "Schemas",
        ["--wrapper-class"] = "Wrapper:ClassName",
        ["--dry-run"] = "DryRun",
    };

    public static ScaffoldOptions Load(string[] args, string workingDirectory)
    {
        IConfigurationRoot config = new ConfigurationBuilder()
            .AddJsonFile(Path.Combine(workingDirectory, DefaultFileName), optional: true)
            .AddEnvironmentVariables(EnvPrefix)
            .AddCommandLine(ExpandBareSwitches(args), Aliases)
            .Build();

        var options = config.Get<ScaffoldOptions>() ?? new ScaffoldOptions();

        // IConfiguration binds arrays from indexed keys (Schemas:0), which is unusable in a CI
        // environment block and tedious on a command line. Accept a delimited scalar too.
        Split(config["Schemas"], options.Schemas);
        Split(config["IgnoreTables"], options.IgnoreTables);

        return options;
    }

    /// <summary>
    /// Gives a valueless boolean switch an explicit <c>true</c>.
    /// </summary>
    /// <remarks>
    /// IConfiguration's command-line provider has no concept of a flag: it reads
    /// <c>--key value</c> pairs, and a trailing <c>--dry-run</c> with nothing after it is simply
    /// dropped. That left <see cref="ScaffoldOptions.DryRun"/> false and the tool wrote files —
    /// the one switch whose entire purpose is not to. Only <c>--dry-run true</c> worked, which is
    /// not what the help text, the README or any reasonable person types.
    /// <para>
    /// Applied only to keys that are genuinely <c>bool</c> on <see cref="ScaffoldOptions"/>, so a
    /// value that happens to begin with <c>-</c> is never mistaken for the next switch.
    /// </para>
    /// </remarks>
    private static string[] ExpandBareSwitches(string[] args)
    {
        var expanded = new List<string>(args.Length);

        for (int i = 0; i < args.Length; i++)
        {
            expanded.Add(args[i]);

            if (!IsBooleanSwitch(args[i])) continue;

            bool hasValue = i + 1 < args.Length && !args[i + 1].StartsWith('-');
            if (!hasValue) expanded.Add("true");
        }

        return expanded.ToArray();
    }

    private static bool IsBooleanSwitch(string arg)
    {
        if (!arg.StartsWith('-')) return false;

        string token = arg.TrimStart('-');

        // An inline `--key=value` already carries its value.
        if (token.Contains('=')) return false;

        return BooleanKeys.Contains(Aliases.TryGetValue(arg, out string? mapped) ? mapped : token);
    }

    /// <summary>Every <c>bool</c> configuration key, by reflection so it cannot go stale.</summary>
    private static readonly HashSet<string> BooleanKeys = BuildBooleanKeys();

    private static HashSet<string> BuildBooleanKeys()
    {
        var keys = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        Collect(typeof(ScaffoldOptions), "");
        return keys;

        void Collect(Type type, string prefix)
        {
            foreach (System.Reflection.PropertyInfo p in type.GetProperties())
            {
                if (p.PropertyType == typeof(bool))
                    keys.Add(prefix + p.Name);
                else if (p.PropertyType.IsClass && p.PropertyType.Namespace == type.Namespace)
                    Collect(p.PropertyType, prefix + p.Name + ":");
            }
        }
    }

    private static void Split(string? scalar, List<string> target)
    {
        if (string.IsNullOrWhiteSpace(scalar)) return;

        target.Clear();
        target.AddRange(scalar.Split([';', ','],
            StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries));
    }
}
