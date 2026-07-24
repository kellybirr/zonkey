#if !NETFRAMEWORK
using System;
using System.Collections.Generic;
using System.Data;
using Microsoft.Data.Sqlite;
using Xunit;
using Zonkey;
using Zonkey.ObjectModel;

namespace Zonkey.Tests.Unit
{
    /// <summary>
    /// Structural, conversion-matrix, and exception-contract coverage for the fast
    /// builder, always checked against the reflection path for parity.
    /// </summary>
    public class FastBuilderEdgeTests : IDisposable
    {
        public enum Rank { None = 0, Low = 1, High = 2 }

        [DataItem("Critters")]
        public class Critter : DataClass
        {
            public Critter() : base(false) { }
            public Critter(bool addingNew) : base(addingNew) { }

            [DataField("Id", DbType.Int32, IsKeyField = true)]
            public int Id { get => field; set => SetFieldValue(ref field, value); }

            [DataField("Name", DbType.String, true)]
            public string Name { get => field; set => SetFieldValue(ref field, value); }

            [DataField("Score", DbType.Int32, true)]
            public int? Score { get => field; set => SetFieldValue(ref field, value); }

            [DataField("Level", DbType.Int16)]
            public short Level { get => field; set => SetFieldValue(ref field, value); }

            [DataField("Tiny", DbType.Byte)]
            public byte Tiny { get => field; set => SetFieldValue(ref field, value); }

            [DataField("Ratio", DbType.Single)]
            public float Ratio { get => field; set => SetFieldValue(ref field, value); }

            [DataField("Grade", DbType.String)]
            public char Grade { get => field; set => SetFieldValue(ref field, value); }

            [DataField("Label", DbType.String)]
            public string Label { get => field; set => SetFieldValue(ref field, value); }        // INTEGER column -> string property

            [DataField("TextNum", DbType.Int32)]
            public int TextNum { get => field; set => SetFieldValue(ref field, value); }         // TEXT column '123' -> int

            [DataField("TextAmount", DbType.Decimal)]
            public decimal TextAmount { get => field; set => SetFieldValue(ref field, value); }  // TEXT column '19.99' -> decimal

            [DataField("TextFlag", DbType.Boolean)]
            public bool TextFlag { get => field; set => SetFieldValue(ref field, value); }       // TEXT column 'true' -> bool

            [DataField("Rounded", DbType.Int32)]
            public int Rounded { get => field; set => SetFieldValue(ref field, value); }         // REAL column -> int (rounds)

            [DataField("Fee", DbType.Decimal)]
            public decimal Fee { get => field; set => SetFieldValue(ref field, value); }         // INTEGER column -> decimal

            [DataField("Rating", DbType.Int32)]
            public Rank Rating { get => field; set => SetFieldValue(ref field, value); }

            [DataField("Born", DbType.DateTime, DateTimeKind = DateTimeKind.Utc)]
            public DateTime Born { get => field; set => SetFieldValue(ref field, value); }       // TEXT -> DateTime with Kind stamped

            [DataField("HatchDate", DbType.Date, true)]
            public DateOnly? HatchDate { get => field; set => SetFieldValue(ref field, value); } // TEXT -> DateOnly? via Parse

            [DataField("FeedTime", DbType.Time, true)]
            public TimeOnly? FeedTime { get => field; set => SetFieldValue(ref field, value); }  // TEXT -> TimeOnly? via Parse
        }

        private readonly SqliteConnection _conn;

        public FastBuilderEdgeTests()
        {
            _conn = new SqliteConnection("Data Source=:memory:");
            _conn.Open();

            using var cmd = _conn.CreateCommand();
            cmd.CommandText = @"
                CREATE TABLE Critters (
                    Id INTEGER PRIMARY KEY, Name TEXT, Score INTEGER, Level INTEGER, Tiny INTEGER,
                    Ratio REAL, Grade TEXT, Label INTEGER, TextNum TEXT, TextAmount TEXT,
                    TextFlag TEXT, Rounded REAL, Fee INTEGER, Rating INTEGER, Born TEXT,
                    HatchDate TEXT, FeedTime TEXT);
                INSERT INTO Critters VALUES
                    (1, 'Ziggy', 88, 3, 7, 0.5, 'A', 12345, '123', '19.99', 'true', 2.6, 42, 2,
                     '2021-04-01 08:15:00', '2021-03-15', '07:45:00');";
            cmd.ExecuteNonQuery();
        }

