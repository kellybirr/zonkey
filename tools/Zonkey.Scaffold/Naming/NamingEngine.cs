using System.Text;
using Zonkey.Scaffold.Diagnostics;
using Zonkey.Scaffold.Options;
using Zonkey.Scaffold.Schema;

namespace Zonkey.Scaffold.Naming;

public sealed class NamingEngine
{
    private readonly NamingOptions _naming;
    private readonly Inflector _inflector;

    /// <summary>The values <c>naming.style</c> / <c>--Naming:Style</c> may take.</summary>
    private static readonly string[] Styles = ["pascal", "preserve"];

    public NamingEngine(NamingOptions naming)
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
        _inflector = new Inflector(naming.Irregulars);
    }

    public string ClassNameFor(TableInfo table)
    {
        string stem = _naming.Singularize ? _inflector.Singularize(table.Name) : table.Name;

        string core = _naming.Style.Equals("preserve", StringComparison.OrdinalIgnoreCase)
            ? stem
            : ToPascalCase(stem);

        return Identifier(core, ClassSubject(table));
    }

    public string PropertyNameFor(TableInfo table, ColumnInfo column)
    {
        string name = _naming.Style.Equals("preserve", StringComparison.OrdinalIgnoreCase)
            ? column.Name
            : ToPascalCase(column.Name);

        return Identifier(name, PropertySubject(table, column));
    }

    private static (string Source, string Remedy) ClassSubject(TableInfo table)
        => ($"Table '{table.QualifiedName}'", "Rename the table, or rename the class afterwards.");

    private static (string Source, string Remedy) PropertySubject(TableInfo table, ColumnInfo column)
        => ($"Column '{table.QualifiedName}.{column.Name}'",
            "Rename the column, or rename the property afterwards.");

    /// <summary>
    /// The C# reserved keywords — the words that are never legal as a bare identifier.
    /// </summary>
    /// <remarks>
    /// A literal set rather than <c>SyntaxFacts</c>, to keep Roslyn out of a CLI tool for one
    /// lookup. Excludes contextual keywords (<c>value</c>, <c>record</c>, <c>field</c>, …): those
    /// are legal identifiers. Ordinal because C# keywords are lower-case, which is also why
    /// PascalCasing hides most of this — <c>Class</c> is simply not a keyword.
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
    /// Applied once here rather than in each emitter, so a second emitter cannot forget.
    /// Idempotent — <c>@lock</c> is not itself in the set. Mostly reachable via
    /// <c>Naming:Style=preserve</c>, since PascalCasing capitalizes the first letter.
    /// </remarks>
    public static string EscapeKeyword(string identifier)
        => ReservedKeywords.Contains(identifier) ? "@" + identifier : identifier;

    /// <summary>
    /// Escapes a keyword and then refuses anything that still is not an identifier, naming the
    /// table or column it came from.
    /// </summary>
    /// <remarks>
    /// <c>Naming:Style=preserve</c> hands the raw schema name through, and a quoted SQL identifier
    /// can be any text at all (<c>"my col"</c>, <c>"1st"</c>, <c>"@"</c>). Refusing here, where the
    /// table and column are still in hand, gives one named error instead of a broken emit.
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
    /// A namespace is a list of identifiers, not one: <c>Acme.lock.Data</c> must become
    /// <c>Acme.@lock.Data</c>, not <c>@Acme.lock.Data</c>.
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
    /// A leading <c>@</c> is skipped first. Slightly stricter than the language, which also admits
    /// connector and combining characters — anything rejected produces a named error.
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
