using System.Text;
using Zonkey.Scaffold.Diagnostics;
using Zonkey.Scaffold.Options;
using Zonkey.Scaffold.Schema;

namespace Zonkey.Scaffold.Naming;

public sealed class NamingEngine
{
    private readonly NamingOptions _naming;
    private readonly OverrideOptions _overrides;
    private readonly Inflector _inflector;

    /// <summary>The values <c>naming.style</c> / <c>--naming-style</c> may take.</summary>
    private static readonly string[] Styles = ["pascal", "preserve"];

    public NamingEngine(NamingOptions naming, OverrideOptions overrides)
    {
        // The style is read as `Equals("preserve") ? … : PascalCase`, so anything that is not
        // "preserve" — including "camel", "snake" or a typo — silently became PascalCase. That is
        // the same defect class as an unvalidated `dbType` (a config string naming a member of a
        // closed set, accepted without checking it is one) and the same failure mode the tool
        // refuses everywhere else: an explicitly specified option quietly doing something other
        // than what was asked.
        if (!Styles.Contains(naming.Style?.Trim() ?? "", StringComparer.OrdinalIgnoreCase))
        {
            throw new ScaffoldException(
                $"naming.style = '{naming.Style}' (--naming-style) is not a naming style. " +
                $"Valid values: {string.Join(", ", Styles)}.");
        }

        _naming = naming;
        _overrides = overrides;
        _inflector = new Inflector(naming.Irregulars);
    }

    public string ClassNameFor(TableInfo table, ICollection<ScaffoldWarning> warnings)
    {
        if (FindTableOverride(table)?.ClassName is { Length: > 0 } forced)
            return Identifier(forced, ClassSubject(table));

        string stem = table.Name;

        if (_naming.Singularize)
        {
            string singular = _inflector.Singularize(stem);

            if (_inflector.IsUncertain(stem, singular))
            {
                warnings.Add(ScaffoldWarning.For(
                    WarningCode.InflectionUncertain,
                    $"Table '{table.QualifiedName}' singularized to '{singular}'. " +
                    $"Set naming.irregulars or overrides.tables.{table.Name}.className if that is wrong.",
                    table: table.QualifiedName));
            }

            stem = singular;
        }

        string core = _naming.Style.Equals("preserve", StringComparison.OrdinalIgnoreCase)
            ? stem
            : ToPascalCase(stem);

        return Identifier(_naming.ClassPrefix + core + _naming.ClassSuffix, ClassSubject(table));
    }

    public string PropertyNameFor(TableInfo table, ColumnInfo column, string className)
    {
        if (FindTableOverride(table) is { } to &&
            to.Columns.TryGetValue(column.Name, out ColumnOverride? co) &&
            co.Property is { Length: > 0 } forced)
        {
            return Identifier(forced, PropertySubject(table, column));
        }

        string name = _naming.Style.Equals("preserve", StringComparison.OrdinalIgnoreCase)
            ? column.Name
            : ToPascalCase(column.Name);

        if (_naming.StripClassName)
            name = Strip(name, className);

        return Identifier(name, PropertySubject(table, column));
    }

    private static (string Source, string Remedy) ClassSubject(TableInfo table)
        => ($"Table '{table.QualifiedName}'",
            $"Set overrides.tables.{table.Name}.className to name the class explicitly.");

    private static (string Source, string Remedy) PropertySubject(TableInfo table, ColumnInfo column)
        => ($"Column '{table.QualifiedName}.{column.Name}'",
            $"Set overrides.tables.{table.Name}.columns.{column.Name}.property to name the " +
            "property explicitly.");

    /// <summary>
    /// Removes a leading or trailing occurrence of the class name, but never returns an empty
    /// string — a column literally named after its table keeps its own name.
    /// </summary>
    private static string Strip(string name, string className)
    {
        if (string.Equals(name, className, StringComparison.Ordinal)) return name;

        if (name.Length > className.Length &&
            name.StartsWith(className, StringComparison.Ordinal))
            return name[className.Length..];

        if (name.Length > className.Length &&
            name.EndsWith(className, StringComparison.Ordinal))
            return name[..^className.Length];

        return name;
    }

