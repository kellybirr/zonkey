using System.CommandLine;
using System.CommandLine.Parsing;

namespace Zonkey.Scaffold.Cli;

/// <summary>
/// One place for every switch. Booleans use ZeroOrOne arity so `--dry-run` and
/// `--dry-run false` both parse — that is what lets every switch be named positively
/// without needing a `--no-*` twin.
/// </summary>
public static class OptionDefinitions
{
    public static readonly Option<string> Provider = new("--provider") { Description = "sqlite" };
    public static readonly Option<string> Connection = new("--connection")
    { Description = "Connection string. Prefer ZONKEY_SCAFFOLD_ConnectionString to keep it out of logs." };
    public static readonly Option<string> ConfigFile = new("--config-file");
    public static readonly Option<string> Namespace = new("--namespace");
    public static readonly Option<string> Out = new("--out") { Description = "Entity output directory." };
    public static readonly Option<string> WrapperOut = new("--wrapper-out");
    public static readonly Option<string> WrapperClass = new("--wrapper-class");
    public static readonly Option<string> ConnectionName = new("--connection-name");
    public static readonly Option<string> Language = new("--language") { Description = "csharp|vb" };

    public static readonly Option<string[]> Schema = new("--schema") { Arity = ArgumentArity.ZeroOrMore };
    public static readonly Option<string[]> Table = new("--table") { Arity = ArgumentArity.ZeroOrMore };
    public static readonly Option<string[]> IgnoreTable = new("--ignore-table") { Arity = ArgumentArity.ZeroOrMore };
    public static readonly Option<string[]> IgnoreColumn = new("--ignore-column") { Arity = ArgumentArity.ZeroOrMore };

    public static readonly Option<bool> PartialClasses = Flag("--partial-classes");
    public static readonly Option<bool> VirtualProperties = Flag("--virtual-properties");
    public static readonly Option<bool> PrivateFieldsAtTop = Flag("--private-fields-at-top");
    public static readonly Option<bool> TypedAdapters = Flag("--typed-adapters");
    public static readonly Option<bool> Relations = Flag("--relations");
    public static readonly Option<bool> Views = Flag("--views");
    public static readonly Option<bool> SystemTables = Flag("--system-tables");
    public static readonly Option<bool> Singularize = Flag("--singularize");
    public static readonly Option<bool> StripClassName = Flag("--strip-class-name");
    public static readonly Option<bool> GeneratedSuffix = Flag("--generated-suffix");
    public static readonly Option<bool> Json = Flag("--json");
    public static readonly Option<bool> DryRun = Flag("--dry-run");

    public static readonly Option<string> FieldKeyword = new("--field-keyword") { Description = "true|false|auto" };
    public static readonly Option<string> NullableRefs = new("--nullable-refs") { Description = "true|false|auto" };
    public static readonly Option<string> NamingStyle = new("--naming-style") { Description = "pascal|preserve" };
    public static readonly Option<string> Collections = new("--collections") { Description = "none|generic|dataclass|bindable" };
    public static readonly Option<string> SchemaDisambiguation = new("--schema-disambiguation")
    { Description = "none|prefix|namespace" };

    private static Option<bool> Flag(string name)
        => new(name) { Arity = ArgumentArity.ZeroOrOne, DefaultValueFactory = _ => true };

    /// <summary>Maps each string-valued option to the configuration key it overrides.</summary>
    private static readonly (Option<string> Option, string Key)[] StringMap =
    [
        (Provider, "Provider"), (Connection, "ConnectionString"), (Namespace, "Namespace"),
        (Language, "Language"), (SchemaDisambiguation, "SchemaDisambiguation"),
        (Out, "Output:Entities"), (WrapperOut, "Output:Wrapper"),
        (WrapperClass, "Wrapper:ClassName"), (ConnectionName, "Wrapper:ConnectionName"),
        (NamingStyle, "Naming:Style"),
        (FieldKeyword, "Emit:FieldKeyword"), (NullableRefs, "Emit:NullableRefs"),
        (Collections, "Emit:Collections"),
    ];

    /// <summary>Maps each boolean-valued option to the configuration key it overrides.</summary>
    private static readonly (Option<bool> Option, string Key)[] BoolMap =
    [
        (GeneratedSuffix, "Output:GeneratedSuffix"),
        (Singularize, "Naming:Singularize"), (StripClassName, "Naming:StripClassName"),
        (PartialClasses, "Emit:PartialClasses"), (PrivateFieldsAtTop, "Emit:PrivateFieldsAtTop"),
        (VirtualProperties, "Emit:VirtualProperties"),
        (TypedAdapters, "Emit:TypedAdapters"), (Relations, "Emit:Relations"),
        (Views, "Emit:Views"), (SystemTables, "Emit:SystemTables"),
    ];

