#if !NETFRAMEWORK
using System;
using System.Linq;
using Microsoft.Data.Sqlite;
using Xunit;
using Zonkey.Dialects;
using Zonkey.ObjectModel;
using Zonkey.Tests.Models;

namespace Zonkey.Tests.Unit
{
    /// <summary>
    /// Quoting precedence in DataClassCommandBuilder: a per-field or per-item setting on the
    /// DataMap wins over the builder-level UseQuotedIdentifier, which wins over the dialect
    /// default. Note that DataItemAttribute/DataFieldAttribute.UseQuotedIdentifier is bool?,
    /// which C# forbids as an attribute argument — map-level settings can only be applied by
    /// mutating a generated DataMap at runtime, as these tests do (always on DataMap.GenerateNew,
    /// never on the shared cached map).
    /// </summary>
    public class QuotedIdentifierBuilderTests : IDisposable
    {
        private readonly SqliteConnection _conn;

        public QuotedIdentifierBuilderTests()
        {
            _conn = new SqliteConnection("Data Source=:memory:");
            _conn.Open();
        }

        public void Dispose() => _conn.Dispose();

        private DataClassCommandBuilder CreateBuilder(SqlDialect dialect, out DataMap map)
        {
            map = DataMap.GenerateNew(typeof(OrderLog));
            return new DataClassCommandBuilder(typeof(OrderLog), map, _conn, dialect);
        }

        // Builder-level setting

        [Fact]
        public void Postgres_BuilderOn_QuotesTableAndAllFields()
        {
            var builder = CreateBuilder(new PostgreSqlDialect(), out _);
            builder.UseQuotedIdentifier = true;
            var sql = builder.GetSelectCommand("").CommandText;
            Assert.Contains("\"Order Log\"", sql);
            Assert.Contains("\"Order\"", sql);
            Assert.Contains("\"Note\"", sql);
        }

        [Fact]
        public void Postgres_BuilderUnset_GeneratesBareIdentifiers()
        {
            var builder = CreateBuilder(new PostgreSqlDialect(), out _);
            var sql = builder.GetSelectCommand("").CommandText;
            Assert.DoesNotContain("\"", sql);
        }

        [Fact]
        public void Sqlite_BuilderUnset_BracketsByDefault()
        {
            var builder = CreateBuilder(new SqliteDialect(), out _);
            var sql = builder.GetSelectCommand("").CommandText;
            Assert.Contains("[Order Log]", sql);
            Assert.Contains("[Order]", sql);
        }

        [Fact]
        public void Sqlite_BuilderOff_NoBrackets()
        {
            var builder = CreateBuilder(new SqliteDialect(), out _);
            builder.UseQuotedIdentifier = false;
            var sql = builder.GetSelectCommand("").CommandText;
            Assert.DoesNotContain("[", sql);
        }

        // Map-level overrides beat the builder-level setting

        [Fact]
        public void ItemLevelOn_QuotesTableName_EvenWhenBuilderUnset()
        {
            var builder = CreateBuilder(new PostgreSqlDialect(), out var map);
            map.DataItem.UseQuotedIdentifier = true;
            var sql = builder.GetSelectCommand("").CommandText;
            Assert.Contains("\"Order Log\"", sql);
            // item-level setting governs the table name only; fields follow the builder
            Assert.DoesNotContain("\"Order\"", sql);
        }

        [Fact]
        public void FieldLevelOn_QuotesOnlyThatField_EvenWhenBuilderUnset()
        {
            var builder = CreateBuilder(new PostgreSqlDialect(), out var map);
            map.GetReadableField("Order").UseQuotedIdentifier = true;
            var sql = builder.GetSelectCommand("").CommandText;
            Assert.Contains("\"Order\"", sql);
            Assert.DoesNotContain("\"Note\"", sql);
        }

        [Fact]
        public void FieldLevelOff_SuppressesBracketDefault()
        {
            var builder = CreateBuilder(new SqliteDialect(), out var map);
            map.GetReadableField("Order").UseQuotedIdentifier = false;
            var sql = builder.GetSelectCommand("").CommandText;
            Assert.Contains("[Note]", sql);
            Assert.DoesNotContain("[Order]", sql);
        }

        // Quoting must be consistent across command types, not just SELECT

        [Fact]
        public void InsertCommand_RespectsBuilderSetting()
        {
            var builder = CreateBuilder(new PostgreSqlDialect(), out _);
            builder.UseQuotedIdentifier = true;
            var log = new OrderLog { Order = 1, Note = "n" };
            var sql = builder.GetInsertCommands(log, SelectBack.None).First().CommandText;
            Assert.Contains("\"Order Log\"", sql);
            Assert.Contains("\"Order\"", sql);
        }

        [Fact]
        public void DeleteCommand_RespectsBuilderSetting()
        {
            var builder = CreateBuilder(new PostgreSqlDialect(), out _);
            builder.UseQuotedIdentifier = true;
            var sql = builder.DeleteItemCommand.CommandText;
            Assert.Contains("\"Order Log\"", sql);
            Assert.Contains("\"Id\"", sql);
        }
    }
}
#endif