    private TableOverride? FindTableOverride(TableInfo table)
    {
        if (_overrides.Tables.TryGetValue(table.QualifiedName, out TableOverride? qualified))
            return qualified;

        return _overrides.Tables.TryGetValue(table.Name, out TableOverride? bare) ? bare : null;
    }

    /// <summary>
    /// The C# reserved keywords — the words that are never legal as a bare identifier.
    /// </summary>
    /// <remarks>
    /// A literal set rather than <c>SyntaxFacts.GetKeywordKind</c>, to keep a whole Roslyn
    /// dependency out of a CLI tool for one lookup. Deliberately excludes contextual keywords
    /// (<c>value</c>, <c>record</c>, <c>nameof</c>, <c>var</c>, <c>async</c>, <c>await</c>,
    /// <c>from</c>, <c>where</c>, <c>field</c>, …): those are legal identifiers, and escaping
    /// them would put an <c>@</c> in front of every <c>value</c> column in every settings and
    /// audit table for no reason. Ordinal comparison because C# keywords are case-sensitive and
    /// lower-case, which is also why PascalCasing hides most of this problem — <c>Class</c> is
    /// simply not a keyword.
    /// </remarks>
    private static readonly HashSet<string> ReservedKeywords = new(StringComparer.Ordinal)
    {
        "abstract", "as", "base", "bool", "break", "byte", "case", "catch", "char", "checked",
        "class", "const", "continue", "decimal", "default", "delegate", "do", "double", "else",
        "enum", "event", "explicit", "extern", "false", "finally", "fixed", "float", "for",
        "foreach", "goto", "if", "implicit", "in", "int", "interface", "internal", "is", "lock",
        "long", "namespace", "new", "null", "object", "operator", "out", "override", "params",
        "private", "protected", "public", "readonly", "ref", "return", "sbyte", "sealed", "short",
        "sizeof", "stackalloc", "static", "string", "struct", "switch", "this", "throw", "true",
        "try", "typeof", "uint", "ulong", "unchecked", "unsafe", "ushort", "using", "virtual",
        "void", "volatile", "while",
    };

    /// <summary>
    /// Prefixes <c>@</c> when the finished identifier is a reserved word, so a table named
    /// <c>event</c> or a column named <c>lock</c> yields source that compiles.
    /// </summary>
    /// <remarks>
    /// Applied here, at the point identifiers are produced, rather than in each emitter: the
    /// emitters take an identifier and are entitled to assume it is one, and doing it once here
    /// means a second emitter cannot forget. Naturally idempotent — <c>@lock</c> is not itself in
    /// the set — so an override that already spells the escape is left alone. Only reachable in
    /// practice via <c>--naming-style preserve</c> and via explicit overrides, because
    /// PascalCasing capitalizes the first letter and no C# keyword is capitalized; that is
    /// precisely why the gap went unnoticed.
    /// <para>
    /// Not every identifier in the output is derived from a table or column, so this is public:
    /// <c>ScaffoldPipeline</c> routes the wrapper class name (through
    /// <see cref="Identifier(string, string, string)"/>), the namespace (through
    /// <see cref="EscapeNamespace"/>) and the wrapper's pluralized adapter property names through
    /// the same rules. Those three were the sites the first keyword fix missed, and each was a
    /// reachable compile break: <c>--wrapper-class lock</c>, <c>--namespace Acme.lock.Data</c>,
    /// and a table named <c>param</c> whose plural is <c>params</c>.
    /// </para>
    /// </remarks>
    public static string EscapeKeyword(string identifier)
        => ReservedKeywords.Contains(identifier) ? "@" + identifier : identifier;

    /// <summary>
    /// Escapes a keyword and then refuses anything that still is not an identifier, naming the
    /// table or column it came from.
    /// </summary>
    /// <remarks>
    /// <c>--naming-style preserve</c> hands the raw schema name straight through, and a quoted SQL
    /// identifier can be any text at all: <c>"my col"</c>, <c>"1st"</c>, <c>"total$"</c>,
    /// <c>"@"</c>. Every one of those used to reach an emitter and produce source that does not
    /// compile, and <c>"@"</c> additionally crashed the tool while the emitter derived
    /// <c>"_" + name[0]</c> from it — exit 2, "unexpected error", for an input the tool could have
    /// named precisely. Refusing here, where the table and column are still in hand, turns all of
    /// them into one actionable message. Repairing them instead was rejected: any repair is a
    /// guess at what the caller wanted to call the member, and an override says it exactly.
    /// </remarks>
    public static string Identifier(string candidate, string source, string remedy)
    {
        string escaped = EscapeKeyword(candidate);

        if (IsIdentifier(escaped)) return escaped;

        throw new ScaffoldException(
            $"{source} yields the name '{candidate}', which is not a valid C# identifier. " + remedy);
    }

