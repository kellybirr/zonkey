using Xunit;
using Zonkey.Scaffold.Diagnostics;
using Zonkey.Scaffold.Mapping;
using Zonkey.Scaffold.Schema;

public class MySqlTypeMapperTests
{
    private readonly MySqlTypeMapper _mapper = new();
    private readonly List<ScaffoldWarning> _warnings = [];
    private static readonly TableInfo Table = new() { Name = "t" };

    private ColumnMapping Map(string native, int? precision = null, int? scale = null,
        bool isUnsigned = false)
        => _mapper.Map(Table,
            new ColumnInfo { Name = "c", NativeType = native, Precision = precision, Scale = scale,
                             IsUnsigned = isUnsigned },
            nullableRefs: true, _warnings);

    [Theory]
    [InlineData("int", "Int32", "int")]
    [InlineData("bigint", "Int64", "long")]
    [InlineData("smallint", "Int16", "short")]
    [InlineData("varchar", "String", "string")]
    [InlineData("text", "String", "string")]
    [InlineData("char", "StringFixedLength", "string")]
    [InlineData("blob", "Binary", "byte[]")]
    [InlineData("datetime", "DateTime2", "DateTime")]
    [InlineData("date", "Date", "DateTime")]
    [InlineData("time", "Time", "TimeSpan")]
    [InlineData("double", "Double", "double")]
    [InlineData("float", "Single", "float")]
    public void Maps_core_types(string native, string dbType, string clr)
    {
        var m = Map(native);
        Assert.Equal(dbType, m.DbType);
        Assert.Equal(clr, m.ClrType);
    }

    [Fact]
    public void Tinyint_width_one_maps_to_bool_and_warns()
    {
        var m = Map("tinyint", precision: 1);
        Assert.Equal("bool", m.ClrType);
        Assert.Contains(_warnings, w => w.Code == WarningCode.UnmappableType);
    }

    [Fact]
    public void Wider_tinyint_stays_a_number_and_does_not_warn()
    {
        var m = Map("tinyint", precision: 4);
        Assert.Equal("sbyte", m.ClrType);
        Assert.Empty(_warnings);
    }

    [Fact]
    public void Decimal_scale_zero_narrows_like_the_other_providers()
    {
        Assert.Equal("int", Map("decimal", precision: 9, scale: 0).ClrType);
        Assert.Equal("long", Map("decimal", precision: 18, scale: 0).ClrType);
        Assert.Equal("decimal", Map("decimal", precision: 8, scale: 2).ClrType);
    }

    // ---- unsigned integer types --------------------------------------------
    // MySQL's data_type never carries sign (INT UNSIGNED reports data_type "int", same as INT), so
    // these all exercise ColumnInfo.IsUnsigned, which the reader derives separately from
    // column_type. Each asserts both DbType and ClrType, and picks a value in the test name /
    // comment that is only representable in the unsigned CLR type, not its signed counterpart --
    // that is the actual bug being guarded against, not merely "does it compile".

    [Fact]
    public void Unsigned_tinyint_maps_to_byte_not_sbyte_and_does_not_warn()
    {
        // byte covers 0-255; sbyte tops out at 127 and would silently mis-map e.g. 200.
        var m = Map("tinyint", isUnsigned: true);
        Assert.Equal("Byte", m.DbType);
        Assert.Equal("byte", m.ClrType);
        Assert.Empty(_warnings);
    }

    [Fact]
    public void Unsigned_tinyint_never_maps_to_bool_even_if_width_one_is_reported()
    {
        // Live MySQL never actually reports precision 1 for an unsigned tinyint (the display width
        // is unconditionally stripped for the unsigned form), but the mapper must not depend on
        // that absence -- IsUnsigned alone must be enough to rule out BoolFromTinyint.
        var m = Map("tinyint", precision: 1, isUnsigned: true);
        Assert.Equal("byte", m.ClrType);
        Assert.Empty(_warnings);
    }

    [Fact]
    public void Unsigned_smallint_maps_to_ushort()
    {
        // ushort covers 0-65,535; short tops out at 32,767.
        var m = Map("smallint", isUnsigned: true);
        Assert.Equal("UInt16", m.DbType);
        Assert.Equal("ushort", m.ClrType);
    }

    [Fact]
    public void Unsigned_int_maps_to_uint()
    {
        // uint covers up to ~4.29e9; int tops out at ~2.15e9 -- half the unsigned range overflows.
        var m = Map("int", isUnsigned: true);
        Assert.Equal("UInt32", m.DbType);
        Assert.Equal("uint", m.ClrType);
    }

    [Fact]
    public void Unsigned_bigint_maps_to_ulong()
    {
        // ulong covers up to ~1.8e19; long tops out at ~9.2e18 -- half the unsigned range overflows.
        var m = Map("bigint", isUnsigned: true);
        Assert.Equal("UInt64", m.DbType);
        Assert.Equal("ulong", m.ClrType);
    }

    [Fact]
    public void Unsigned_mediumint_still_fits_Int32_so_stays_signed()
    {
        // Unlike int/bigint/smallint, mediumint unsigned's ceiling (16,777,215) fits comfortably
        // inside Int32.MaxValue, so there is nothing to widen and no unsigned arm is needed.
        var m = Map("mediumint", isUnsigned: true);
        Assert.Equal("Int32", m.DbType);
        Assert.Equal("int", m.ClrType);
    }

    [Fact]
    public void Signed_integers_are_unaffected_by_the_unsigned_arms()
    {
        Assert.Equal("short", Map("smallint", isUnsigned: false).ClrType);
        Assert.Equal("int", Map("int", isUnsigned: false).ClrType);
        Assert.Equal("long", Map("bigint", isUnsigned: false).ClrType);
    }
}
