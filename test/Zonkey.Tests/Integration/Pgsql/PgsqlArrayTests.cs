#if !NETFRAMEWORK
using System.Data;
using System.Data.Common;
using System.Threading.Tasks;
using Npgsql;
using NpgsqlTypes;
using Xunit;
using Zonkey;
using Zonkey.ObjectModel;
using Zonkey.Tests.Infrastructure;

namespace Zonkey.Tests.Integration.Pgsql
{
    /// <summary>
    /// End-to-end PostgreSQL array and jsonb support: typed array properties
    /// (string[], int[]) fill from text[]/integer[] columns, and writes flow through
    /// the NativeType / UseTypeSetter mechanism documented in docs/postgresql.md.
    /// </summary>
    public class PgsqlArrayTests : IClassFixture<PgsqlFixture>
    {
        private readonly PgsqlFixture _db;

        static PgsqlArrayTests()
        {
            // the canonical Npgsql helper from docs/postgresql.md
            DbParameterExtensions.UseTypeSetter<NpgsqlParameter>(DbType.Object, (p, f) =>
            {
                if (p is not NpgsqlParameter n) return;

                if (f.NativeType is NpgsqlDbType nt)
                    n.NpgsqlDbType = nt;
            });
        }

        public PgsqlArrayTests(PgsqlFixture db) => _db = db;

        [DataItem("array_zone")]
        public class ArrayDoc : DataClass
        {
            public ArrayDoc() : base(false) { }
            public ArrayDoc(bool addingNew) : base(addingNew) { }

            [DataField("id", DbType.Int32, IsKeyField = true, IsAutoIncrement = true)]
            public int Id { get => field; set => SetFieldValue(ref field, value); }

            [DataField("tags", DbType.Object, true, NativeType = NpgsqlDbType.Array | NpgsqlDbType.Text)]
            public string[] Tags { get => field; set => SetFieldValue(ref field, value); }

            [DataField("nums", DbType.Object, true, NativeType = NpgsqlDbType.Array | NpgsqlDbType.Integer)]
            public int[] Nums { get => field; set => SetFieldValue(ref field, value); }

            [DataField("doc", DbType.Object, true, NativeType = NpgsqlDbType.Jsonb)]
            public string Doc { get => field; set => SetFieldValue(ref field, value); }
        }

        private static async Task EnsureTable(DbConnection conn)
        {
            using var cmd = conn.CreateCommand();
            cmd.CommandText = "CREATE TABLE IF NOT EXISTS array_zone (id SERIAL PRIMARY KEY, tags TEXT[], nums INTEGER[], doc JSONB)";
            await cmd.ExecuteNonQueryAsync();
        }

        [Fact]
        public async Task ArraysAndJsonb_InsertAndFill_RoundTrip()
        {
            if (!_db.IsAvailable) Assert.Skip(_db.SkipReason);

            using var conn = _db.CreateConnection();
            await EnsureTable(conn);

            var adapter = new DataClassAdapter<ArrayDoc>(conn);

            var doc = new ArrayDoc(addingNew: true)
            {
                Tags = new[] { "urgent", "parking" },
                Nums = new[] { 10, 20, 30 },
                Doc = "{\"kind\": \"analysis\", \"level\": 3}"
            };

            Assert.True(await adapter.Save(doc));
            Assert.True(doc.Id > 0);

            var fetched = await adapter.GetOne(d => d.Id == doc.Id);
            Assert.Equal(new[] { "urgent", "parking" }, fetched.Tags);
            Assert.Equal(new[] { 10, 20, 30 }, fetched.Nums);
            Assert.Contains("\"kind\": \"analysis\"", fetched.Doc);

            await adapter.DeleteItem(fetched);
        }

        [Fact]
        public async Task Arrays_Update_RoundTrips()
        {
            if (!_db.IsAvailable) Assert.Skip(_db.SkipReason);

            using var conn = _db.CreateConnection();
            await EnsureTable(conn);

            var adapter = new DataClassAdapter<ArrayDoc>(conn);

            var doc = new ArrayDoc(addingNew: true) { Tags = new[] { "first" }, Nums = new[] { 1 } };
            await adapter.Save(doc);

            var loaded = await adapter.GetOne(d => d.Id == doc.Id);
            loaded.Tags = new[] { "first", "second" };
            loaded.Nums = new[] { 1, 2 };
            Assert.True(await adapter.Save(loaded));

            var reloaded = await adapter.GetOne(d => d.Id == doc.Id);
            Assert.Equal(new[] { "first", "second" }, reloaded.Tags);
            Assert.Equal(new[] { 1, 2 }, reloaded.Nums);

            await adapter.DeleteItem(reloaded);
        }

        [Fact]
        public async Task NullArrays_RoundTrip()
        {
            if (!_db.IsAvailable) Assert.Skip(_db.SkipReason);

            using var conn = _db.CreateConnection();
            await EnsureTable(conn);

            var adapter = new DataClassAdapter<ArrayDoc>(conn);

            var doc = new ArrayDoc(addingNew: true); // everything null
            Assert.True(await adapter.Save(doc));

            var fetched = await adapter.GetOne(d => d.Id == doc.Id);
            Assert.Null(fetched.Tags);
            Assert.Null(fetched.Nums);
            Assert.Null(fetched.Doc);

            await adapter.DeleteItem(fetched);
        }
    }
}
#endif
