using Humanizer;

namespace Zonkey.Scaffold.Naming;

/// <summary>
/// Wraps Humanizer so inflection is a real dictionary-backed operation rather than a trailing-s
/// trim. Two guards sit on top: caller-supplied irregulars (Humanizer confidently gets
/// taxes -> Taxis, data -> Datum, media -> Medium wrong), and an uncertainty check so the
/// non-obvious calls can be surfaced as warnings instead of shipped silently.
/// </summary>
public sealed class Inflector
{
    private readonly Dictionary<string, string> _irregulars;

    public Inflector(IDictionary<string, string> irregulars)
        => _irregulars = new Dictionary<string, string>(irregulars, StringComparer.OrdinalIgnoreCase);

    public string Singularize(string name) => Transform(name, token =>
        _irregulars.TryGetValue(token, out string? forced)
            ? forced
            // inputIsKnownToBePlural: false leaves already-singular and uncountable words alone,
            // which is what saves "species", "status", "equipment", and "news".
            : token.Singularize(inputIsKnownToBePlural: false));

    public string Pluralize(string name) => Transform(name, token =>
        token.Pluralize(inputIsKnownToBeSingular: false));

    /// <summary>Applies <paramref name="f"/> to the final separator-delimited token only.</summary>
    private static string Transform(string name, Func<string, string> f)
    {
        if (string.IsNullOrEmpty(name)) return name;

        int cut = name.LastIndexOfAny(['_', ' ', '-']);
        if (cut < 0) return f(name);

        return string.Concat(name.AsSpan(0, cut + 1), f(name[(cut + 1)..]));
    }

    /// <summary>
    /// True when the singular differs from the input by more than a trailing s/es/ies change.
    /// Keeps quiet for animals -> animal; speaks up for people -> person.
    /// </summary>
    public bool IsUncertain(string input, string result)
    {
        if (string.Equals(input, result, StringComparison.OrdinalIgnoreCase)) return false;

        foreach (string obvious in Obvious(result))
            if (string.Equals(input, obvious, StringComparison.OrdinalIgnoreCase))
                return false;

        return true;
    }

    private static IEnumerable<string> Obvious(string singular)
    {
        yield return singular + "s";
        yield return singular + "es";
        if (singular.EndsWith('y'))
            yield return string.Concat(singular.AsSpan(0, singular.Length - 1), "ies");
    }
}
