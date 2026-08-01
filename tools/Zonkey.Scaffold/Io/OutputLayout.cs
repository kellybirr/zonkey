using Zonkey.Scaffold.Options;

namespace Zonkey.Scaffold.Io;

/// <summary>
/// Where every generated file lands: one resolution of <c>output.entities</c>,
/// <c>output.wrapper</c> and <c>output.generatedSuffix</c> into absolute paths, computed once per
/// run and used by everything that needs to know a path.
/// </summary>
/// <remarks>
/// It exists because the two things that must agree about a path — the writer that creates the
/// file and the check that refuses two files competing for one name — used to derive it
/// separately, and in different currencies. The writer joined resolved directories; the check
/// asked whether the option <em>string</em> <c>output.wrapper</c> was empty. So
/// <c>--out-entities ./Entities --out-wrapper ./Entities</c> named one directory in two spellings:
/// the writer put both files in it, and the check concluded the wrapper had a directory of its own
/// and excluded it. A path is now only ever compared to another path, and only ever to one this
/// type produced.
/// <para>
/// Paths are compared as whole file paths rather than as directory + name, so the spellings
/// <c>Entities</c> and <c>./Entities/</c> (which <see cref="Path.GetFullPath(string, string)"/>
/// preserves the trailing separator of) cannot be mistaken for two directories:
/// <see cref="Path.Combine(string, string)"/> normalizes both to the same string.
/// </para>
/// </remarks>
public sealed class OutputLayout
{
    private readonly string _suffix;

    private OutputLayout(string entitiesDirectory, string wrapperDirectory, string suffix)
    {
        EntitiesDirectory = entitiesDirectory;
        WrapperDirectory = wrapperDirectory;
        _suffix = suffix;
    }

    public string EntitiesDirectory { get; }

    /// <summary>
    /// The entities' directory unless <c>output.wrapper</c> named one — resolved, so "did the
    /// caller give the wrapper a directory of its own?" is answered by comparing this with
    /// <see cref="EntitiesDirectory"/> rather than by inspecting an option string.
    /// </summary>
    public string WrapperDirectory { get; }

    public static OutputLayout Resolve(ScaffoldOptions options, string workingDirectory)
    {
        string root = string.IsNullOrWhiteSpace(workingDirectory)
            ? Directory.GetCurrentDirectory()
            : workingDirectory;

        string entities = Path.GetFullPath(options.Output.Entities, root);

        string wrapper = string.IsNullOrWhiteSpace(options.Output.Wrapper)
            ? entities
            : Path.GetFullPath(options.Output.Wrapper, root);

        return new OutputLayout(
            entities, wrapper, options.Output.GeneratedSuffix ? ".g.cs" : ".cs");
    }

    public string EntityPath(string className) => Path.Combine(EntitiesDirectory, className + _suffix);

    public string WrapperPath(string className) => Path.Combine(WrapperDirectory, className + _suffix);
}
