#if !NETFRAMEWORK
using System;
using System.Data.Common;
using System.IO;
using System.Threading.Tasks;
using Microsoft.Data.Sqlite;
using Zonkey.Dialects;

namespace Zonkey.Tests.Infrastructure
{
    public class SqliteFixture : IDatabaseFixture
    {
        private readonly string _dbPath;

        public bool IsAvailable => true;
        public string SkipReason => string.Empty;
        public SqlDialect Dialect { get; } = new SqliteDialect();
        public bool SupportsRowVersion => false;

        public SqliteFixture()
        {
            _dbPath = Path.Combine(Path.GetTempPath(), $"zonkey_test_{Guid.NewGuid():N}.db");
        }

        public DbConnection CreateConnection()
        {
            var conn = new SqliteConnection($"Data Source={_dbPath}");
            conn.Open();
            return conn;
        }

        public async ValueTask InitializeAsync()
        {
            var seedPath = Path.Combine(AppContext.BaseDirectory, "Seed", "sqlite-seed.sql");
            var sql = File.ReadAllText(seedPath);

            using var conn = CreateConnection();
            using var cmd = conn.CreateCommand();
            cmd.CommandText = sql;
            await cmd.ExecuteNonQueryAsync();
        }

        public ValueTask DisposeAsync()
        {
            try
            {
                if (File.Exists(_dbPath))
                    File.Delete(_dbPath);
            }
            catch
            {
                // Best effort cleanup
            }
            return ValueTask.CompletedTask;
        }
    }
}
#endif
