using System.Reflection;
using Zonkey.Scaffold.Diagnostics;

namespace Zonkey.Scaffold;

/// <summary>
/// Writes the bundled agent skill into a project, so an agent working there loads it automatically.
/// </summary>
public static class SkillInstaller
{
    private const string ResourceName = "Zonkey.Scaffold.SKILL.md";
    private const string RelativePath = ".claude/skills/zonkey-scaffold/SKILL.md";

    public static int Install(string workingDirectory, string? targetDirectory)
    {
        string path = Path.GetFullPath(
            targetDirectory is null ? RelativePath : Path.Combine(targetDirectory, "SKILL.md"),
            workingDirectory);

        Directory.CreateDirectory(Path.GetDirectoryName(path)!);

        bool existed = File.Exists(path);
        File.WriteAllText(path, Read());

        Console.WriteLine($"{(existed ? "Updated" : "Wrote")} {path}");
        return 0;
    }

    private static string Read()
    {
        using Stream? stream = Assembly.GetExecutingAssembly().GetManifestResourceStream(ResourceName);

        if (stream is null)
            throw new ScaffoldException($"The skill resource '{ResourceName}' is missing from this build.");

        using var reader = new StreamReader(stream);
        return reader.ReadToEnd();
    }
}
