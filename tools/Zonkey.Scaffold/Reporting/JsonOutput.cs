using System.Text.Json;
using System.Text.Json.Serialization;
using Zonkey.Scaffold.Config;

namespace Zonkey.Scaffold.Reporting;

public static class JsonOutput
{
    private static readonly JsonSerializerOptions Options = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        WriteIndented = true
    };

    /// <summary>
    /// Serializes, then scrubs. Redaction happens on the rendered text rather than per-property
    /// so a connection string that leaked into an exception message is caught too.
    /// </summary>
    /// <remarks>
    /// Scrubbing rendered text means the needle has to be the connection string <em>as JSON writes
    /// it</em>, not as the caller typed it. <see cref="SecretRedactor.Redact"/> does an ordinal
    /// replace, and <see cref="JsonSerializer"/> doubles every backslash, so
    /// <c>Server=.\SQLEXPRESS</c> appears in the payload as <c>Server=.\\SQLEXPRESS</c> and the
    /// verbatim replace found nothing — on Windows, where a file path or a named instance puts a
    /// backslash in almost every connection string, the whole-string half of redaction was a
    /// silent no-op. (The <c>password=</c> key regex still fired, so this leaked the rest of the
    /// string rather than the password itself; that is a narrower hole, not a closed one.) The
    /// escaped form is produced with the same <see cref="JsonSerializerOptions"/> that rendered
    /// the payload, so the two encoders cannot disagree.
    /// <para>
    /// <c>Redact</c> is nullable-in/nullable-out because it also redacts arbitrary free text (e.g.
    /// driver exception messages) that may legitimately be null or empty. Here the input is always
    /// freshly serialized JSON for a non-null <paramref name="value"/>, which
    /// <see cref="JsonSerializer.Serialize{T}(T, JsonSerializerOptions)"/> never renders as null
    /// or empty — so <c>Redact</c>'s only null/empty-preserving early-out cannot trigger, and the
    /// null-forgiving operator documents that invariant rather than suppressing a real
    /// possibility.
    /// </para>
    /// </remarks>
    public static string Serialize<T>(T value, string? connectionString)
    {
        string json = JsonSerializer.Serialize(value, Options);

        if (!string.IsNullOrWhiteSpace(connectionString))
        {
            json = json.Replace(
                JsonEncoded(connectionString), SecretRedactor.Marker, StringComparison.Ordinal);
        }

        return SecretRedactor.Redact(json, connectionString)!;
    }

    /// <summary>
    /// A string as it appears inside a JSON document — the serialized form with its delimiting
    /// quotes removed.
    /// </summary>
    private static string JsonEncoded(string value)
    {
        string quoted = JsonSerializer.Serialize(value, Options);
        return quoted[1..^1];
    }
}