    private static readonly (Option<string[]> Option, string Key)[] ListMap =
    [
        (Schema, "Schemas"), (Table, "Include:Tables"),
        (IgnoreTable, "Ignore:Tables"), (IgnoreColumn, "Ignore:Columns"),
    ];

    /// <summary>
    /// Returns only options the caller actually typed. Unspecified options must not enter the
    /// configuration stack, or parser defaults would mask JSON and environment values.
    /// </summary>
    /// <remarks>
    /// <see cref="ParseResult.GetResult(Option)"/> returns a non-null result for an option that
    /// was never typed as soon as that option carries a <c>DefaultValueFactory</c> (every
    /// boolean flag here does, so `--dry-run` alone means true) — the XML docs for
    /// <c>GetResult</c> say null comes back only "if it was not provided and no default was
    /// configured". So <c>is null</c> alone cannot tell "typed" from "defaulted" for those
    /// options. <see cref="OptionResult.Implicit"/> is the actual signal: it is true exactly
    /// when the result was synthesized from a default rather than from a token on the command
    /// line. Every option below is checked the same way for consistency, even the ones that
    /// have no default and so would already return null when unspecified.
    /// </remarks>
    public static IDictionary<string, string?> ToConfigurationValues(ParseResult result)
    {
        var values = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase);

        foreach ((Option<string> option, string key) in StringMap)
        {
            if (!WasSpecified(result, option)) continue;
            values[key] = result.GetValue(option);
        }

        foreach ((Option<bool> option, string key) in BoolMap)
        {
            if (!WasSpecified(result, option)) continue;
            values[key] = result.GetValue(option) ? "true" : "false";
        }

        // Repeatable list options deliberately do NOT go in here; see ToListValues.
        return values;
    }

    /// <summary>
    /// Returns the repeatable list options the caller actually typed, keyed by the configuration
    /// key each one owns.
    /// </summary>
    /// <remarks>
    /// Deliberately a separate channel from <see cref="ToConfigurationValues"/> rather than more
    /// entries in the same dictionary. Lists used to be written into it as indexed keys
    /// (<c>Ignore:Tables:0</c>) and layered over the JSON file as an in-memory provider — but
    /// <see cref="Microsoft.Extensions.Configuration.IConfiguration"/> resolves an array as the
    /// *union of child indices across providers*, not last-provider-wins on the array as a whole.
    /// A one-element command-line list therefore overwrote index 0 and left the JSON file's
    /// indices 1..n in place: <c>--table delta</c> against <c>"tables": ["alpha","beta"]</c>
    /// generated delta *and* beta, and <c>--ignore-table x</c> silently un-ignored whatever sat at
    /// index 0. The spec is explicit that the command line overrides the config file, so
    /// <see cref="Config.ConfigurationLoader"/> takes these separately and replaces the bound list
    /// outright.
    /// <para>
    /// An option typed with no values at all (bare <c>--table</c>, legal at
    /// <see cref="ArgumentArity.ZeroOrMore"/>) is omitted, so it continues to mean "not specified"
    /// rather than "generate nothing" — nobody types a bare <c>--table</c> intending to clear the
    /// project's configured list, and reading it that way would turn a typo into an empty run.
    /// </para>
    /// </remarks>
    public static IDictionary<string, IReadOnlyList<string>> ToListValues(ParseResult result)
    {
        var lists = new Dictionary<string, IReadOnlyList<string>>(StringComparer.OrdinalIgnoreCase);

        foreach ((Option<string[]> option, string key) in ListMap)
        {
            if (!WasSpecified(result, option)) continue;

            string[] items = result.GetValue(option) ?? [];
            if (items.Length == 0) continue;

            lists[key] = items;
        }

        return lists;
    }

    /// <summary>
    /// True only when the caller actually typed this option on the command line — false both
    /// when it is entirely absent and when its value came from a <c>DefaultValueFactory</c>.
    /// Exposed (not just used internally by <see cref="ToConfigurationValues"/>) because
    /// <c>--json</c> and <c>--dry-run</c> are command-level switches that never enter the
    /// configuration stack yet need the identical "typed vs. defaulted" distinction: both use
    /// <c>Flag()</c>'s <c>DefaultValueFactory = _ =&gt; true</c> so a bare <c>--dry-run</c>
    /// means true, which means <see cref="ParseResult.GetValue{T}(Option{T})"/> alone would
    /// return true even when the option was never typed at all. The caller must gate the
    /// read with this method and treat "not specified" as false for those two switches.
    /// </summary>
    public static bool WasSpecified(ParseResult result, Option option)
    {
        OptionResult? optionResult = result.GetResult(option);
        return optionResult is not null && !optionResult.Implicit;
    }

    public static IEnumerable<Option> All()
    {
        foreach ((Option option, _) in StringMap) yield return option;
        foreach ((Option option, _) in BoolMap) yield return option;
        foreach ((Option option, _) in ListMap) yield return option;
        yield return ConfigFile;
        yield return Json;
        yield return DryRun;
    }
}
