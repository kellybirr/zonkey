using System.Collections.Concurrent;
using System.Text;
using System.Text.RegularExpressions;

namespace Zonkey.Scaffold.Selection;

/// <summary>Case-insensitive glob matching. Only <c>*</c> and <c>?</c> are wildcards.</summary>
public static class GlobPattern
{
    private static readonly ConcurrentDictionary<string, Regex> Cache = new(StringComparer.Ordinal);

    public static bool IsMatch(string pattern, string value)
        => Cache.GetOrAdd(pattern, Compile).IsMatch(value);

    private static Regex Compile(string pattern)
    {
        var sb = new StringBuilder(@"\A");
        foreach (char c in pattern)
        {
            sb.Append(c switch
            {
                '*' => ".*",
                '?' => ".",
                _   => Regex.Escape(c.ToString())
            });
        }
        sb.Append(@"\z");

        return new Regex(sb.ToString(),
            RegexOptions.IgnoreCase | RegexOptions.CultureInvariant | RegexOptions.Compiled);
    }
}
