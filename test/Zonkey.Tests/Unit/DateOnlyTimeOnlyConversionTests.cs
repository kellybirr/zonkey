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
    /// Symmetric DateOnly/TimeOnly conversion coverage. Providers may surface date/time
    /// columns as DateOnly/TimeOnly (Npgsql 10+), DateTime/TimeSpan (SqlClient,
    /// MySqlConnector, Npgsql 9), or strings (Microsoft.Data.Sqlite) -- every source shape
    /// must land in every reasonable destination property type on BOTH materialization
    /// paths. Date/time-ish sources destined for string properties use round-trip ISO
    /// formats ("O" for DateTime/DateOnly/TimeOnly, "c" for TimeSpan), never the current
    /// culture's short patterns.
    /// </summary>
    public class DateOnlyTimeOnlyConversionTests
    {
        private static T ReadOne<T>(Type columnType, object cell, bool fast) where T : DataClass
        {
            var table = new DataTable("T");
            table.Columns.Add("V", columnType);
            table.Rows.Add(cell);

            var conn = new MockDbConnection();
            conn.Open();
            conn.SetupCommandFunc = cmd => cmd.DoExecuteReader = _ => table;

            using DbCommand command = conn.CreateCommand();
            command.CommandText = "SELECT V FROM T";
            using var reader = new DataClassReader<T>(command.ExecuteReader()) { UseFastBuilder = fast };
            return reader.Read();
        }

        // ---- destinations that exist on all target frameworks ----

        [DataItem("T")]
        public class StringDest : DataClass
        {
            public StringDest() : base(false) { }
            [DataField("V", DbType.String, true)]
            public string V { get => field; set => SetFieldValue(ref field, value); }
        }

        [DataItem("T")]
        public class DateTimeDest : DataClass
        {
            public DateTimeDest() : base(false) { }
            [DataField("V", DbType.DateTime)]
            public DateTime V { get => field; set => SetFieldValue(ref field, value); }
        }

        [DataItem("T")]
        public class DateTimeNDest : DataClass
        {
            public DateTimeNDest() : base(false) { }
            [DataField("V", DbType.DateTime, true)]
            public DateTime? V { get => field; set => SetFieldValue(ref field, value); }
        }

        [DataItem("T")]
        public class UtcDateTimeDest : DataClass
        {
            public UtcDateTimeDest() : base(false) { }
            [DataField("V", DbType.Date, DateTimeKind = DateTimeKind.Utc)]
            public DateTime V { get => field; set => SetFieldValue(ref field, value); }
        }

        [DataItem("T")]
        public class TimeSpanDest : DataClass
        {
            public TimeSpanDest() : base(false) { }
            [DataField("V", DbType.Time)]
            public TimeSpan V { get => field; set => SetFieldValue(ref field, value); }
        }

        [DataItem("T")]
        public class TimeSpanNDest : DataClass
        {
            public TimeSpanNDest() : base(false) { }
            [DataField("V", DbType.Time, true)]
            public TimeSpan? V { get => field; set => SetFieldValue(ref field, value); }
        }

        // DateTime and TimeSpan sources into string destinations apply on net48 too

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void DateTimeSource_ToString_UsesIsoRoundTripFormat(bool fast)
        {
            var item = ReadOne<StringDest>(typeof(DateTime), new DateTime(2024, 5, 20, 14, 30, 15), fast);
            Assert.Equal("2024-05-20T14:30:15.0000000", item.V);
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void TimeSpanSource_ToString_UsesIsoConstantFormat(bool fast)
        {
            var item = ReadOne<StringDest>(typeof(TimeSpan), new TimeSpan(14, 30, 15), fast);
            Assert.Equal("14:30:15", item.V);
        }

        // string and DateTime sources into TimeSpan destinations: SQLite surfaces time
        // columns as text, and Access/ODBC-era drivers surface them as DateTime

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void StringSource_ToTimeSpan_ParsesInvariant(bool fast)
        {
            Assert.Equal(new TimeSpan(14, 30, 15), ReadOne<TimeSpanDest>(typeof(string), "14:30:15", fast).V);
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void StringSource_ToNullableTimeSpan_KeepsFractionalSeconds(bool fast)
        {
            Assert.Equal(new TimeSpan(0, 14, 30, 15, 500), ReadOne<TimeSpanNDest>(typeof(string), "14:30:15.5000000", fast).V);
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void StringSource_ToTimeSpan_ParsesStandardDayFormat(bool fast)
        {
            // the standard .NET "d.hh:mm:ss" form understood by TimeSpan.Parse/TryParse
            Assert.Equal(new TimeSpan(2, 8, 15, 42), ReadOne<TimeSpanDest>(typeof(string), "2.08:15:42", fast).V);
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void DateTimeSource_ToTimeSpan_UsesTimeOfDay(bool fast)
        {
            var item = ReadOne<TimeSpanDest>(typeof(DateTime), new DateTime(2023, 11, 5, 8, 15, 42), fast);
            Assert.Equal(new TimeSpan(8, 15, 42), item.V);
        }

#if !NETFRAMEWORK
        // ---- DateOnly/TimeOnly destinations (sanity: the already-working happy paths) ----

        [DataItem("T")]
        public class DateOnlyDest : DataClass
        {
            public DateOnlyDest() : base(false) { }
            [DataField("V", DbType.Date)]
            public DateOnly V { get => field; set => SetFieldValue(ref field, value); }
        }

        [DataItem("T")]
        public class DateOnlyNDest : DataClass
        {
            public DateOnlyNDest() : base(false) { }
            [DataField("V", DbType.Date, true)]
            public DateOnly? V { get => field; set => SetFieldValue(ref field, value); }
        }

        [DataItem("T")]
        public class TimeOnlyDest : DataClass
        {
            public TimeOnlyDest() : base(false) { }
            [DataField("V", DbType.Time)]
            public TimeOnly V { get => field; set => SetFieldValue(ref field, value); }
        }

        [DataItem("T")]
        public class TimeOnlyNDest : DataClass
        {
            public TimeOnlyNDest() : base(false) { }
            [DataField("V", DbType.Time, true)]
            public TimeOnly? V { get => field; set => SetFieldValue(ref field, value); }
        }

        private static readonly DateOnly TestDate = new DateOnly(2024, 5, 20);
        private static readonly TimeOnly TestTime = new TimeOnly(14, 30, 15);

        // ---- DateOnly source (what Npgsql 10 returns for 'date' columns) ----

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void DateOnlySource_ToDateOnly_AssignsDirectly(bool fast)
        {
            Assert.Equal(TestDate, ReadOne<DateOnlyDest>(typeof(DateOnly), TestDate, fast).V);
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void DateOnlySource_ToNullableDateOnly_AssignsDirectly(bool fast)
        {
            Assert.Equal(TestDate, ReadOne<DateOnlyNDest>(typeof(DateOnly), TestDate, fast).V);
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void DateOnlySource_ToDateTime_ConvertsAtMidnight(bool fast)
        {
            Assert.Equal(new DateTime(2024, 5, 20), ReadOne<DateTimeDest>(typeof(DateOnly), TestDate, fast).V);
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void DateOnlySource_ToNullableDateTime_ConvertsAtMidnight(bool fast)
        {
            Assert.Equal(new DateTime(2024, 5, 20), ReadOne<DateTimeNDest>(typeof(DateOnly), TestDate, fast).V);
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void DateOnlySource_ToDateTime_AppliesMappedDateTimeKind(bool fast)
        {
            var item = ReadOne<UtcDateTimeDest>(typeof(DateOnly), TestDate, fast);
            Assert.Equal(new DateTime(2024, 5, 20), item.V);
            Assert.Equal(DateTimeKind.Utc, item.V.Kind);
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void DateOnlySource_ToString_UsesIsoFormat(bool fast)
        {
            Assert.Equal("2024-05-20", ReadOne<StringDest>(typeof(DateOnly), TestDate, fast).V);
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void NullDateOnlySource_ToNullableDateTime_YieldsNull(bool fast)
        {
            Assert.Null(ReadOne<DateTimeNDest>(typeof(DateOnly), DBNull.Value, fast).V);
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void NullDateOnlySource_ToNullableDateOnly_YieldsNull(bool fast)
        {
            Assert.Null(ReadOne<DateOnlyNDest>(typeof(DateOnly), DBNull.Value, fast).V);
        }

        // ---- TimeOnly source (what Npgsql 10 returns for 'time' columns) ----

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void TimeOnlySource_ToTimeOnly_AssignsDirectly(bool fast)
        {
            Assert.Equal(TestTime, ReadOne<TimeOnlyDest>(typeof(TimeOnly), TestTime, fast).V);
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void TimeOnlySource_ToNullableTimeOnly_AssignsDirectly(bool fast)
        {
            Assert.Equal(TestTime, ReadOne<TimeOnlyNDest>(typeof(TimeOnly), TestTime, fast).V);
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void TimeOnlySource_ToTimeSpan_Converts(bool fast)
        {
            Assert.Equal(new TimeSpan(14, 30, 15), ReadOne<TimeSpanDest>(typeof(TimeOnly), TestTime, fast).V);
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void TimeOnlySource_ToNullableTimeSpan_Converts(bool fast)
        {
            Assert.Equal(new TimeSpan(14, 30, 15), ReadOne<TimeSpanNDest>(typeof(TimeOnly), TestTime, fast).V);
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void TimeOnlySource_ToDateTime_UsesMinDateBaseline(bool fast)
        {
            // baseline date 0001-01-01: mirrors TimeOnly.FromDateTime, which strips the
            // date part, so the round-trip is lossless
            Assert.Equal(new DateTime(1, 1, 1, 14, 30, 15), ReadOne<DateTimeDest>(typeof(TimeOnly), TestTime, fast).V);
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void TimeOnlySource_ToString_UsesIsoFormat(bool fast)
        {
            Assert.Equal("14:30:15.0000000", ReadOne<StringDest>(typeof(TimeOnly), TestTime, fast).V);
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void NullTimeOnlySource_ToNullableTimeSpan_YieldsNull(bool fast)
        {
            Assert.Null(ReadOne<TimeSpanNDest>(typeof(TimeOnly), DBNull.Value, fast).V);
        }

        // ---- parameter DbType inference ----

        [Fact]
        public void GetDbType_MapsDateOnlyAndTimeOnly()
        {
            Assert.Equal(DbType.Date, DataManager.GetDbType(typeof(DateOnly)));
            Assert.Equal(DbType.Date, DataManager.GetDbType(typeof(DateOnly?)));
            Assert.Equal(DbType.Time, DataManager.GetDbType(typeof(TimeOnly)));
            Assert.Equal(DbType.Time, DataManager.GetDbType(typeof(TimeOnly?)));
        }
#endif
    }
}
