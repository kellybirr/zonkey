using Xunit;
using Zonkey.Scaffold.Config;

namespace Zonkey.Scaffold.Tests.Config;

public class SecretRedactorTests
{
    private const string Conn =
        "Server=db;Database=zoo;User Id=sa;Password=Sup3rS3cret!;TrustServerCertificate=true";

    [Fact]
    public void Redacts_whole_connection_string_from_message()
    {
        string msg = $"Login failed for connection '{Conn}'.";
        string safe = SecretRedactor.Redact(msg, Conn)!;
        Assert.DoesNotContain("Sup3rS3cret!", safe);
        Assert.Contains("***REDACTED***", safe);
    }

    [Fact]
    public void Redacts_password_even_when_full_string_not_present()
    {
        string msg = "Failed: Password=Sup3rS3cret!;Server=db";
        string safe = SecretRedactor.Redact(msg, connectionString: null)!;
        Assert.DoesNotContain("Sup3rS3cret!", safe);
    }

    [Theory]
    [InlineData("Pwd=abc123;Server=x")]
    [InlineData("password=abc123")]
    [InlineData("PASSWORD =abc123;")]
    public void Redacts_password_key_variants(string text)
        => Assert.DoesNotContain("abc123", SecretRedactor.Redact(text, null));

    [Fact]
    public void Describe_keeps_only_non_secret_parts()
    {
        string d = SecretRedactor.Describe(Conn);
        Assert.Contains("Server=db", d);
        Assert.Contains("Database=zoo", d);
        Assert.DoesNotContain("Sup3rS3cret!", d);

        // The two-character substring "sa" is a fragile proxy for "the User Id pair was
        // dropped" (it would also pass if e.g. "Database" were renamed). Assert directly
        // that no segment of the output is a User Id/Uid pair, case-insensitively.
        string[] segments = d.Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        Assert.DoesNotContain(segments, s =>
            s.StartsWith("User Id", StringComparison.OrdinalIgnoreCase) ||
            s.StartsWith("Uid", StringComparison.OrdinalIgnoreCase));
        Assert.DoesNotContain("sa", d);
    }

    [Fact]
    public void Describe_drops_segment_with_no_equals_sign_instead_of_leaking_it()
    {
        // A malformed/truncated segment with no '=' carries no key to classify, so it must
        // be dropped entirely rather than passed through — otherwise an unkeyed secret value
        // could slip past the key-based filter untouched.
        string d = SecretRedactor.Describe("Server=db;Sup3rS3cret!;Database=zoo");
        Assert.DoesNotContain("Sup3rS3cret!", d);
        Assert.Contains("Server=db", d);
        Assert.Contains("Database=zoo", d);
    }

    [Fact]
    public void Describe_drops_all_occurrences_of_duplicate_secret_keys()
    {
        string d = SecretRedactor.Describe("Server=db;Password=first;Password=second");
        Assert.DoesNotContain("first", d);
        Assert.DoesNotContain("second", d);
        Assert.Contains("Server=db", d);
    }

    [Fact]
    public void Describe_is_safe_for_null_and_empty_and_whitespace()
    {
        Assert.Equal("(none)", SecretRedactor.Describe(null!));
        Assert.Equal("(none)", SecretRedactor.Describe(""));
        Assert.Equal("(none)", SecretRedactor.Describe("   "));
    }

    [Fact]
    public void Null_and_empty_are_safe()
    {
        Assert.Equal("", SecretRedactor.Redact("", null));
        Assert.Equal("hello", SecretRedactor.Redact("hello", null));
    }

    [Fact]
    public void Redact_is_safe_for_null_text()
    {
        Assert.Null(SecretRedactor.Redact(null, null));
    }

    [Fact]
    public void Redacts_quoted_password_value_containing_embedded_semicolon()
    {
        // ADO.NET permits quoting a value that itself contains ';'. The value capture must try
        // the quoted forms before the bare [^;]* fallback, otherwise it stops at the embedded
        // ';' and the quoted tail (here "Secret!") survives redaction untouched.
        string msg = "Failed: Password=\"Sup3r;Secret!\";Server=db";
        string? safe = SecretRedactor.Redact(msg, connectionString: null);
        Assert.DoesNotContain("Sup3r", safe);
        Assert.DoesNotContain("Secret!", safe);
        Assert.Contains(SecretRedactor.Marker, safe);
    }

    [Fact]
    public void Redacts_single_quoted_password_value_containing_embedded_semicolon()
    {
        string msg = "Failed: Password='Sup3r;Secret!';Server=db";
        string? safe = SecretRedactor.Redact(msg, connectionString: null);
        Assert.DoesNotContain("Sup3r", safe);
        Assert.DoesNotContain("Secret!", safe);
    }

    [Theory]
    [InlineData("User Id=AdminUser42;Server=db")]
    [InlineData("UserId=AdminUser42;Server=db")]
    [InlineData("Uid=AdminUser42;Server=db")]
    [InlineData("uid=AdminUser42;Server=db")]
    public void Redacts_user_id_key_variants_without_full_connection_string_present(string text)
    {
        // Describe() treats "user id" / "userid" / "uid" as secret. Redact's regex must match
        // the same keys, or the two halves of this class disagree about what's sensitive — a
        // 'User Id=' fragment could leak through Redact while being scrubbed from Describe.
        string? safe = SecretRedactor.Redact(text, null);
        Assert.DoesNotContain("AdminUser42", safe);
        Assert.Contains(SecretRedactor.Marker, safe);
    }
}
