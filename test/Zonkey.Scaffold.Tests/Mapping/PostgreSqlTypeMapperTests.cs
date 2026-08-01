using Xunit;
using Zonkey.Scaffold.Diagnostics;
using Zonkey.Scaffold.Mapping;
using Zonkey.Scaffold.Schema;

public class PostgreSqlTypeMapperTests
{
    private readonly PostgreSqlTypeMapper _mapper = new();
    private readonly List<ScaffoldWarning> _warnings = [];
    private static readonly TableInfo Table = new() { Name = "t" };

    private ColumnMapping Map(string native, bool nullable = false,
        int? precision = null, int? scale = null, int? length = null)
        => _mapper.Map(Table,
            new ColumnInfo { Name = "c", NativeType = native, IsNullable = nullable,
                             Precision = precision, Scale = scale, MaxLength = length },
            nullableRefs: true, _warnings);

    // ---- the rule that must never regress -----------------------------------

    [Fact]
    public void Timestamptz_maps_to_DateTime_with_Utc_kind()
    {
        var m = Map("timestamp with time zone");
        Assert.Equal("DateTime", m.DbType);
        Assert.Equal("Utc", m.DateTimeKind);
    }

    [Fact]
    public void Plain_timestamp_maps_to_DateTime2_with_no_kind()
    {
        var m = Map("timestamp without time zone");
        Assert.Equal("DateTime2", m.DbType);
        Assert.Null(m.DateTimeKind);
    }

    [Fact]
    public void Timestamp_reason_explains_the_npgsql_asymmetry()
        => Assert.Contains("timestamptz", Map("timestamp with time zone").Reason);

    // ---- everything else ----------------------------------------------------

    [Theory]
    [InlineData("integer", "Int32", "int")]
    [InlineData("bigint", "Int64", "long")]
    [InlineData("smallint", "Int16", "short")]
    [InlineData("boolean", "Boolean", "bool")]
    [InlineData("uuid", "Guid", "Guid")]
    [InlineData("date", "Date", "DateTime")]
    [InlineData("text", "String", "string")]
    [InlineData("character varying", "String", "string")]
    [InlineData("character", "StringFixedLength", "string")]
    [InlineData("bytea", "Binary", "byte[]")]
    [InlineData("real", "Single", "float")]
    [InlineData("double precision", "Double", "double")]
    [InlineData("time without time zone", "Time", "TimeSpan")]
    public void Maps_core_types(string native, string dbType, string clr)
    {
        var m = Map(native);
        Assert.Equal(dbType, m.DbType);
        Assert.Equal(clr, m.ClrType);
    }

    [Fact]
    public void Numeric_with_scale_is_decimal()
        => Assert.Equal("decimal", Map("numeric", precision: 8, scale: 2).ClrType);

    [Fact]
    public void Numeric_scale_zero_small_precision_narrows_to_int()
        => Assert.Equal("int", Map("numeric", precision: 9, scale: 0).ClrType);

    [Fact]
    public void Numeric_scale_zero_large_precision_narrows_to_long()
        => Assert.Equal("long", Map("numeric", precision: 18, scale: 0).ClrType);

    [Fact]
    public void Nullable_reference_gets_a_question_mark()
        => Assert.Equal("string?", Map("text", nullable: true).ClrType);

    [Fact]
    public void Nullable_value_gets_a_question_mark()
        => Assert.Equal("int?", Map("integer", nullable: true).ClrType);

    [Fact]
    public void Array_types_warn_rather_than_guess()
    {
        Map("ARRAY");
        Assert.Contains(_warnings, w => w.Code == WarningCode.UnmappableType);
    }

    [Fact]
    public void Unknown_type_warns_and_names_the_type()
    {
        Map("geography");
        Assert.Contains(_warnings, w => w.Message.Contains("geography"));
    }
}
