using System.Data;
using Xunit;
using Zonkey.Dialects;

namespace Zonkey.Tests.Unit
{
    public class DialectTests
    {
        [Fact]
        public void SqlServer_SupportsRowVersion() =>
            Assert.True(new SqlServerDialect().SupportsRowVersion);

        [Fact]
        public void SqlServer_SupportsSchema() =>
            Assert.True(new SqlServerDialect().SupportsSchema);

        [Fact]
        public void SqlServer_SupportsNoLock() =>
            Assert.True(new SqlServerDialect().SupportsNoLock);

        [Fact]
        public void SqlServer_SupportsLimit() =>
            Assert.True(new SqlServerDialect().SupportsLimit);

        [Fact]
        public void Sqlite_DoesNotSupportRowVersion() =>
            Assert.False(new SqliteDialect().SupportsRowVersion);

        [Fact]
        public void Sqlite_SupportsLimit() =>
            Assert.True(new SqliteDialect().SupportsLimit);

        [Fact]
        public void Postgres_SupportsLimit() =>
            Assert.True(new PostgreSqlDialect().SupportsLimit);

        [Fact]
        public void Postgres_DoesNotSupportRowVersion() =>
            Assert.False(new PostgreSqlDialect().SupportsRowVersion);

        [Fact]
        public void SqlServer_FormatsFieldName_WithBrackets()
        {
            var dialect = new SqlServerDialect();
            var result = dialect.FormatFieldName("Name", true);
            Assert.Equal("[Name]", result);
        }

        [Fact]
        public void Sqlite_FormatsFieldName_WithBrackets()
        {
            var dialect = new SqliteDialect();
            var result = dialect.FormatFieldName("Name", true);
            Assert.Equal("[Name]", result);
        }

        [Fact]
        public void MySql_FormatsFieldName_WithBackticks()
        {
            var dialect = new MySqlDialect();
            var result = dialect.FormatFieldName("Name", true);
            Assert.Contains("`", result);
        }

        [Fact]
        public void SqlServer_AutoIncrement_UsesScopeIdentity()
        {
            var dialect = new SqlServerDialect();
            var result = dialect.FormatAutoIncrementSelect(null);
            Assert.Contains("SCOPE_IDENTITY", result);
        }

        [Fact]
        public void Sqlite_AutoIncrement_UsesLastInsertRowId()
        {
            var dialect = new SqliteDialect();
            var result = dialect.FormatAutoIncrementSelect(null);
            Assert.Contains("last_insert_rowid", result);
        }

        [Fact]
        public void Postgres_AutoIncrement_UsesLastVal()
        {
            var dialect = new PostgreSqlDialect();
            var result = dialect.FormatAutoIncrementSelect(null);
            Assert.Contains("lastval", result);
        }

        [Fact]
        public void Postgres_AutoIncrement_WithSequence_UsesCurrVal()
        {
            var dialect = new PostgreSqlDialect();
            var result = dialect.FormatAutoIncrementSelect("my_seq");
            Assert.Contains("currval", result);
            Assert.Contains("my_seq", result);
        }

        [Fact]
        public void SqlServer_FormatsParameter_WithAtSign()
        {
            var dialect = new SqlServerDialect();
            var result = dialect.FormatParameterName(0, CommandType.Text);
            Assert.StartsWith("@", result);
        }

        [Fact]
        public void SqlServer_FormatUnaryBoolean_UsesEqualsOne()
        {
            var dialect = new SqlServerDialect();
            var result = dialect.FormatUnaryBoolean("IsOpen");
            Assert.Contains("= 1", result);
        }

        [Fact]
        public void Postgres_FormatUnaryBoolean_UsesFieldDirectly()
        {
            var dialect = new PostgreSqlDialect();
            var result = dialect.FormatUnaryBoolean("IsOpen");
            Assert.Equal("(IsOpen)", result);
        }

        [Fact]
        public void Sqlite_FormatLimitQuery_LimitIsLength_OffsetIsStart()
        {
            var dialect = new SqliteDialect();
            var sql = dialect.FormatLimitQuery("*", "Animal", "1=1", "AnimalId", 20, 10);
            Assert.Contains("LIMIT 10", sql);
            Assert.Contains("OFFSET 20", sql);
        }

        [Fact]
        public void SqlServer_FormatLimitQuery_UsesAnsiOffsetFetch()
        {
            // SQL Server 2012+ inherits the ANSI SQL:2008 OFFSET/FETCH form from the base
            // SqlDialect (the pre-v7.0 ROW_NUMBER() wrapper, compatible with SQL Server
            // 2005/2008, has been removed).
            var dialect = new SqlServerDialect();
            var sql = dialect.FormatLimitQuery("*", "Animal", "1=1", "AnimalId", 20, 10);
            Assert.Equal("SELECT * FROM Animal WHERE 1=1 ORDER BY AnimalId OFFSET 20 ROWS FETCH NEXT 10 ROWS ONLY;", sql);
        }

        [Fact]
        public void SqlServer_FormatTableName_WithSchema()
        {
            var dialect = new SqlServerDialect();
            var result = dialect.FormatTableName("Animals", "dbo", true);
            Assert.Contains("dbo", result);
            Assert.Contains("Animals", result);
        }
    }
}
