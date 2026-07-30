using System;
using System.Linq.Expressions;
using Xunit;
using Zonkey.Dialects;
using Zonkey.ObjectModel;
using Zonkey.Tests.Models;

namespace Zonkey.Tests.Unit
{
    /// <summary>
    /// Dialect-level tri-state quoting semantics. The two dialect families differ on purpose:
    /// bracket dialects (SqlServer, Sqlite) quote unless explicitly disabled (null => quoted,
    /// brackets never change meaning), while ANSI-style dialects (PostgreSql, MySql) quote
    /// only when explicitly enabled (null => bare, because quoting changes case semantics).
    /// </summary>
    public class QuotedIdentifierDialectTests
    {
        // SQL Server — brackets unless explicitly off

        [Theory]
        [InlineData(true, "[Order]")]
        [InlineData(null, "[Order]")]
        [InlineData(false, "Order")]
        public void SqlServer_FieldName(bool? useQuoted, string expected) =>
            Assert.Equal(expected, new SqlServerDialect().FormatFieldName("Order", useQuoted));

        [Theory]
        [InlineData(true, "[dbo].[Order Log]")]
        [InlineData(null, "[dbo].[Order Log]")]
        [InlineData(false, "dbo.Order Log")]
        public void SqlServer_TableName_WithSchema(bool? useQuoted, string expected) =>
            Assert.Equal(expected, new SqlServerDialect().FormatTableName("Order Log", "dbo", useQuoted));

        // Sqlite — brackets unless explicitly off (no schema support)

        [Theory]
        [InlineData(true, "[Order]")]
        [InlineData(null, "[Order]")]
        [InlineData(false, "Order")]
        public void Sqlite_FieldName(bool? useQuoted, string expected) =>
            Assert.Equal(expected, new SqliteDialect().FormatFieldName("Order", useQuoted));

        [Theory]
        [InlineData(true, "[Order Log]")]
        [InlineData(null, "[Order Log]")]
        [InlineData(false, "Order Log")]
        public void Sqlite_TableName(bool? useQuoted, string expected) =>
            Assert.Equal(expected, new SqliteDialect().FormatTableName("Order Log", null, useQuoted));

        // PostgreSql — double quotes only when explicitly on

        [Theory]
        [InlineData(true, "\"Order\"")]
        [InlineData(null, "Order")]
        [InlineData(false, "Order")]
        public void Postgres_FieldName(bool? useQuoted, string expected) =>
            Assert.Equal(expected, new PostgreSqlDialect().FormatFieldName("Order", useQuoted));

        [Theory]
        [InlineData(true, "\"zoo\".\"Order Log\"")]
        [InlineData(null, "zoo.Order Log")]
        [InlineData(false, "zoo.Order Log")]
        public void Postgres_TableName_WithSchema(bool? useQuoted, string expected) =>
            Assert.Equal(expected, new PostgreSqlDialect().FormatTableName("Order Log", "zoo", useQuoted));

        // MySql — backticks only when explicitly on

        [Theory]
        [InlineData(true, "`Order`")]
        [InlineData(null, "Order")]
        [InlineData(false, "Order")]
        public void MySql_FieldName(bool? useQuoted, string expected) =>
            Assert.Equal(expected, new MySqlDialect().FormatFieldName("Order", useQuoted));

        [Theory]
        [InlineData(true, "`zoo`.`Order Log`")]
        [InlineData(null, "zoo.Order Log")]
        public void MySql_TableName_WithSchema(bool? useQuoted, string expected) =>
            Assert.Equal(expected, new MySqlDialect().FormatTableName("Order Log", "zoo", useQuoted));
    }

    /// <summary>
    /// The LINQ WHERE-clause parser must apply the same quoting setting the adapter's
    /// command builder uses, or filters would reference differently-cased identifiers
    /// than the rest of the statement.
    /// </summary>
    public class QuotedIdentifierParserTests
    {
        private static SqlWhereClause Parse(SqlDialect dialect, bool? useQuoted)
        {
            var parser = new WhereExpressionParser<OrderLog>(dialect) { UseQuotedIdentifier = useQuoted };
            return parser.Parse((Expression<Func<OrderLog, bool>>)(o => o.Order == 5));
        }

        [Fact]
        public void SqlServer_QuotedByDefault()
        {
            var clause = Parse(new SqlServerDialect(), null);
            Assert.Contains("[Order]", clause.SqlText);
        }

        [Fact]
        public void SqlServer_ExplicitlyOff_NoBrackets()
        {
            var clause = Parse(new SqlServerDialect(), false);
            Assert.DoesNotContain("[", clause.SqlText);
            Assert.Contains("Order", clause.SqlText);
        }

        [Fact]
        public void Postgres_ExplicitlyOn_QuotesField()
        {
            var clause = Parse(new PostgreSqlDialect(), true);
            Assert.Contains("\"Order\"", clause.SqlText);
        }

        [Fact]
        public void Postgres_Default_BareField()
        {
            var clause = Parse(new PostgreSqlDialect(), null);
            Assert.DoesNotContain("\"", clause.SqlText);
            Assert.Contains("Order", clause.SqlText);
        }
    }
}
