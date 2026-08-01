using System.Text.RegularExpressions;

namespace Zonkey.Scaffold.Config;

/// <summary>
/// Database drivers echo the full connection string on failure. Every message and every JSON
/// payload the tool emits passes through here first, because that output lands in CI logs,
/// shell history, and agent transcripts.
/// </summary>
public static partial class SecretRedactor
{
    public const string Marker = "***REDACTED***";

    // Keep in sync with the key alternation in SecretPairRegex() below — every key considered
    // sensitive here must also be matched there, and vice versa, or Redact and Describe will
    // disagree about what counts as a secret. GeneratedRegex requires a compile-time-constant
    // pattern, so this can't be built from a single shared array; the two lists are kept
    // side by side instead, each pointing at the other.
    private static readonly string[] SecretKeys =
        ["password", "pwd", "user id", "uid", "userid", "accountkey", "sharedaccesssignature"];

    // Keep the key alternation in sync with SecretKeys above. "user\s*id" covers both "User Id"
    // and "UserId" (zero or more spaces between the words); "uid" is matched separately. Values
    // may be double- or single-quoted per ADO.NET connection-string rules, and a quoted value can
    // itself contain ';' — the quoted alternatives must be tried before the bare [^;]* fallback,
    // otherwise a value like "Sup3r;Secret!" (quoted) truncates at the embedded semicolon and the
    // tail survives redaction.
    [GeneratedRegex(
        @"(?<key>\b(?:password|pwd|accountkey|sharedaccesssignature|user\s*id|uid)\b)\s*=\s*(?<val>""[^""]*""|'[^']*'|[^;]*)",
        RegexOptions.IgnoreCase)]
    private static partial Regex SecretPairRegex();

    /// <summary>
    /// Scrubs a connection string (if supplied and present verbatim) and any recognizable
    /// key=value secret pairs (password, pwd, user id/uid, account key, SAS token — see
    /// <see cref="SecretPairRegex"/>) out of arbitrary text, such as a driver's exception
    /// message. Safe to call with a null or empty <paramref name="text"/> or
    /// <paramref name="connectionString"/>.
    /// </summary>
    public static string? Redact(string? text, string? connectionString)
    {
        if (string.IsNullOrEmpty(text)) return text;

        if (!string.IsNullOrWhiteSpace(connectionString))
            text = text.Replace(connectionString, Marker, StringComparison.Ordinal);

        return SecretPairRegex().Replace(text, m => $"{m.Groups["key"].Value}={Marker}");
    }

    /// <summary>A loggable summary: every pair whose key is not sensitive.</summary>
    public static string Describe(string connectionString)
    {
        if (string.IsNullOrWhiteSpace(connectionString)) return "(none)";

        IEnumerable<string> kept = connectionString
            .Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Where(pair =>
            {
                int eq = pair.IndexOf('=');
                if (eq < 0) return false;
                string key = pair[..eq].Trim().ToLowerInvariant();
                return !SecretKeys.Contains(key);
            });

        return string.Join(";", kept);
    }
}
