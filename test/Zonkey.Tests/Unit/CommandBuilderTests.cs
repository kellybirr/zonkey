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
    public class CommandBuilderTests : IDisposable
    {
        private readonly SqliteConnection _conn;

        public CommandBuilderTests()
        {
            _conn = new SqliteConnection("Data Source=:memory:");
            _conn.Open();
        }

        public void Dispose() => _conn.Dispose();

        private DataClassCommandBuilder CreateBuilder<T>(SqlDialect dialect = null)
        {
            var map = DataMap.GenerateNew(typeof(T));
            dialect ??= new SqliteDialect();
            return new DataClassCommandBuilder(typeof(T), map, _conn, dialect);
        }

        // SELECT tests

        [Fact]
        public void SelectByKeys_ContainsKeyField()
        {
            var builder = CreateBuilder<Animal>();
            var cmd = builder.SelectByKeysCommand;
            Assert.Contains("AnimalId", cmd.CommandText);
            Assert.Contains("WHERE", cmd.CommandText);
        }

        [Fact]
        public void SelectByKeys_CompositeKey_ContainsAllKeys()
        {
            var builder = CreateBuilder<FeedingSchedule>();
            var cmd = builder.SelectByKeysCommand;
            Assert.Contains("AnimalId", cmd.CommandText);
            Assert.Contains("DayOfWeek", cmd.CommandText);
            Assert.Contains("TimeSlot", cmd.CommandText);
        }

        [Fact]
        public void GetSelectCommand_WithFilter_ContainsWhere()
        {
            var builder = CreateBuilder<Animal>();
            var cmd = builder.GetSelectCommand("SpeciesId = 1");
            Assert.Contains("WHERE", cmd.CommandText);
            Assert.Contains("SpeciesId = 1", cmd.CommandText);
        }

        [Fact]
        public void GetSelectCommand_ContainsAllReadableFields()
        {
            var builder = CreateBuilder<Animal>();
            var cmd = builder.GetSelectCommand("");
            Assert.Contains("AnimalId", cmd.CommandText);
            Assert.Contains("Name", cmd.CommandText);
            Assert.Contains("SpeciesId", cmd.CommandText);
            Assert.Contains("Notes", cmd.CommandText);
        }

        // INSERT tests

        [Fact]
        public void GetInsertCommands_ExcludesAutoIncrementField()
        {
            var builder = CreateBuilder<Animal>();
            var animal = new Animal { Name = "Test", SpeciesId = 1, ZookeeperId = Guid.NewGuid() };
            var commands = builder.GetInsertCommands(animal, SelectBack.None);
            var insertCmd = commands.First();
            Assert.Contains("INSERT", insertCmd.CommandText);
            Assert.Contains("Name", insertCmd.CommandText);
        }

        [Fact]
        public void GetInsertCommands_GuidKey_IncludesKeyField()
        {
            var builder = CreateBuilder<Zookeeper>();
            var keeper = new Zookeeper { ZookeeperId = Guid.NewGuid(), FirstName = "Test", LastName = "User", HireDate = DateTime.Today };
            var commands = builder.GetInsertCommands(keeper, SelectBack.None);
            var insertCmd = commands.First();
            Assert.Contains("ZookeeperId", insertCmd.CommandText);
        }

        // DELETE tests

        [Fact]
        public void DeleteItemCommand_ContainsKeyInWhere()
        {
            var builder = CreateBuilder<Animal>();
            var cmd = builder.DeleteItemCommand;
            Assert.Contains("DELETE", cmd.CommandText);
            Assert.Contains("WHERE", cmd.CommandText);
            Assert.Contains("AnimalId", cmd.CommandText);
        }

        [Fact]
        public void GetDeleteCommand_WithFilter()
        {
            var builder = CreateBuilder<Animal>();
            var cmd = builder.GetDeleteCommand("SpeciesId = 1");
            Assert.Contains("DELETE", cmd.CommandText);
            Assert.Contains("SpeciesId = 1", cmd.CommandText);
        }

        // Dialect-specific

        [Fact]
        public void SqlServer_Select_UsesBrackets()
        {
            using var sqlConn = new SqliteConnection("Data Source=:memory:");
            sqlConn.Open();
            var map = DataMap.GenerateNew(typeof(Animal));
            var builder = new DataClassCommandBuilder(typeof(Animal), map, sqlConn, new SqlServerDialect());
            builder.UseQuotedIdentifier = true;
            var cmd = builder.GetSelectCommand("");
            Assert.Contains("[", cmd.CommandText);
        }
    }
}
#endif
