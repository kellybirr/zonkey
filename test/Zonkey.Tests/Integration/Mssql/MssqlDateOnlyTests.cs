#if !NETFRAMEWORK
using System;
using System.Data;
using System.Data.Common;
using System.Threading.Tasks;
using Xunit;
using Zonkey;
using Zonkey.ObjectModel;
using Zonkey.Tests.Infrastructure;

namespace Zonkey.Tests.Integration.Mssql
{
    /// <summary>
    /// SQL Server date/time column round-trips. Microsoft.Data.SqlClient surfaces
    /// date as DateTime and time as TimeSpan, but accepts DateOnly/TimeOnly parameter
    /// values -- so modern DateOnly/TimeOnly entities must write natively and fill via
    /// Zonkey's conversions, on both materialization paths. If a future SqlClient major
    /// ever flips its defaults the way Npgsql 10 did, these tests keep passing.
    /// </summary>
    public class MssqlDateOnlyTests : IClassFixture<MssqlFixture>
    {
        private readonly MssqlFixture _db;

        public MssqlDateOnlyTests(MssqlFixture db) => _db = db;

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
            cmd.CommandText = "IF OBJECT_ID('date_probe', 'U') IS NULL CREATE TABLE date_probe (id INT IDENTITY PRIMARY KEY, d date NULL, t time NULL)";
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
