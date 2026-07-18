#if !NETFRAMEWORK
using System;
using System.Data.Common;
using System.IO;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.Data.SqlClient;
using Zonkey.Dialects;

namespace Zonkey.Tests.Infrastructure
{
    public class MssqlFixture : IDatabaseFixture
    {
        private readonly string _baseConnectionString;
        private readonly string _databaseName;

        public bool IsAvailable { get; private set; }
        public string SkipReason { get; private set; } = string.Empty;
        public SqlDialect Dialect { get; } = new SqlServerDialect();
        public bool SupportsRowVersion => true;

        public MssqlFixture()
        {
            _baseConnectionString = TestConfiguration.MssqlConnectionString;
            _databaseName = $"zonkey_test_{Guid.NewGuid():N}";
        }

        public DbConnection CreateConnection()
        {
            var conn = new SqlConnection($"{_baseConnectionString};Database={_databaseName}");
            conn.Open();
            return conn;
        }

        public async ValueTask InitializeAsync()
        {
            try
            {
                // Connect to master to create test database
                using (var masterConn = new SqlConnection($"{_baseConnectionString};Database=master"))
                {
                    await masterConn.OpenAsync();
                    using var cmd = masterConn.CreateCommand();
                    cmd.CommandText = $"CREATE DATABASE [{_databaseName}]";
                    await cmd.ExecuteNonQueryAsync();
                }

                // Seed the test database
                var seedPath = Path.Combine(AppContext.BaseDirectory, "Seed", "mssql-seed.sql");
                var sql = File.ReadAllText(seedPath);
                var batches = Regex.Split(sql, @"^\s*GO\s*$", RegexOptions.Multiline | RegexOptions.IgnoreCase);

                using var conn = CreateConnection();
                foreach (var batch in batches)
                {
                    var trimmed = batch.Trim();
                    if (string.IsNullOrEmpty(trimmed)) continue;

                    using var cmd = conn.CreateCommand();
                    cmd.CommandText = trimmed;
                    cmd.CommandTimeout = 30;
                    await cmd.ExecuteNonQueryAsync();
                }

                IsAvailable = true;
            }
            catch (Exception ex)
            {
                IsAvailable = false;
                SkipReason = $"MSSQL not available: {ex.Message}. Set ZONKEY_TEST_MSSQL or run docker-compose up.";
            }
        }

        public async ValueTask DisposeAsync()
        {
            if (!IsAvailable) return;

            try
            {
                using var conn = new SqlConnection($"{_baseConnectionString};Database=master");
                await conn.OpenAsync();
                using var cmd = conn.CreateCommand();
                cmd.CommandText = $@"
                    IF DB_ID('{_databaseName}') IS NOT NULL
                    BEGIN
                        ALTER DATABASE [{_databaseName}] SET SINGLE_USER WITH ROLLBACK IMMEDIATE;
                        DROP DATABASE [{_databaseName}];
                    END";
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
