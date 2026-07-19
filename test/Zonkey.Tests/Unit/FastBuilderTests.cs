#if !NETFRAMEWORK
using System;
using System.Data;
using Microsoft.Data.Sqlite;
using Xunit;
using Zonkey;
using Zonkey.ObjectModel;

namespace Zonkey.Tests.Unit
{
    /// <summary>
    /// The fast builder emits IL per (type, result-set shape) to populate objects without
    /// per-field reflection. These tests run every conversion through BOTH the fast and
    /// slow paths and require identical results, and pin the exception contract:
    /// a conversion failure surfaces as PropertyReadException naming the property,
    /// regardless of path.
    /// </summary>
    public class FastBuilderTests : IDisposable
    {
        public enum Habitat { Unknown = 0, Forest = 1, Aquatic = 2 }

        [DataItem("Menagerie")]
        public class Beast : DataClass
        {
            public Beast() : base(false) { }
            public Beast(bool addingNew) : base(addingNew) { }

            [DataField("Id", DbType.Int32, IsKeyField = true)]
            public int Id { get => field; set => SetFieldValue(ref field, value); }

            [DataField("BigCount", DbType.Int64)]
            public long BigCount { get => field; set => SetFieldValue(ref field, value); }

            [DataField("SmallCount", DbType.Int32)]
            public int SmallCount { get => field; set => SetFieldValue(ref field, value); }  // sqlite INTEGER arrives as long -> narrowing convert

            [DataField("Name", DbType.String)]
            public string Name { get => field; set => SetFieldValue(ref field, value); }

            [DataField("Weight", DbType.Decimal, true)]
            public decimal? Weight { get => field; set => SetFieldValue(ref field, value); } // sqlite REAL arrives as double -> nullable convert

            [DataField("Tag", DbType.Guid)]
            public Guid Tag { get => field; set => SetFieldValue(ref field, value); }        // sqlite TEXT -> Guid

            [DataField("AltTag", DbType.Guid, true)]
            public Guid? AltTag { get => field; set => SetFieldValue(ref field, value); }    // sqlite TEXT -> Guid?

            [DataField("IsWild", DbType.Boolean)]
            public bool IsWild { get => field; set => SetFieldValue(ref field, value); }     // sqlite INTEGER -> bool

            [DataField("Home", DbType.Int32)]
            public Habitat Home { get => field; set => SetFieldValue(ref field, value); }    // long -> enum

            [DataField("AltHome", DbType.Int32, true)]
            public Habitat? AltHome { get => field; set => SetFieldValue(ref field, value); } // long -> enum?

            [DataField("BornUtc", DbType.DateTime, true, DateTimeKind = DateTimeKind.Utc)]
            public DateTime? BornUtc { get => field; set => SetFieldValue(ref field, value); }

            [DataField("Photo", DbType.Binary, true)]
            public byte[] Photo { get => field; set => SetFieldValue(ref field, value); }
        }

        private readonly SqliteConnection _conn;
        private static readonly Guid TestTag = Guid.Parse("11111111-2222-3333-4444-555555555555");
        private static readonly Guid TestAltTag = Guid.Parse("AAAAAAAA-BBBB-CCCC-DDDD-EEEEEEEEEEEE");

        public FastBuilderTests()
        {
            _conn = new SqliteConnection("Data Source=:memory:");
            _conn.Open();

            using var cmd = _conn.CreateCommand();
            cmd.CommandText = @"
                CREATE TABLE Menagerie (
                    Id INTEGER PRIMARY KEY, BigCount INTEGER, SmallCount INTEGER, Name TEXT,
                    Weight REAL, Tag TEXT, AltTag TEXT, IsWild INTEGER, Home INTEGER,
                    AltHome INTEGER, BornUtc TEXT, Photo BLOB);
                INSERT INTO Menagerie VALUES
                    (1, 9000000000, 42, 'Zonkey', 350.25, '11111111-2222-3333-4444-555555555555',
                     'AAAAAAAA-BBBB-CCCC-DDDD-EEEEEEEEEEEE', 1, 2, 1, '2020-06-15 10:30:00', X'CAFEBABE'),
                    (2, 5, 7, 'Nullsy', NULL, '11111111-2222-3333-4444-555555555555',
                     NULL, 0, 0, NULL, NULL, NULL);";
            cmd.ExecuteNonQuery();
        }

