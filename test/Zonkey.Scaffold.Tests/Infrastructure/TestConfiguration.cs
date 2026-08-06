namespace Zonkey.Scaffold.Tests.Infrastructure;

/// <summary>
/// Connection strings and switches for the container-backed provider fixtures. Defaults mirror
/// <c>test/Zonkey.Tests/Infrastructure/TestConfiguration.cs</c> (same docker-compose containers,
/// same env var names) so overriding one suite's target also overrides the other's.
/// </summary>
public static class TestConfiguration
{
    public static string MssqlConnectionString =>
        Environment.GetEnvironmentVariable("ZONKEY_TEST_MSSQL")
        ?? "Server=localhost,1434;User=sa;Password=Zonkey#Test123;TrustServerCertificate=true";

    public static string PgsqlConnectionString =>
        Environment.GetEnvironmentVariable("ZONKEY_TEST_PGSQL")
        ?? "Host=localhost;Port=5433;Username=zonkey;Password=zonkey";

    public static string MysqlConnectionString =>
        Environment.GetEnvironmentVariable("ZONKEY_TEST_MYSQL")
        ?? "Server=localhost;Port=3308;User=root;Password=zonkey;AllowPublicKeyRetrieval=True;SslMode=None;GuidFormat=Char36";

    /// <summary>When set, an unavailable container is a hard failure instead of a skip.</summary>
    public static bool RequireDatabase
    {
        get
        {
            string? value = Environment.GetEnvironmentVariable("ZONKEY_REQUIRE_DB");
            return string.Equals(value, "1", StringComparison.OrdinalIgnoreCase)
                || string.Equals(value, "true", StringComparison.OrdinalIgnoreCase);
        }
    }
}