    private static string Identifier(string candidate, (string Source, string Remedy) subject)
        => Identifier(candidate, subject.Source, subject.Remedy);

    /// <summary>
    /// Escapes each dot-separated segment of a namespace.
    /// </summary>
    /// <remarks>
    /// A namespace is a list of identifiers, not one identifier, so it needs its own entry point:
    /// <c>--namespace Acme.lock.Data</c> must become <c>Acme.@lock.Data</c> and not
    /// <c>@Acme.lock.Data</c>. It is computed once (see <c>ScaffoldPlan.Namespace</c>) because
    /// the entity files and the wrapper must agree on the spelling — the wrapper names the entity
    /// types, so two spellings do not compile.
    /// </remarks>
    public static string? EscapeNamespace(string? ns)
    {
        if (string.IsNullOrWhiteSpace(ns)) return ns;

        return string.Join('.', ns.Split('.').Select(segment =>
        {
            string escaped = EscapeKeyword(segment);

            if (!IsIdentifier(escaped))
                throw new ScaffoldException(
                    $"Namespace '{ns}' (--namespace) has the segment '{segment}', which is not a " +
                    "valid C# identifier.");

            return escaped;
        }));
    }

    /// <summary>
    /// True when the text is usable as written in emitted source.
    /// </summary>
    /// <remarks>
    /// A leading <c>@</c> is the verbatim-identifier escape and is skipped before the check. The
    /// letter test is <see cref="char.IsLetter(char)"/> rather than an ASCII range because C#
    /// identifiers are Unicode; the rule is deliberately a little stricter than the language
    /// (which also admits connector, combining and formatting characters), since anything it
    /// rejects produces a *named* error rather than a silent miscompile.
    /// </remarks>
    private static bool IsIdentifier(string text)
    {
        string name = text.StartsWith('@') ? text[1..] : text;

        if (name.Length == 0) return false;
        if (!char.IsLetter(name[0]) && name[0] != '_') return false;

        foreach (char c in name)
            if (!char.IsLetterOrDigit(c) && c != '_') return false;

        return true;
    }

    /// <summary>
    /// True when the type name would raise CS8981 ("only contains lower-cased ascii characters",
    /// a name the language may reserve). Verbatim identifiers are exempt, which is why the
    /// <c>@</c> is not stripped first. Used by the emitters to decide whether the generated file
    /// needs to say it accepts that risk.
    /// </summary>
    public static bool IsAllLowerCaseAscii(string name)
    {
        if (name.Length == 0) return false;

        foreach (char c in name)
            if (c is < 'a' or > 'z') return false;

        return true;
    }

    /// <summary>
    /// snake_case / worm_case to PascalCase, leaving already-Pascal input intact so
    /// SQL Server's <c>CustomerID</c> survives as <c>CustomerID</c> rather than becoming
    /// <c>Customerid</c>.
    /// </summary>
    public static string ToPascalCase(string raw)
    {
        if (string.IsNullOrEmpty(raw)) return raw;

        var sb = new StringBuilder(raw.Length);
        bool capitalizeNext = true;

        foreach (char c in raw)
        {
            if (c is '_' or ' ' or '-')
            {
                capitalizeNext = true;
            }
            else if (capitalizeNext)
            {
                sb.Append(char.ToUpperInvariant(c));
                capitalizeNext = false;
            }
            else
            {
                sb.Append(c);
            }
        }

        // Input that was entirely separators (e.g. "_", "__", "-", " ", "_-_") appends nothing.
        // Fall back to a valid identifier rather than emitting an empty class/property name.
        // Checked before the digit check so that check never sees an empty builder.
        if (sb.Length == 0)
            return "_";

        if (char.IsDigit(sb[0]))
            sb.Insert(0, '_');

        return sb.ToString();
    }
}