        public void Dispose() => _conn.Dispose();

        private List<Critter> Query(string sql, bool fast)
        {
            using var cmd = _conn.CreateCommand();
            cmd.CommandText = sql;
            using var reader = new DataClassReader<Critter>(cmd.ExecuteReader()) { UseFastBuilder = fast };
            return reader.ToList();
        }

        // ---- conversion matrix ----

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void ConversionMatrix_AllPathsCorrect(bool fast)
        {
            var c = Query("SELECT * FROM Critters", fast)[0];

            Assert.Equal(1, c.Id);
            Assert.Equal("Ziggy", c.Name);
            Assert.Equal(88, c.Score);
            Assert.Equal((short)3, c.Level);
            Assert.Equal((byte)7, c.Tiny);
            Assert.Equal(0.5f, c.Ratio);
            Assert.Equal('A', c.Grade);
            Assert.Equal("12345", c.Label);
            Assert.Equal(123, c.TextNum);
            Assert.Equal(19.99m, c.TextAmount);
            Assert.True(c.TextFlag);
            Assert.Equal(3, c.Rounded); // Convert.ChangeType rounds 2.6 -> 3
            Assert.Equal(42m, c.Fee);
            Assert.Equal(Rank.High, c.Rating);
            Assert.Equal(new DateTime(2021, 4, 1, 8, 15, 0), c.Born);
            Assert.Equal(DateTimeKind.Utc, c.Born.Kind);
            Assert.Equal(new DateOnly(2021, 3, 15), c.HatchDate);
            Assert.Equal(new TimeOnly(7, 45), c.FeedTime);
        }

        [Fact]
        public void ConversionMatrix_FastMatchesSlow_PropertyByProperty()
        {
            var fast = Query("SELECT * FROM Critters", true)[0];
            var slow = Query("SELECT * FROM Critters", false)[0];

            foreach (var pi in typeof(Critter).GetProperties())
            {
                if (pi.Name is nameof(DataClass.DataRowState) or nameof(DataClass.OriginalValues)) continue;
                Assert.Equal(pi.GetValue(slow), pi.GetValue(fast));
            }
        }

