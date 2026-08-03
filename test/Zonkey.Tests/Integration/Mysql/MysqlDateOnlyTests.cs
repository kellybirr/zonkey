#if !NETFRAMEWORK
using System;
using System.Data;
using System.Data.Common;
using System.Threading.Tasks;
using Xunit;
using Zonkey;
using Zonkey.ObjectModel;
using Zonkey.Tests.Infrastructure;

namespace Zonkey.Tests.Integration.Mysql
{
    /// <summary>
    /// MySQL DATE/TIME column round-trips. MySqlConnector surfaces DATE as DateTime and
    /// TIME as TimeSpan, but accepts DateOnly/TimeOnly parameter values -- modern
    /// DateOnly/TimeOnly entities must write natively and fill via Zonkey's conversions,
    /// on both materialization paths.
    /// </summary>
    public class MysqlDateOnlyTests : IClassFixture<MysqlFixture>
    {
        private readonly MysqlFixture _db;

        public MysqlDateOnlyTests(MysqlFixture db) => _db = db;

        [DataItem("date_probe")]
        public class ModernRow : DataClass
        {
            public ModernRow() : base(false) { }
            public ModernRow(bool addingNew) : base(addingNew) { }

            [DataField("id", DbType.Int32, IsKeyField = true, IsAutoIncrement = true)]
            public int Id { get => field; set => SetFieldValue(ref field, value); }

            [DataField("d", DbType.Date, true)]
            public DateOnly? D { get => field; set => SetFieldValue(ref field, value); }

            [DataField("t", DbType.Time, true)]
            public TimeOnly? T { get => field; set => SetFieldValue(ref field, value); }
        }

        [DataItem("date_probe")]
        public class LegacyRow : DataClass
        {
            public LegacyRow() : base(false) { }
            public LegacyRow(bool addingNew) : base(addingNew) { }

            [DataField("id", DbType.Int32, IsKeyField = true, IsAutoIncrement = true)]
            public int Id { get => field; set => SetFieldValue(ref field, value); }

            [DataField("d", DbType.Date, true)]
            public DateTime? D { get => field; set => SetFieldValue(ref field, value); }

            [DataField("t", DbType.Time, true)]
            public TimeSpan? T { get => field; set => SetFieldValue(ref field, value); }
        }

        [DataItem("date_probe")]
        public class TextRow : DataClass
        {
            public TextRow() : base(false) { }
            public TextRow(bool addingNew) : base(addingNew) { }

            [DataField("id", DbType.Int32, IsKeyField = true, IsAutoIncrement = true)]
            public int Id { get => field; set => SetFieldValue(ref field, value); }

            [DataField("d", DbType.Date, true)]
            public string D { get => field; set => SetFieldValue(ref field, value); }

            [DataField("t", DbType.Time, true)]
            public string T { get => field; set => SetFieldValue(ref field, value); }
        }

        private static async Task EnsureTable(DbConnection conn)
        {
            using var cmd = conn.CreateCommand();
            cmd.CommandText = "CREATE TABLE IF NOT EXISTS date_probe (id INT AUTO_INCREMENT PRIMARY KEY, d DATE NULL, t TIME NULL)";
            await cmd.ExecuteNonQueryAsync();
        }

        [Fact]
        public async Task Modern_SaveAndReload_RoundTrips_BothModes()
        {
            if (!_db.IsAvailable) Assert.Skip(_db.SkipReason);

            using var conn = _db.CreateConnection();
            await EnsureTable(conn);
            var adapter = new DataClassAdapter<ModernRow>(conn);

            var row = new ModernRow(addingNew: true) { D = new DateOnly(2024, 5, 20), T = new TimeOnly(14, 30, 15) };
            Assert.True(await adapter.Save(row));
            Assert.True(row.Id > 0);

            foreach (bool fast in new[] { true, false })
            {
                using var reader = await adapter.OpenReader(r => r.Id == row.Id);
                reader.UseFastBuilder = fast;
                var back = await reader.ReadAsync();
                Assert.Equal(new DateOnly(2024, 5, 20), back.D);
                Assert.Equal(new TimeOnly(14, 30, 15), back.T);
            }
        }

        [Fact]
        public async Task Legacy_DateTimeAndTimeSpan_RoundTrips_BothModes()
        {
            if (!_db.IsAvailable) Assert.Skip(_db.SkipReason);

            using var conn = _db.CreateConnection();
            await EnsureTable(conn);
            var adapter = new DataClassAdapter<LegacyRow>(conn);

            var row = new LegacyRow(addingNew: true) { D = new DateTime(2023, 11, 5), T = new TimeSpan(8, 15, 42) };
            Assert.True(await adapter.Save(row));

            foreach (bool fast in new[] { true, false })
            {
                using var reader = await adapter.OpenReader(r => r.Id == row.Id);
                reader.UseFastBuilder = fast;
                var back = await reader.ReadAsync();
                Assert.Equal(new DateTime(2023, 11, 5), back.D);
                Assert.Equal(new TimeSpan(8, 15, 42), back.T);
            }
        }

        [Fact]
        public async Task Text_ReadsIsoStrings_BothModes()
        {
            if (!_db.IsAvailable) Assert.Skip(_db.SkipReason);

            using var conn = _db.CreateConnection();
            await EnsureTable(conn);

            var writeAdapter = new DataClassAdapter<ModernRow>(conn);
            var row = new ModernRow(addingNew: true) { D = new DateOnly(2022, 2, 2), T = new TimeOnly(6, 7, 8) };
            Assert.True(await writeAdapter.Save(row));

            var adapter = new DataClassAdapter<TextRow>(conn);
            foreach (bool fast in new[] { true, false })
            {
                using var reader = await adapter.OpenReader(r => r.Id == row.Id);
                reader.UseFastBuilder = fast;
                var back = await reader.ReadAsync();
                Assert.StartsWith("2022-02-02", back.D);
                Assert.StartsWith("06:07:08", back.T);
            }
        }
    }
}
#endif
