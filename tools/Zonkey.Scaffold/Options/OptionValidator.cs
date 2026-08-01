using Zonkey.Scaffold.Diagnostics;

namespace Zonkey.Scaffold.Options;

/// <summary>
/// Refuses options that this release binds and persists but cannot act on.
/// </summary>
/// <remarks>
/// The switches below are deliberately still on the command-line surface, and
/// <see cref="Commands.InitCommand"/> deliberately still writes their keys into the config file:
/// later releases implement them, and removing them now would churn every caller's config twice.
/// But bound-and-ignored is the worse failure: <c>generate --language vb</c> exited 0 and wrote
/// C#, which is exactly the outcome the spec singles out —
/// <i>"silently ignoring an explicit option is the failure mode that leaves an agent believing it
/// got output it did not get"</i>. So an option asking for behavior this build does not have is a
/// refusal that names the limitation, and an option sitting at its default stays silent.
/// <para>
/// The check is on the <em>value</em> rather than on whether the token appeared on the command
/// line, and that is the deliberate choice here. A value that differs from the default is a
/// request this build cannot serve no matter which layer it arrived from, so this catches
/// <c>"language": "vb"</c> committed in <c>zonkey.scaffold.config.json</c> — the layer an agent is
/// most likely to have written it into, and the one a parser-level "was it typed?" check cannot
/// see. The converse holds too: <c>--language csharp</c> is explicitly typed but costs nothing to
/// honour, so it passes, which keeps the file <c>init</c> writes loadable by the tool that wrote
/// it.
/// </para>
/// <para>
/// The list is the result of a sweep of every property on <see cref="ScaffoldOptions"/> against
/// its consumers, not of the examples a review happened to name: the first pass covered the five
/// options that were reported and missed <c>emit.systemTables</c>, <c>overrides…dbType</c> and
/// <c>connectionStrings</c>. Only the first belongs here. The other two were unread because they
/// were <em>unimplemented</em>, not because they were unimplementable — <c>dbType</c> is now
/// honoured by <c>ScaffoldPipeline.ForcedDbType</c>, and <c>connectionStrings</c> is a feature the
/// design spec requires (<c>ConnectionStrings:Zonkey</c>), now resolved in
/// <c>ScaffoldPipeline.Build</c>. That distinction is the whole test for membership of this list:
/// refusal is for what this release will not do, never for what it has simply not got round to,
/// because refusing the latter rejects config files that are correct. Each entry here is deleted
/// by the release that implements the option; the coupling is real and deliberate.
/// </para>
/// <para>
/// Every violation is collected before throwing, for the same reason
/// <c>ScaffoldPipeline.DetectCollisions</c> does it: a caller fixing a config file should see the
/// whole list once, not discover it one run at a time.
/// </para>
/// </remarks>
public static class OptionValidator
{
    public static void Validate(ScaffoldOptions options)
    {
        var problems = new List<string>();

        if (!Is(options.Language, "csharp"))
        {
            problems.Add(
                $"language = '{options.Language}' (--language): this release emits C# only. " +
                "Set it to 'csharp' or omit the option; VB emission arrives with the VB emitter.");
        }

        if (!Is(options.SchemaDisambiguation, "none"))
        {
            problems.Add(
                $"schemaDisambiguation = '{options.SchemaDisambiguation}' " +
                "(--schema-disambiguation): this release generates from one schema at a time and " +
                "never disambiguates. Set it to 'none' or omit the option; 'prefix' and " +
                "'namespace' arrive with multi-schema support.");
        }

        if (!Is(options.Emit.Collections, "none"))
        {
            problems.Add(
                $"emit.collections = '{options.Emit.Collections}' (--collections): this release " +
                "emits scalar properties only. Set it to 'none' or omit the option; collection " +
                "properties arrive with relation emission.");
        }

        if (options.Emit.TypedAdapters)
        {
            problems.Add(
                "emit.typedAdapters = true (--typed-adapters): this release emits a " +
                "DatabaseWrapper exposing DataClassAdapter<T> properties and no per-entity " +
                "adapter types. Set it to false or omit the option.");
        }

        if (options.Emit.Relations)
        {
            problems.Add(
                "emit.relations = true (--relations): this release emits no foreign-key " +
                "navigation members. Set it to false or omit the option; foreign keys are read " +
                "and reported by `inspect` today, but nothing is emitted from them.");
        }

        if (options.Emit.SystemTables)
        {
            problems.Add(
                "emit.systemTables = true (--system-tables): this release never reads system " +
                "tables — the SQLite reader filters 'sqlite_%' unconditionally — so the setting " +
                "changes nothing. Set it to false or omit the option.");
        }

        if (problems.Count == 0) return;

        throw new ScaffoldException(
            $"{problems.Count} option(s) requested behavior this release does not implement:\n" +
            string.Join("\n", problems.Select(p => "  - " + p)) + "\n" +
            "These options are accepted by the parser because later releases implement them; " +
            "until then they are refused rather than silently ignored.");
    }

    private static bool Is(string? value, string expected)
        => string.IsNullOrWhiteSpace(value)
        || string.Equals(value.Trim(), expected, StringComparison.OrdinalIgnoreCase);
}
