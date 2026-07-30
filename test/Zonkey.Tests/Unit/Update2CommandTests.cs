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
    /// GetUpdate2Commands builds the SET clause solely from the object's OriginalValues
    /// (the fields assigned while tracking was armed) and the WHERE clause from the key
    /// fields' current values. This is what makes the "stub update" pattern work:
    /// key assigned while Detached (untracked), CommitValues() to arm tracking, then
    /// assign only the fields to change.
    /// </summary>
    public class Update2CommandTests : IDisposable
    {
        private readonly SqliteConnection _conn;

        public Update2CommandTests()
        {
            _conn = new SqliteConnection("Data Source=:memory:");
            _conn.Open();
        }

        public void Dispose() => _conn.Dispose();

        private DataClassCommandBuilder CreateBuilder()
        {
            var map = DataMap.GenerateNew(typeof(OrderLog));
            return new DataClassCommandBuilder(typeof(OrderLog), map, _conn, new SqliteDialect());
        }

        private static OrderLog CreateStub(int id)
        {
            var stub = new OrderLog(false) { Id = id }; // Detached: nothing tracked
            stub.CommitValues();                        // -> Unchanged: tracking armed
            return stub;
        }

        [Fact]
        public void Stub_SetClause_ContainsOnlyAssignedField()
        {
            var stub = CreateStub(7);
            stub.Order = 42; // tracked; state -> Modified

            var sql = CreateBuilder().GetUpdate2Commands(stub, UpdateCriteria.KeyOnly, false)[0].CommandText;
            var setPart = sql.Substring(0, sql.IndexOf("WHERE", StringComparison.Ordinal));

            Assert.StartsWith("UPDATE", sql);
            Assert.Contains("[Order] =", setPart);
            Assert.DoesNotContain("[Note]", setPart);
            Assert.DoesNotContain("[Id]", setPart); // key set while Detached stays out of SET
        }

        [Fact]
        public void Stub_KeyOnly_WhereContainsOnlyKey()
        {
            var stub = CreateStub(7);
            stub.Order = 42;

            var sql = CreateBuilder().GetUpdate2Commands(stub, UpdateCriteria.KeyOnly, false)[0].CommandText;
            var wherePart = sql.Substring(sql.IndexOf("WHERE", StringComparison.Ordinal));

            Assert.Contains("[Id] =", wherePart);
            Assert.DoesNotContain("[Order]", wherePart);
        }

        [Fact]
        public void Stub_ChangedFieldsCriteria_PutsTrackedOriginalsInWhere()
        {
            // this is why the stub pattern requires KeyOnly: the tracked "original" is
            // the stale field default, not the database value
            var stub = CreateStub(7);
            stub.Order = 42;

            var sql = CreateBuilder().GetUpdate2Commands(stub, UpdateCriteria.ChangedFields, false)[0].CommandText;
            var wherePart = sql.Substring(sql.IndexOf("WHERE", StringComparison.Ordinal));

            Assert.Contains("[Order]", wherePart);
        }

        [Fact]
        public void NoTrackedFields_ReturnsNull()
        {
            var stub = CreateStub(7); // nothing assigned after CommitValues

            Assert.Null(CreateBuilder().GetUpdate2Commands(stub, UpdateCriteria.KeyOnly, false));
        }

        [Fact]
        public void AllFieldsCriteria_IsRejected()
        {
            var stub = CreateStub(7);
            stub.Order = 42;

            Assert.Throws<ArgumentException>(() =>
                CreateBuilder().GetUpdate2Commands(stub, UpdateCriteria.AllFields, false));
        }

        [Fact]
        public void SelectBack_ReturnsSecondCommandKeyedSelect()
        {
            var stub = CreateStub(7);
            stub.Order = 42;

            var commands = CreateBuilder().GetUpdate2Commands(stub, UpdateCriteria.KeyOnly, true);
            Assert.Equal(2, commands.Length);
            Assert.StartsWith("SELECT", commands[1].CommandText);
            Assert.Contains("[Id] =", commands[1].CommandText);
        }
    }
}
#endif
