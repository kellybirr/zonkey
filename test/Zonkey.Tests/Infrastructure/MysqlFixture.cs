#if !NETFRAMEWORK
using System;
using System.Data.Common;
using System.IO;
using System.Threading.Tasks;
using MySqlConnector;
using Zonkey.Dialects;

namespace Zonkey.Tests.Infrastructure
{
    public class MysqlFixture : IDatabaseFixture
    {
        private readonly string _baseConnectionString;
        private readonly string _databaseName;

        public bool IsAvailable { get; private set; }
        public string SkipReason { get; private set; } = string.Empty;
        public SqlDialect Dialect { get; } = new MySqlDialect();
        public bool SupportsRowVersion => false;

        public MysqlFixture()
        {
            _baseConnectionString = TestConfiguration.MysqlConnectionString;
            _databaseName = $"zonkey_test_{Guid.NewGuid():N}";
        }

        private static bool RequireDatabase
        {
            get
            {
                var value = Environment.GetEnvironmentVariable("ZONKEY_REQUIRE_DB");
                return string.Equals(value, "1", StringComparison.OrdinalIgnoreCase)
                    || string.Equals(value, "true", StringComparison.OrdinalIgnoreCase);
            }
        }

        public DbConnection CreateConnection()
        {
            var conn = new MySqlConnection($"{_baseConnectionString};Database={_databaseName}");
            conn.Open();
            return conn;
        }

        public async ValueTask InitializeAsync()
        {
            try
            {
                // Connect without a default database to create the test database
                using (var adminConn = new MySqlConnection(_baseConnectionString))
                {
                    await adminConn.OpenAsync();
                    using var cmd = adminConn.CreateCommand();
                    cmd.CommandText = $"CREATE DATABASE `{_databaseName}`";
                    await cmd.ExecuteNonQueryAsync();
                }

                // Seed the test database
                var seedPath = Path.Combine(AppContext.BaseDirectory, "Seed", "mysql-seed.sql");
                var sql = File.ReadAllText(seedPath);

                using var conn = CreateConnection();
                using var seedCmd = conn.CreateCommand();
                seedCmd.CommandText = sql;
                await seedCmd.ExecuteNonQueryAsync();

                IsAvailable = true;
            }
            catch (Exception ex)
            {
                if (RequireDatabase)
                    throw new InvalidOperationException($"MySQL is required (ZONKEY_REQUIRE_DB is set) but initialization failed: {ex.Message}", ex);

                IsAvailable = false;
                SkipReason = $"MySQL not available: {ex.Message}. Set ZONKEY_TEST_MYSQL or run docker-compose up.";
            }
        }

        public async ValueTask DisposeAsync()
        {
            if (!IsAvailable) return;

            try
            {
                using var conn = new MySqlConnection(_baseConnectionString);
                await conn.OpenAsync();
                using var cmd = conn.CreateCommand();
                cmd.CommandText = $"DROP DATABASE IF EXISTS `{_databaseName}`";
                await cmd.ExecuteNonQueryAsync();
            }
            catch
            {
                // Best effort cleanup
            }
        }
    }
}
#endif
