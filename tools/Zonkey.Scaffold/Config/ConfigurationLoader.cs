using Microsoft.Extensions.Configuration;
using Zonkey.Scaffold.Options;

namespace Zonkey.Scaffold.Config;

public static class ConfigurationLoader
{
    public const string DefaultFileName = "zonkey.scaffold.config.json";
    public const string EnvPrefix = "ZONKEY_SCAFFOLD_";

    public static ScaffoldOptions Load(
        string? configFile,
        IDictionary<string, string?> cliValues,
        string workingDirectory,
        IDictionary<string, IReadOnlyList<string>>? cliLists = null)
    {
        string path = configFile is null
            ? Path.Combine(workingDirectory, DefaultFileName)
            : Path.GetFullPath(configFile, workingDirectory);

        IConfigurationRoot config = new ConfigurationBuilder()
            .AddJsonFile(path, optional: true, reloadOnChange: false)
            .AddEnvironmentVariables(EnvPrefix)
            .AddInMemoryCollection(cliValues)
            .Build();

        var options = config.Get<ScaffoldOptions>() ?? new ScaffoldOptions();

        ExpandDelimited(config, "Ignore:Tables",  options.Ignore.Tables);
        ExpandDelimited(config, "Ignore:Columns", options.Ignore.Columns);
        ExpandDelimited(config, "Include:Tables", options.Include.Tables);
        ExpandDelimited(config, "Schemas",        options.Schemas);

        // Applied last and outside the configuration stack entirely, because the stack cannot
        // express what the command line means here. See Replace.
        Replace(cliLists, "Ignore:Tables",  options.Ignore.Tables);
        Replace(cliLists, "Ignore:Columns", options.Ignore.Columns);
        Replace(cliLists, "Include:Tables", options.Include.Tables);
        Replace(cliLists, "Schemas",        options.Schemas);

        return options;
    }

    /// <summary>
    /// Overwrites a bound list with the command line's, wholesale.
    /// </summary>
    /// <remarks>
    /// This cannot be done by adding another configuration provider.
    /// <see cref="IConfiguration"/> merges arrays by unioning child indices across providers, so
    /// an in-memory provider holding <c>Ignore:Tables:0 = onlythis</c> layered over a JSON file
    /// holding <c>["alpha","beta","gamma"]</c> yields <c>["onlythis","beta","gamma"]</c> — the CLI
    /// value replaces only the element it happens to line up with, and the rest of the config
    /// file's list survives. The observable damage: <c>--table X</c> generated files the caller
    /// never asked for, and <c>--ignore-table X</c> un-ignored the config's first entry while the
    /// skip report cheerfully confirmed the wrong answer. The spec says the command line
    /// overrides the config file, so replacement happens here, after binding, where "the whole
    /// list" is expressible.
    /// <para>
    /// Same clear-then-fill shape as <see cref="ExpandDelimited"/> — which is why the delimited
    /// scalar path never had this bug.
    /// </para>
    /// </remarks>
    private static void Replace(
        IDictionary<string, IReadOnlyList<string>>? cliLists, string key, List<string> target)
    {
        if (cliLists is null || !cliLists.TryGetValue(key, out IReadOnlyList<string>? items)) return;

        target.Clear();
        target.AddRange(items);
    }

    /// <summary>
    /// Binding handles JSON arrays and indexed env vars natively. This additionally accepts a
    /// single delimited scalar (Ignore__Tables="__*;aspnet_*"), because the indexed form
    /// (Ignore__Tables__0=...) is unusable in a CI environment block. When the key holds a real
    /// array (JSON array or indexed env vars), <see cref="IConfiguration"/> exposes it as indexed
    /// children rather than a scalar at the key itself, so <c>config[key]</c> returns null and the
    /// bound list is left untouched here.
    /// </summary>
    private static void ExpandDelimited(IConfiguration config, string key, List<string> target)
    {
        string? scalar = config[key];
        if (string.IsNullOrWhiteSpace(scalar)) return;

        target.Clear();
        target.AddRange(scalar
            .Split([';', ','], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries));
    }
}
