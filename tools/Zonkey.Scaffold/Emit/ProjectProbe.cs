using System.Xml.Linq;

namespace Zonkey.Scaffold.Emit;

public sealed class ProjectCapabilities
{
    public string? ProjectPath { get; init; }
    public string? LangVersion { get; init; }
    public string? TargetFramework { get; init; }
    public bool SupportsFieldKeyword { get; init; }
    public bool NullableEnabled { get; init; }
}

/// <summary>
/// Resolves what the target project can actually compile, so tri-state options can mean
/// "auto" rather than forcing the caller to know. Every failure path is conservative:
/// unknown means "assume not supported", because emitting code the project cannot compile
/// is far worse than emitting the older form.
/// </summary>
public static class ProjectProbe
{
    /// <summary>C# 14 introduced the <c>field</c> keyword; .NET 10 defaults to it.</summary>
    private const int FieldKeywordLangVersion = 14;

    public static ProjectCapabilities Probe(string startDirectory)
    {
        string? projectPath = FindNearestProject(startDirectory);
        if (projectPath is null) return new ProjectCapabilities();

        XDocument doc;
        try
        {
            doc = XDocument.Load(projectPath);
        }
        catch (Exception)
        {
            // An unreadable or malformed project is not fatal — fall back to the safe form.
            return new ProjectCapabilities { ProjectPath = projectPath };
        }

        string? langVersion = Property(doc, "LangVersion");
        string? targetFrameworkSingle = Property(doc, "TargetFramework");
        string[] targetFrameworksMulti = Property(doc, "TargetFrameworks")
            ?.Split(';').Select(s => s.Trim()).Where(s => s.Length > 0).ToArray() ?? [];
        string? tfm = targetFrameworkSingle ?? targetFrameworksMulti.FirstOrDefault();
        bool nullable = string.Equals(Property(doc, "Nullable"), "enable", StringComparison.OrdinalIgnoreCase);

        bool isVb = projectPath.EndsWith(".vbproj", StringComparison.OrdinalIgnoreCase);

        // A single <TargetFramework> is one entry; <TargetFrameworks> may list several.
        // Every entry must independently support the field keyword — a multi-targeted
        // project that includes even one legacy TFM cannot use it project-wide, since the
        // emitted code has to compile for all targets.
        string[] tfmEntries = targetFrameworkSingle is not null
            ? [targetFrameworkSingle]
            : targetFrameworksMulti;

        return new ProjectCapabilities
        {
            ProjectPath = projectPath,
            LangVersion = langVersion,
            TargetFramework = tfm,
            NullableEnabled = nullable,
            SupportsFieldKeyword = !isVb && SupportsField(langVersion, tfmEntries)
        };
    }

    private static bool SupportsField(string? langVersion, IReadOnlyList<string> tfmEntries)
    {
        if (!string.IsNullOrWhiteSpace(langVersion))
        {
            if (langVersion.Equals("latest", StringComparison.OrdinalIgnoreCase) ||
                langVersion.Equals("preview", StringComparison.OrdinalIgnoreCase) ||
                langVersion.Equals("latestMajor", StringComparison.OrdinalIgnoreCase))
                return true;

            // "14.0" or "14"
            string major = langVersion.Split('.')[0];
            return int.TryParse(major, out int v) && v >= FieldKeywordLangVersion;
        }

        // No explicit LangVersion: the SDK picks the default for the target framework.
        // An empty or unparseable list is unknown, which is conservatively "not supported".
        // For a multi-targeted project, EVERY entry must independently support it, since the
        // emitted code has to compile under all of the project's targets.
        if (tfmEntries.Count == 0) return false;
        return tfmEntries.All(SupportsFieldForSingleTfm);
    }

    private static bool SupportsFieldForSingleTfm(string tfm)
    {
        // Only dotted modern monikers (net10.0, net10.0-windows, ...) default to C# 14;
        // legacy forms like net48 or netstandard2.0 must not.
        if (string.IsNullOrWhiteSpace(tfm)) return false;
        if (!tfm.StartsWith("net", StringComparison.OrdinalIgnoreCase)) return false;

        // Strip the "net" prefix and any platform suffix (e.g. "-windows"), leaving the
        // version portion, e.g. "net10.0-windows" -> "10.0".
        string versionPart = tfm[3..];
        int dashIndex = versionPart.IndexOf('-');
        if (dashIndex >= 0) versionPart = versionPart[..dashIndex];

        // net48 has no dot ("48") and netstandard2.0 has no numeric major ("standard2");
        // both must fail here rather than accidentally parsing as a modern version.
        if (!versionPart.Contains('.')) return false;

        string majorPart = versionPart.Split('.')[0];
        return int.TryParse(majorPart, out int tfmMajor) && tfmMajor >= 10;
    }

    private static string? Property(XDocument doc, string name) => doc
        .Descendants()
        .Where(e => e.Name.LocalName.Equals(name, StringComparison.OrdinalIgnoreCase))
        .Select(e => e.Value.Trim())
        .FirstOrDefault(v => v.Length > 0);

    private static string? FindNearestProject(string startDirectory)
    {
        var dir = new DirectoryInfo(Path.GetFullPath(startDirectory));

        // The directory being probed is very often the scaffold output directory, which commonly
        // does not exist yet — that's the normal case for `generate`/`inspect` targeting a fresh
        // --out folder. Directory.EnumerateFiles throws DirectoryNotFoundException on a directory
        // that isn't there, so walk up to the nearest ancestor that does exist before enumerating,
        // rather than treating "not created yet" as a crash.
        while (dir is not null && !dir.Exists) dir = dir.Parent;

        while (dir is not null)
        {
            string? found = Directory
                .EnumerateFiles(dir.FullName)
                .Where(f => f.EndsWith(".csproj", StringComparison.OrdinalIgnoreCase)
                         || f.EndsWith(".vbproj", StringComparison.OrdinalIgnoreCase))
                .OrderBy(f => f, StringComparer.Ordinal)
                .FirstOrDefault();

            if (found is not null) return found;
            dir = dir.Parent;
        }

        return null;
    }
}
