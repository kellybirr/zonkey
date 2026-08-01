using System.CommandLine;
using Zonkey.Scaffold.Commands;
using Zonkey.Scaffold.Config;
using Zonkey.Scaffold.Options;

namespace Zonkey.Scaffold.Cli;

public static class CommandFactory
{
    public static RootCommand Create()
    {
        var root = new RootCommand("Scaffold Zonkey data classes from a database schema.");

        root.Subcommands.Add(Build("init",     "Write zonkey.scaffold.config.json."));
        root.Subcommands.Add(Build("inspect",  "Report the schema and the mapping decisions."));
        root.Subcommands.Add(Build("generate", "Write entity classes and the DatabaseWrapper."));

        return root;
    }

    private static Command Build(string name, string description)
    {
        var command = new Command(name, description);

        // `init` writes a config file to disk; there is no JSON report for it to produce (unlike
        // inspect/generate, which can render their result as either text or JSON), so --json is
        // left off its surface entirely rather than being accepted and silently ignored.
        IEnumerable<Option> commandOptions = name == "init"
            ? OptionDefinitions.All().Where(o => !ReferenceEquals(o, OptionDefinitions.Json))
            : OptionDefinitions.All();

        foreach (Option option in commandOptions) command.Options.Add(option);

        command.SetAction((parseResult, ct) =>
        {
            string cwd = Directory.GetCurrentDirectory();
            string? configFile = parseResult.GetValue(OptionDefinitions.ConfigFile);

            // Scalars and lists travel separately: a scalar is last-provider-wins inside the
            // configuration stack, but a list is not (IConfiguration unions array indices across
            // providers), so a CLI list has to be applied by ConfigurationLoader after binding.
            ScaffoldOptions options = ConfigurationLoader.Load(
                configFile,
                OptionDefinitions.ToConfigurationValues(parseResult),
                cwd,
                OptionDefinitions.ToListValues(parseResult));

            // --json and --dry-run never enter the configuration stack (they are per-invocation
            // switches, not persisted settings), but they share the same ZeroOrOne/DefaultValueFactory
            // shape as every option in OptionDefinitions.ToConfigurationValues: GetValue alone would
            // return true even when the flag was never typed, because the default value factory
            // (needed so a bare `--dry-run` means true) also backfills the fully-absent case. Gating
            // on WasSpecified is what makes "not typed" mean false here instead of true.
            bool json = OptionDefinitions.WasSpecified(parseResult, OptionDefinitions.Json)
                        && parseResult.GetValue(OptionDefinitions.Json);
            bool dryRun = OptionDefinitions.WasSpecified(parseResult, OptionDefinitions.DryRun)
                        && parseResult.GetValue(OptionDefinitions.DryRun);

            return name switch
            {
                "init"     => InitCommand.Run(options, configFile, dryRun, cwd, Console.Out),
                "inspect"  => InspectCommand.Run(options, json, cwd, Console.Out, ct),
                "generate" => GenerateCommand.Run(options, dryRun, json, cwd, Console.Out, ct),
                _ => Task.FromResult(1)
            };
        });

        return command;
    }
}
