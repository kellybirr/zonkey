#if !NETFRAMEWORK
using System.Data;
using System.Threading.Tasks;
using Xunit;
using Zonkey;
using Zonkey.ObjectModel;
using Zonkey.Tests.Infrastructure;

namespace Zonkey.Tests.Integration.Pgsql
{
    /// <summary>
    /// End-to-end proof of the PostgreSQL case-folding trap: a schema created with quoted
    /// PascalCase identifiers is only reachable when the adapter quotes too. With quoting
    /// enabled the round-trip works; with Zonkey's default (unquoted) SQL the identifiers
    /// fold to lowercase and PostgreSQL reports the relation as missing.
    /// </summary>
    public class PgsqlQuotedIdentifierTests : IClassFixture<PgsqlFixture>
    {
        private readonly PgsqlFixture _db;

        public PgsqlQuotedIdentifierTests(PgsqlFixture db) => _db = db;

        private static async Task EnsureTable(System.Data.Common.DbConnection conn)
        {
            using var cmd = conn.CreateCommand();
            cmd.CommandText =
                "CREATE TABLE IF NOT EXISTS \"QuotedZone\" (" +
                "\"ZoneId\" SERIAL PRIMARY KEY, " +
                "\"ZoneName\" VARCHAR(50) NOT NULL)";
            await cmd.ExecuteNonQueryAsync();
        }

        [Fact]
        public async Task QuotingOn_PascalCaseSchema_RoundTrip()
        {
            if (!_db.IsAvailable) Assert.Skip(_db.SkipReason);

            using var conn = _db.CreateConnection();
            await EnsureTable(conn);

            var adapter = new DataClassAdapter<QuotedZone>(conn);
            adapter.SetProperty(AdapterProperty.UseQuotedIdentifiers, true);

            var zone = new QuotedZone { ZoneName = "North" };
            Assert.True(await adapter.Save(zone));
            Assert.True(zone.ZoneId > 0);

            var fetched = await adapter.GetOne(z => z.ZoneId == zone.ZoneId);
            Assert.NotNull(fetched);
            Assert.Equal("North", fetched.ZoneName);
        }

        [Fact]
        public async Task QuotingOff_PascalCaseSchema_FailsWithMissingRelation()
        {
            if (!_db.IsAvailable) Assert.Skip(_db.SkipReason);

            using var conn = _db.CreateConnection();
            await EnsureTable(conn);

            // default: PostgreSqlDialect only quotes when explicitly enabled, so the
            // generated SQL folds to lowercase and misses the "QuotedZone" table
            var adapter = new DataClassAdapter<QuotedZone>(conn);

            var zone = new QuotedZone { ZoneName = "South" };
            await Assert.ThrowsAsync<SaveFailedException>(() => adapter.Save(zone));
        }
    }

    [DataItem("QuotedZone")]
    public class QuotedZone : DataClass
    {
        private int _zoneId;
        private string _zoneName;

        public QuotedZone() : base(true) { }
        public QuotedZone(bool addingNew) : base(addingNew) { }

        [DataField("ZoneId", DbType.Int32, IsKeyField = true, IsAutoIncrement = true)]
        public int ZoneId
        {
            get => _zoneId;
            set => SetFieldValue(ref _zoneId, value);
        }

        [DataField("ZoneName", DbType.String)]
        public string ZoneName
        {
            get => _zoneName;
            set => SetFieldValue(ref _zoneName, value);
        }
    }
}
#endif