        // ---- structural cases ----

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void ExtraUnmappedColumn_IsIgnored(bool fast)
        {
            var c = Query("SELECT Id, Name, 'noise' AS Bogus FROM Critters", fast)[0];
            Assert.Equal("Ziggy", c.Name);
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void MissingColumns_LeavePropertiesAtDefault(bool fast)
        {
            var c = Query("SELECT Id, Name FROM Critters", fast)[0];
            Assert.Equal("Ziggy", c.Name);
            Assert.Null(c.Score);
            Assert.Equal(0, c.Level);
            Assert.Equal(Rank.None, c.Rating);
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void PermutedColumnOrder_MapsByName(bool fast)
        {
            var c = Query("SELECT Name, Score, Id FROM Critters", fast)[0];
            Assert.Equal(1, c.Id);
            Assert.Equal("Ziggy", c.Name);
            Assert.Equal(88, c.Score);
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void ColumnNameMatching_IsCaseInsensitive(bool fast)
        {
            var c = Query("SELECT Id AS id, Name AS NAME FROM Critters", fast)[0];
            Assert.Equal(1, c.Id);
            Assert.Equal("Ziggy", c.Name);
        }

        [Fact]
        public void SameType_TwoResultShapes_GetIndependentBuilders()
        {
            var narrow = Query("SELECT Id, Name FROM Critters", true)[0];
            var wide = Query("SELECT * FROM Critters", true)[0];

            Assert.Null(narrow.Score);
            Assert.Equal(88, wide.Score);
            Assert.Equal(narrow.Name, wide.Name);
        }

        // ---- non-DataClass and visibility cases ----

        [DataItem("Critters")]
        public class CritterPoco   // plain class: no DataClass, no ISavable
        {
            [DataField("Id", DbType.Int32, IsKeyField = true)]
            public int Id { get; set; }

            [DataField("Name", DbType.String, true)]
            public string Name { get; private set; } // private setter: needs skipVisibility IL
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void Poco_WithPrivateSetter_Populates(bool fast)
        {
            using var cmd = _conn.CreateCommand();
            cmd.CommandText = "SELECT Id, Name FROM Critters";
            using var reader = new DataClassReader<CritterPoco>(cmd.ExecuteReader()) { UseFastBuilder = fast };
            var c = reader.Read();

            Assert.Equal(1, c.Id);
            Assert.Equal("Ziggy", c.Name);
        }

        // ---- exception contract ----

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void FailureOnLaterColumn_AttributesTheRightProperty(bool fast)
        {
            using (var upd = _conn.CreateCommand())
            {
                upd.CommandText = "UPDATE Critters SET TextAmount = 'not-a-number' WHERE Id = 1";
                upd.ExecuteNonQuery();
            }

            var ex = Assert.Throws<PropertyReadException>(() => Query("SELECT * FROM Critters", fast));
            Assert.Equal(nameof(Critter.TextAmount), ex.Property.Name);
            Assert.Equal("not-a-number", ex.FieldValue);
        }

        [Fact]
        public void FactoryFailure_PropagatesOriginalException()
        {
            using var cmd = _conn.CreateCommand();
            cmd.CommandText = "SELECT * FROM Critters";
            using var reader = new DataClassReader<Critter>(cmd.ExecuteReader())
            {
                UseFastBuilder = true,
                ObjectFactory = () => throw new InvalidOperationException("factory boom")
            };

            var ex = Assert.Throws<InvalidOperationException>(() => reader.Read());
            Assert.Equal("factory boom", ex.Message);
        }

        // ---- string-sourced enums: names and numeric strings, case-insensitive,
        //      identical on both paths; invalid values throw PropertyReadException ----

        [DataItem("EnumSource")]
        public class EnumBeast : DataClass
        {
            public EnumBeast() : base(false) { }
            public EnumBeast(bool addingNew) : base(addingNew) { }

            [DataField("Id", DbType.Int32, IsKeyField = true)]
            public int Id { get => field; set => SetFieldValue(ref field, value); }

            [DataField("PlainName", DbType.String)]
            public Rank Plain { get => field; set => SetFieldValue(ref field, value); }

            [DataField("NullName", DbType.String, true)]
            public Rank? Nully { get => field; set => SetFieldValue(ref field, value); }

            [DataField("NumText", DbType.String)]
            public Rank NumParsed { get => field; set => SetFieldValue(ref field, value); }

            [DataField("BigNum", DbType.Int64, true)]
            public Rank? FromBig { get => field; set => SetFieldValue(ref field, value); }
        }

        private List<EnumBeast> QueryEnums(string sql, bool fast)
        {
            using var cmd = _conn.CreateCommand();
            cmd.CommandText = sql;
            using var reader = new DataClassReader<EnumBeast>(cmd.ExecuteReader()) { UseFastBuilder = fast };
            return reader.ToList();
        }

        private void CreateEnumTable()
        {
            using var cmd = _conn.CreateCommand();
            cmd.CommandText = @"
                CREATE TABLE EnumSource (Id INTEGER PRIMARY KEY, PlainName TEXT, NullName TEXT, NumText TEXT, BigNum INTEGER);
                INSERT INTO EnumSource VALUES (1, 'High', 'low', '2', 999999999999);";
            cmd.ExecuteNonQuery();
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void EnumFromString_ParsesName(bool fast)
        {
            CreateEnumTable();
            var e = QueryEnums("SELECT Id, PlainName FROM EnumSource", fast)[0];
            Assert.Equal(Rank.High, e.Plain);
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void EnumFromString_NameIsCaseInsensitive_AndNullableWorks(bool fast)
        {
            CreateEnumTable();
            var e = QueryEnums("SELECT Id, NullName FROM EnumSource", fast)[0];
            Assert.Equal(Rank.Low, e.Nully);
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void EnumFromString_ParsesNumericString(bool fast)
        {
            CreateEnumTable();
            var e = QueryEnums("SELECT Id, NumText FROM EnumSource", fast)[0];
            Assert.Equal(Rank.High, e.NumParsed);
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void EnumFromString_InvalidName_ThrowsPropertyReadException(bool fast)
        {
            CreateEnumTable();
            using (var upd = _conn.CreateCommand())
            {
                upd.CommandText = "UPDATE EnumSource SET PlainName = 'Bogus'";
                upd.ExecuteNonQuery();
            }

            var ex = Assert.Throws<PropertyReadException>(() => QueryEnums("SELECT Id, PlainName FROM EnumSource", fast));
            Assert.Equal(nameof(EnumBeast.Plain), ex.Property.Name);
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void EnumFromIntegral_Overflow_Throws_NotSilentlyWraps(bool fast)
        {
            CreateEnumTable();
            var ex = Assert.Throws<PropertyReadException>(() => QueryEnums("SELECT Id, BigNum FROM EnumSource", fast));
            Assert.Equal(nameof(EnumBeast.FromBig), ex.Property.Name);
        }

        // ---- DateOnly/TimeOnly from NATIVE DateTime/TimeSpan columns (mock DataTable;
        //      SQLite cannot produce these column types) ----

        [DataItem("Schedule")]
        public class Slot : DataClass
        {
            public Slot() : base(false) { }
            public Slot(bool addingNew) : base(addingNew) { }

            [DataField("Id", DbType.Int32, IsKeyField = true)]
            public int Id { get => field; set => SetFieldValue(ref field, value); }

            [DataField("Day", DbType.Date)]
            public DateOnly Day { get => field; set => SetFieldValue(ref field, value); }       // DateTime column -> DateOnly

            [DataField("Slot1", DbType.Time, true)]
            public TimeOnly? Slot1 { get => field; set => SetFieldValue(ref field, value); }    // TimeSpan column -> TimeOnly?

            [DataField("Slot2", DbType.Time, true)]
            public TimeOnly? Slot2 { get => field; set => SetFieldValue(ref field, value); }    // DateTime column -> TimeOnly?
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void DateOnlyTimeOnly_FromNativeColumns(bool fast)
        {
            var table = new DataTable("Schedule");
            table.Columns.Add("Id", typeof(int));
            table.Columns.Add("Day", typeof(DateTime));
            table.Columns.Add("Slot1", typeof(TimeSpan));
            table.Columns.Add("Slot2", typeof(DateTime));
            table.Rows.Add(1, new DateTime(2024, 8, 9, 23, 59, 0), new TimeSpan(9, 30, 0), new DateTime(2000, 1, 1, 16, 15, 0));

            var conn = new Zonkey.Mocks.MockDbConnection();
            conn.Open();
            conn.SetupCommandFunc = cmd => cmd.DoExecuteReader = _ => table;

            using var command = conn.CreateCommand();
            using var reader = new DataClassReader<Slot>(command.ExecuteReader()) { UseFastBuilder = fast };
            var slot = reader.Read();

            Assert.Equal(new DateOnly(2024, 8, 9), slot.Day);
            Assert.Equal(new TimeOnly(9, 30), slot.Slot1);
            Assert.Equal(new TimeOnly(16, 15), slot.Slot2);
        }

        // ---- volume: alternating null patterns exercise every branch repeatedly ----

        [Fact]
        public void BulkAlternatingNulls_FastMatchesSlow()
        {
            using (var ins = _conn.CreateCommand())
            {
                ins.CommandText = "INSERT INTO Critters SELECT Id + (SELECT MAX(Id) FROM Critters), NULL, NULL, 1, 1, 1.0, 'B', 1, '1', '1', 'False', 1.0, 1, 0, '2022-01-01', NULL, NULL FROM Critters";
                for (int i = 0; i < 10; i++) ins.ExecuteNonQuery(); // 1 -> 1024 rows
            }

            var fast = Query("SELECT * FROM Critters ORDER BY Id", true);
            var slow = Query("SELECT * FROM Critters ORDER BY Id", false);

            Assert.Equal(slow.Count, fast.Count);
            Assert.True(fast.Count > 1000);
            for (int i = 0; i < fast.Count; i++)
            {
                Assert.Equal(slow[i].Id, fast[i].Id);
                Assert.Equal(slow[i].Name, fast[i].Name);
                Assert.Equal(slow[i].Score, fast[i].Score);
                Assert.Equal(slow[i].HatchDate, fast[i].HatchDate);
            }
        }
    }
}
#endif
