using System;

namespace Zonkey.Tests.Infrastructure
{
    public static class TestConfiguration
    {
        public static string MssqlConnectionString =>
            Environment.GetEnvironmentVariable("ZONKEY_TEST_MSSQL")
            ?? "Server=localhost,1434;User=sa;Password=Zonkey#Test123;TrustServerCertificate=true";

        public static string PgsqlConnectionString =>
            Environment.GetEnvironmentVariable("ZONKEY_TEST_PGSQL")
            ?? "Host=localhost;Port=5433;Username=zonkey;Password=zonkey";
    }
}