        public void Dispose() => _conn.Dispose();

        private Beast ReadOne(bool fast, int id)
        {
            using var cmd = _conn.CreateCommand();
            cmd.CommandText = $"SELECT * FROM Menagerie WHERE Id = {id}";
            using var reader = new DataClassReader<Beast>(cmd.ExecuteReader()) { UseFastBuilder = fast };
            return reader.Read();
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void AllConversions_PopulateCorrectly(bool fast)
        {
            var b = ReadOne(fast, 1);

            Assert.Equal(1, b.Id);
            Assert.Equal(9000000000L, b.BigCount);
            Assert.Equal(42, b.SmallCount);
            Assert.Equal("Zonkey", b.Name);
            Assert.Equal(350.25m, b.Weight);
            Assert.Equal(TestTag, b.Tag);
            Assert.Equal(TestAltTag, b.AltTag);
            Assert.True(b.IsWild);
            Assert.Equal(Habitat.Aquatic, b.Home);
            Assert.Equal(Habitat.Forest, b.AltHome);
            Assert.Equal(new DateTime(2020, 6, 15, 10, 30, 0), b.BornUtc);
            Assert.Equal(DateTimeKind.Utc, b.BornUtc!.Value.Kind);
            Assert.Equal(new byte[] { 0xCA, 0xFE, 0xBA, 0xBE }, b.Photo);
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void Nulls_LeavePropertiesAtDefault(bool fast)
        {
            var b = ReadOne(fast, 2);

            Assert.Null(b.Weight);
            Assert.Null(b.AltTag);
            Assert.Null(b.AltHome);
            Assert.Null(b.BornUtc);
            Assert.Null(b.Photo);
            Assert.False(b.IsWild);
            Assert.Equal(Habitat.Unknown, b.Home);
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void MaterializedObjects_AreUnchanged(bool fast)
        {
            var b = ReadOne(fast, 1);
            Assert.Equal(DataRowState.Unchanged, b.DataRowState);
            Assert.Empty(b.OriginalValues);
        }

        [Fact]
        public void FastAndSlow_ProduceIdenticalObjects()
        {
            var fast = ReadOne(true, 1);
            var slow = ReadOne(false, 1);

            foreach (var pi in typeof(Beast).GetProperties())
            {
                if (pi.Name is nameof(DataClass.DataRowState) or nameof(DataClass.OriginalValues)) continue;
                Assert.Equal(pi.GetValue(slow), pi.GetValue(fast));
            }
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void ConversionFailure_ThrowsPropertyReadException_NamingTheProperty(bool fast)
        {
            using var setup = _conn.CreateCommand();
            setup.CommandText = "UPDATE Menagerie SET Tag = 'not-a-guid' WHERE Id = 1";
            setup.ExecuteNonQuery();

            var ex = Assert.Throws<PropertyReadException>(() => ReadOne(fast, 1));
            Assert.Contains("Tag", ex.Message);
        }

        [Fact]
        public void FastBuilder_IsTheDefault()
        {
            using var cmd = _conn.CreateCommand();
            cmd.CommandText = "SELECT * FROM Menagerie WHERE Id = 1";
            using var reader = new DataClassReader<Beast>(cmd.ExecuteReader());
            Assert.True(reader.UseFastBuilder);
        }

        [Fact]
        public void CustomObjectFactory_IsHonored()
        {
            int factoryCalls = 0;
            using var cmd = _conn.CreateCommand();
            cmd.CommandText = "SELECT * FROM Menagerie ORDER BY Id";
            using var reader = new DataClassReader<Beast>(cmd.ExecuteReader())
            {
                UseFastBuilder = true,
                ObjectFactory = () => { factoryCalls++; return new Beast(); }
            };

            var list = reader.ToList();
            Assert.Equal(2, list.Count);
            Assert.Equal(2, factoryCalls);
        }
    }
}
#endif
