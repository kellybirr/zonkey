using Xunit;
using Zonkey.Scaffold.Diagnostics;
using Zonkey.Scaffold.Mapping;
using Zonkey.Scaffold.Schema;

public class SqlServerTypeMapperTests
{
    private readonly SqlServerTypeMapper _mapper = new();
    private readonly List<ScaffoldWarning> _warnings = [];
    private static readonly TableInfo Table = new() { Name = "t" };

    private ColumnMapping Map(string native, int? precision = null, int? scale = null)
        => _mapper.Map(Table,
            new ColumnInfo { Name = "c", NativeType = native, Precision = precision, Scale = scale },
            nullableRefs: true, _warnings);

    [Theory]
    [InlineData("int", "Int32", "int")]
    [InlineData("bigint", "Int64", "long")]
    [InlineData("bit", "Boolean", "bool")]
    [InlineData("uniqueidentifier", "Guid", "Guid")]
    [InlineData("nvarchar", "String", "string")]
    [InlineData("varchar", "AnsiString", "string")]
    [InlineData("nchar", "StringFixedLength", "string")]
    [InlineData("char", "AnsiStringFixedLength", "string")]
    [InlineData("datetime2", "DateTime2", "DateTime")]
    [InlineData("datetime", "DateTime", "DateTime")]
    [InlineData("date", "Date", "DateTime")]
    [InlineData("time", "Time", "TimeSpan")]
    [InlineData("varbinary", "Binary", "byte[]")]
    [InlineData("rowversion", "Binary", "byte[]")]
    [InlineData("money", "Currency", "decimal")]
    [InlineData("xml", "Xml", "string")]
    public void Maps_core_types(string native, string dbType, string clr)
    {
        var m = Map(native);
        Assert.Equal(dbType, m.DbType);
        Assert.Equal(clr, m.ClrType);
    }

    [Fact]
    public void Varchar_maps_to_AnsiString_not_String()
    {
        // Getting this wrong makes every parameter widen to nvarchar, which silently defeats
        // index seeks on varchar columns — a performance bug with no visible symptom.
        Assert.Equal("AnsiString", Map("varchar").DbType);
        Assert.Equal("String", Map("nvarchar").DbType);
    }

    [Fact]
    public void Decimal_narrows_consistently_with_other_providers()
    {
        Assert.Equal("int", Map("decimal", precision: 9, scale: 0).ClrType);
        Assert.Equal("long", Map("decimal", precision: 18, scale: 0).ClrType);
        Assert.Equal("decimal", Map("decimal", precision: 8, scale: 2).ClrType);
    }

    // ---- tinyint is unsigned in SQL Server, unlike every other supported provider -------------
    [Fact]
    public void Tinyint_maps_to_byte_not_sbyte()
    {
        // SQL Server's tinyint range is 0-255 (unsigned), unlike every other provider's tinyint /
        // TINYINT, which is signed. sbyte would silently misread any value above 127.
        var m = Map("tinyint");
        Assert.Equal("Byte", m.DbType);
        Assert.Equal("byte", m.ClrType);
        Assert.Empty(_warnings);
    }

    [Fact]
    public void Timestamp_native_type_maps_the_same_as_rowversion()
    {
        // sys.types actually reports "timestamp" for a ROWVERSION column, never "rowversion" (see
        // SqlServerSchemaReaderTests.Rowversion_column_reports_timestamp_as_its_native_type) --
        // both spellings must map identically since the reader only ever produces one of them.
        var byRowversion = Map("rowversion");
        var byTimestamp = Map("timestamp");
        Assert.Equal(byRowversion.DbType, byTimestamp.DbType);
        Assert.Equal(byRowversion.ClrType, byTimestamp.ClrType);
    }

    [Fact]
    public void Datetimeoffset_maps_to_DateTimeOffset()
    {
        var m = Map("datetimeoffset");
        Assert.Equal("DateTimeOffset", m.DbType);
        Assert.Equal("DateTimeOffset", m.ClrType);
    }

    [Fact]
    public void Nullable_value_type_gets_decorated_with_question_mark()
    {
        var m = _mapper.Map(Table,
            new ColumnInfo { Name = "c", NativeType = "int", IsNullable = true },
            nullableRefs: true, _warnings);
        Assert.Equal("int?", m.ClrType);
    }

    [Fact]
    public void Unrecognized_type_falls_back_to_string_and_warns()
    {
        var m = Map("geography");
        Assert.Equal("String", m.DbType);
        Assert.Equal("string", m.ClrType);
        Assert.Contains(_warnings, w => w.Code == WarningCode.UnmappableType);
    }
}
