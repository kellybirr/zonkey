#if !NETFRAMEWORK
using System;
using Microsoft.Data.Sqlite;
using Xunit;
using Zonkey;
using Zonkey.Dialects;
using Zonkey.ObjectModel;
using Zonkey.Tests.Models;

namespace Zonkey.Tests.Unit
{
    /// <summary>
    /// Exists() must generate portable SQL through the dialect system, not hard-coded
    /// T-SQL. The ANSI form "SELECT CASE WHEN EXISTS(...) THEN 1 ELSE 0 END" works on
    /// SQL Server, SQLite, PostgreSQL, and MySQL; Oracle and DB2 need a dummy FROM table.
    /// </summary>
    public class ExistsQueryTests : IDisposable
    {
        private readonly SqliteConnection _conn;

        public ExistsQueryTests()
        {
            _conn = new SqliteConnection("Data Source=:memory:");
            _conn.Open();
        }

        public void Dispose() => _conn.Dispose();

        private string ExistsSql(SqlDialect dialect, string filter = "SpeciesId = 1")
        {
            var map = DataMap.GenerateNew(typeof(Animal));
            var builder = new DataClassCommandBuilder(typeof(Animal), map, _conn, dialect);
            return builder.GetExistsCommand(filter).CommandText;
        }

        [Fact]
        public void SqlServer_UsesPortableCaseWhenForm()
        {
            var sql = ExistsSql(new SqlServerDialect());
            Assert.Contains("CASE WHEN EXISTS", sql);
            Assert.DoesNotContain("IF EXISTS", sql);
        }

        [Fact]
        public void Sqlite_UsesPortableCaseWhenForm()
        {
            var sql = ExistsSql(new SqliteDialect());
            Assert.Contains("CASE WHEN EXISTS", sql);
            Assert.DoesNotContain("IF EXISTS", sql);
        }

        [Fact]
        public void Postgres_UsesPortableCaseWhenForm()
        {
            var sql = ExistsSql(new PostgreSqlDialect());
            Assert.Contains("CASE WHEN EXISTS", sql);
            Assert.DoesNotContain("IF EXISTS", sql);
        }

        [Fact]
        public void MySql_UsesPortableCaseWhenForm()
        {
            var sql = ExistsSql(new MySqlDialect());
            Assert.Contains("CASE WHEN EXISTS", sql);
            Assert.DoesNotContain("IF EXISTS", sql);
        }

        [Fact]
        public void Oracle_SelectsFromDual()
        {
            var sql = ExistsSql(new OracleSqlDialect());
            Assert.Contains("CASE WHEN EXISTS", sql);
            Assert.Contains("FROM DUAL", sql);
        }

        [Fact]
        public void Db2_SelectsFromSysDummy()
        {
            var sql = ExistsSql(new DB2SqlDialect());
            Assert.Contains("CASE WHEN EXISTS", sql);
            Assert.Contains("SYSIBM.SYSDUMMY1", sql);
        }

        [Fact]
        public void Filter_AppearsInWhereClause()
        {
            var sql = ExistsSql(new PostgreSqlDialect(), "SpeciesId = 42");
            Assert.Contains("WHERE SpeciesId = 42", sql);
        }

        [Fact]
        public void EmptyFilter_OmitsWhereClause()
        {
            var sql = ExistsSql(new PostgreSqlDialect(), "");
            Assert.DoesNotContain("WHERE", sql);
        }

        [Fact]
        public void SqlFilterOverload_UsesPortableFormAndBindsParameter()
        {
            var map = DataMap.GenerateNew(typeof(Animal));
            var builder = new DataClassCommandBuilder(typeof(Animal), map, _conn, new PostgreSqlDialect());
            var cmd = builder.GetExistsCommand(new[] { SqlFilter.EQ("SpeciesId", 1) });
            Assert.Contains("CASE WHEN EXISTS", cmd.CommandText);
            Assert.DoesNotContain("IF EXISTS", cmd.CommandText);
            Assert.Single(cmd.Parameters);
        }
    }
}
#endif
