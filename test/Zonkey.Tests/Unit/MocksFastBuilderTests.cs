using System;
using System.Data;
using System.Data.Common;
using Xunit;
using Zonkey;
using Zonkey.Mocks;
using Zonkey.ObjectModel;

namespace Zonkey.Tests.Unit
{
    /// <summary>
    /// Fast-builder coverage over a DataTable-backed mock reader, which supplies
    /// natively typed columns (Guid, DateTime, decimal, byte[]) the way SQL Server
    /// and PostgreSQL providers do -- and, unlike the SQLite-based tests, runs on
    /// .NET Framework as well, exercising the emitted IL on both CLRs.
    /// </summary>
    public class MocksFastBuilderTests
    {
        public enum Species { None = 0, Zebra = 1, Donkey = 2 }

        public class FakeSqlHierarchyId
        {
            public override string ToString() => "/1/3/";
        }

        [DataItem("Hybrids")]
        public class Hybrid : DataClass
        {
            private int _id;
            private string _name;
            private Guid _tag;
            private Guid? _altTag;
            private decimal _price;
            private int? _count;
            private bool _active;
            private Species _kind;
            private DateTime _seenUtc;
            private byte[] _photo;
            private string _path;

            public Hybrid() : base(false) { }
            public Hybrid(bool addingNew) : base(addingNew) { }

            [DataField("Id", DbType.Int32, IsKeyField = true)]
            public int Id { get => _id; set => SetFieldValue(ref _id, value); }

            [DataField("Name", DbType.String, true)]
            public string Name { get => _name; set => SetFieldValue(ref _name, value); }

            [DataField("Tag", DbType.Guid)]
            public Guid Tag { get => _tag; set => SetFieldValue(ref _tag, value); }          // native Guid column

            [DataField("AltTag", DbType.Guid, true)]
            public Guid? AltTag { get => _altTag; set => SetFieldValue(ref _altTag, value); }

            [DataField("Price", DbType.Decimal)]
            public decimal Price { get => _price; set => SetFieldValue(ref _price, value); } // native decimal column

            [DataField("Count", DbType.Int32, true)]
            public int? Count { get => _count; set => SetFieldValue(ref _count, value); }

            [DataField("Active", DbType.Boolean)]
            public bool Active { get => _active; set => SetFieldValue(ref _active, value); }

            [DataField("Kind", DbType.Int32)]
            public Species Kind { get => _kind; set => SetFieldValue(ref _kind, value); }

            [DataField("SeenUtc", DbType.DateTime, DateTimeKind = DateTimeKind.Utc)]
            public DateTime SeenUtc { get => _seenUtc; set => SetFieldValue(ref _seenUtc, value); } // native DateTime + Kind stamp

            [DataField("Photo", DbType.Binary, true)]
            public byte[] Photo { get => _photo; set => SetFieldValue(ref _photo, value); }

            [DataField("Path", DbType.String, true)]
            public string Path { get => _path; set => SetFieldValue(ref _path, value); }     // FakeSqlHierarchyId column -> string
        }

        private static readonly Guid TestTag = Guid.Parse("99999999-8888-7777-6666-555555555555");

        private static DataTable BuildTable(bool withNulls)
        {
            var table = new DataTable("Hybrids");
            table.Columns.Add("Id", typeof(int));
            table.Columns.Add("Name", typeof(string));
            table.Columns.Add("Tag", typeof(Guid));
            table.Columns.Add("AltTag", typeof(Guid));
            table.Columns.Add("Price", typeof(decimal));
            table.Columns.Add("Count", typeof(int));
            table.Columns.Add("Active", typeof(bool));
            table.Columns.Add("Kind", typeof(int));
            table.Columns.Add("SeenUtc", typeof(DateTime));
            table.Columns.Add("Photo", typeof(byte[]));
            table.Columns.Add("Path", typeof(FakeSqlHierarchyId));

            if (withNulls)
                table.Rows.Add(2, DBNull.Value, TestTag, DBNull.Value, 1.5m, DBNull.Value, false, 0,
                               new DateTime(2024, 1, 1), DBNull.Value, DBNull.Value);
            else
                table.Rows.Add(1, "Zonkey", TestTag, TestTag, 129.95m, 4, true, 2,
                               new DateTime(2023, 5, 20, 14, 0, 0), new byte[] { 1, 2, 3 }, new FakeSqlHierarchyId());

            return table;
        }

        private static Hybrid ReadOne(bool fast, bool withNulls)
        {
            var conn = new MockDbConnection();
            conn.Open();
            conn.SetupCommandFunc = cmd => cmd.DoExecuteReader = _ => BuildTable(withNulls);

            using DbCommand command = conn.CreateCommand();
            command.CommandText = "SELECT * FROM Hybrids";
            using var reader = new DataClassReader<Hybrid>(command.ExecuteReader()) { UseFastBuilder = fast };
            return reader.Read();
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void NativeTypedColumns_PopulateCorrectly(bool fast)
        {
            var h = ReadOne(fast, withNulls: false);

            Assert.Equal(1, h.Id);
            Assert.Equal("Zonkey", h.Name);
            Assert.Equal(TestTag, h.Tag);
            Assert.Equal(TestTag, h.AltTag);
            Assert.Equal(129.95m, h.Price);
            Assert.Equal(4, h.Count);
            Assert.True(h.Active);
            Assert.Equal(Species.Donkey, h.Kind);
            Assert.Equal(new DateTime(2023, 5, 20, 14, 0, 0), h.SeenUtc);
            Assert.Equal(DateTimeKind.Utc, h.SeenUtc.Kind);
            Assert.Equal(new byte[] { 1, 2, 3 }, h.Photo);
            Assert.Equal("/1/3/", h.Path);
            Assert.Equal(DataRowState.Unchanged, h.DataRowState);
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void NativeTypedColumns_NullsLeaveDefaults(bool fast)
        {
            var h = ReadOne(fast, withNulls: true);

            Assert.Equal(2, h.Id);
            Assert.Null(h.Name);
            Assert.Null(h.AltTag);
            Assert.Null(h.Count);
            Assert.Null(h.Photo);
            Assert.Null(h.Path);
            Assert.False(h.Active);
            Assert.Equal(Species.None, h.Kind);
        }

        [Fact]
        public void NativeTypedColumns_FastMatchesSlow()
        {
            var fast = ReadOne(true, withNulls: false);
            var slow = ReadOne(false, withNulls: false);

            foreach (var pi in typeof(Hybrid).GetProperties())
            {
                if (pi.Name is nameof(DataClass.DataRowState) or nameof(DataClass.OriginalValues)) continue;
                Assert.Equal(pi.GetValue(slow), pi.GetValue(fast));
            }
        }
    }
}
