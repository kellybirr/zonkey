#if !NETFRAMEWORK
using System;
using System.Data.Common;
using System.IO;
using System.Threading.Tasks;
using Npgsql;
using Zonkey.Dialects;

namespace Zonkey.Tests.Infrastructure
{
    public class PgsqlFixture : IDatabaseFixture
    {
        private readonly string _baseConnectionString;
        private readonly string _databaseName;

        public bool IsAvailable { get; private set; }
        public string SkipReason { get; private set; } = string.Empty;
        public SqlDialect Dialect { get; } = new PostgreSqlDialect();
        public bool SupportsRowVersion => false;

        public PgsqlFixture()
        {
            _baseConnectionString = TestConfiguration.PgsqlConnectionString;
            _databaseName = $"zonkey_test_{Guid.NewGuid():N}";
        }

        public DbConnection CreateConnection()
        {
            var conn = new NpgsqlConnection($"{_baseConnectionString};Database={_databaseName}");
            conn.Open();
            return conn;
        }

        public async ValueTask InitializeAsync()
        {
            try
            {
                // Connect to default db to create test database
                using (var adminConn = new NpgsqlConnection($"{_baseConnectionString};Database=zonkey_test"))
                {
                    await adminConn.OpenAsync();
                    using var cmd = adminConn.CreateCommand();
                    cmd.CommandText = $"CREATE DATABASE \"{_databaseName}\"";
                    await cmd.ExecuteNonQueryAsync();
                }

                // Seed the test database
                var seedPath = Path.Combine(AppContext.BaseDirectory, "Seed", "pgsql-seed.sql");
                var sql = File.ReadAllText(seedPath);

                using var conn = CreateConnection();
                using var seedCmd = conn.CreateCommand();
                seedCmd.CommandText = sql;
                await seedCmd.ExecuteNonQueryAsync();

                IsAvailable = true;
            }
            catch (Exception ex)
            {
                IsAvailable = false;
                SkipReason = $"PostgreSQL not available: {ex.Message}. Set ZONKEY_TEST_PGSQL or run docker-compose up.";
            }
        }

        public async ValueTask DisposeAsync()
        {
            if (!IsAvailable) return;

            try
            {
                using var conn = new NpgsqlConnection($"{_baseConnectionString};Database=zonkey_test");
                await conn.OpenAsync();

                // Terminate existing connections
                using var termCmd = conn.CreateCommand();
                termCmd.CommandText = $@"
                    SELECT pg_terminate_backend(pid)
                    FROM pg_stat_activity
                    WHERE datname = '{_databaseName}' AND pid <> pg_backend_pid()";
                await termCmd.ExecuteNonQueryAsync();

                using var dropCmd = conn.CreateCommand();
                dropCmd.CommandText = $"DROP DATABASE IF EXISTS \"{_databaseName}\"";
                await dropCmd.ExecuteNonQueryAsync();
            }
            catch
            {
                // Best effort cleanup
            }
        }
    }
}
#endif
