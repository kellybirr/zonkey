#if !NETFRAMEWORK
using System.Threading.Tasks;
using Xunit;
using Zonkey;
using Zonkey.Tests.Infrastructure;
using Zonkey.Tests.Models;

namespace Zonkey.Tests.Integration.Sqlite
{
    /// <summary>
    /// End-to-end proof that identifier quoting matters on SQLite: the "Order Log" table
    /// (space in the name) with an "Order" column (reserved word) round-trips with the
    /// dialect's quote-by-default behavior, and fails when quoting is explicitly disabled.
    /// </summary>
    public class SqliteQuotedIdentifierTests : IClassFixture<SqliteFixture>
    {
        private readonly SqliteFixture _db;

        public SqliteQuotedIdentifierTests(SqliteFixture db) => _db = db;

        private static async Task EnsureTable(System.Data.Common.DbConnection conn)
        {
            using var cmd = conn.CreateCommand();
            cmd.CommandText =
                "CREATE TABLE IF NOT EXISTS [Order Log] (" +
                "[Id] INTEGER PRIMARY KEY AUTOINCREMENT, " +
                "[Order] INTEGER NOT NULL, " +
                "[Note] TEXT)";
            await cmd.ExecuteNonQueryAsync();
        }

        [Fact]
        public async Task QuotingOn_ReservedNames_RoundTrip()
        {
            using var conn = _db.CreateConnection();
            await EnsureTable(conn);

            var adapter = new DataClassAdapter<OrderLog>(conn); // Sqlite quotes by default

            var log = new OrderLog { Order = 42, Note = "quoted round trip" };
            Assert.True(await adapter.Save(log));
            Assert.True(log.Id > 0);

            var fetched = await adapter.GetOne(o => o.Order == 42);
            Assert.NotNull(fetched);
            Assert.Equal("quoted round trip", fetched.Note);

            await adapter.DeleteItem(fetched);
        }

        [Fact]
        public async Task QuotingOff_ReservedNames_FailsWithSyntaxError()
        {
            using var conn = _db.CreateConnection();
            await EnsureTable(conn);

            var adapter = new DataClassAdapter<OrderLog>(conn);
            adapter.SetProperty(AdapterProperty.UseQuotedIdentifiers, false);

            var log = new OrderLog { Order = 1, Note = "should never be saved" };
            await Assert.ThrowsAsync<SaveFailedException>(() => adapter.Save(log));
        }
    }
}
#endif
