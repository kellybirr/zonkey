using System.Text.Json;
using System.Text.Json.Serialization;
using Zonkey.Scaffold.Config;
using Zonkey.Scaffold.Options;

namespace Zonkey.Scaffold.Commands;

public static class InitCommand
{
    private static readonly JsonSerializerOptions Options = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        WriteIndented = true
    };

    public static Task<int> Run(
        ScaffoldOptions options, string? configFile, bool dryRun, string workingDirectory, TextWriter stdout)
    {
        // `init` never touches a database, so it skips the rest of the pipeline — but it is the
        // command that writes these keys into the config file, and writing a file that the very
        // next `generate` will refuse would be the same silent-acceptance failure one step
        // removed. Refuse here, at the point the value is captured.
        OptionValidator.Validate(options);

        string path = configFile is null
            ? Path.Combine(workingDirectory, ConfigurationLoader.DefaultFileName)
            : Path.GetFullPath(configFile, workingDirectory);

        // Never serialize the connection string, even when it arrived on the command line.
        // The naive implementation commits a password to git.
        string? held = options.ConnectionString;
        options.ConnectionString = null;
        options.ConnectionStrings.Clear();

        string json = JsonSerializer.Serialize(options, Options);
        options.ConnectionString = held;

        json = json.Insert(1,
            "\n  \"//\": \"Set the connection string via the ZONKEY_SCAFFOLD_ConnectionString " +
            "environment variable. It is deliberately never written to this file.\",");

        // A caller who typed --dry-run gets exactly what every other command promises: a report
        // of what would happen and no write. Silently ignoring the flag here (the file would
        // still land on disk) is the failure mode this whole tool exists to avoid.
        if (dryRun)
        {
            stdout.WriteLine($"Would write {path}:");
            stdout.WriteLine(json);
            return Task.FromResult(0);
        }

        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        File.WriteAllText(path, json + "\n");

        stdout.WriteLine($"Wrote {path}");
        return Task.FromResult(0);
    }
}
