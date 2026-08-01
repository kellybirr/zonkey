using Xunit;
using Zonkey.Scaffold.Diagnostics;
using Zonkey.Scaffold.Mapping;
using Zonkey.Scaffold.Schema;

public class SqliteTypeMapperTests
{
    private readonly SqliteTypeMapper _mapper = new();
    private readonly List<ScaffoldWarning> _warnings = [];
    private static readonly TableInfo Table = new() { Name = "T" };

    private ColumnMapping Map(string nativeType, bool nullable = false, bool nullableRefs = true,
        int? precision = null, int? scale = null)
        => _mapper.Map(Table,
            new ColumnInfo { Name = "c", NativeType = nativeType, IsNullable = nullable,
                             Precision = precision, Scale = scale },
            nullableRefs, _warnings);

    [Theory]
    [InlineData("INTEGER", "Int64", "long")]
    [InlineData("REAL", "Double", "double")]
    [InlineData("TEXT", "String", "string")]
    [InlineData("BLOB", "Binary", "byte[]")]
    public void Maps_the_four_storage_classes(string native, string dbType, string clr)
    {
        var m = Map(native);
        Assert.Equal(dbType, m.DbType);
        Assert.Equal(clr, m.ClrType);
    }

    [Theory]
    [InlineData("INT", "Int32", "int")]
    [InlineData("BIGINT", "Int64", "long")]
    [InlineData("BOOLEAN", "Boolean", "bool")]
    [InlineData("DATETIME", "DateTime", "DateTime")]
    [InlineData("VARCHAR", "String", "string")]
    [InlineData("NVARCHAR", "String", "string")]
    [InlineData("CHAR", "StringFixedLength", "string")]
    [InlineData("GUID", "Guid", "Guid")]
    [InlineData("UNIQUEIDENTIFIER", "Guid", "Guid")]
    public void Maps_common_declared_affinities(string native, string dbType, string clr)
    {
        var m = Map(native);
        Assert.Equal(dbType, m.DbType);
        Assert.Equal(clr, m.ClrType);
    }

    [Fact]
    public void Decimal_with_scale_stays_decimal()
    {
        var m = Map("DECIMAL", precision: 18, scale: 2);
        Assert.Equal("Decimal", m.DbType);
        Assert.Equal("decimal", m.ClrType);
    }

    [Fact]
    public void Decimal_with_zero_scale_and_large_precision_narrows_to_long()
        => Assert.Equal("long", Map("DECIMAL", precision: 18, scale: 0).ClrType);

    [Fact]
    public void Decimal_with_zero_scale_and_small_precision_narrows_to_int()
        => Assert.Equal("int", Map("DECIMAL", precision: 9, scale: 0).ClrType);

    [Fact]
    public void Nullable_value_type_gets_question_mark()
        => Assert.Equal("int?", Map("INT", nullable: true).ClrType);

    [Fact]
    public void Nullable_reference_type_gets_question_mark_when_nrt_enabled()
        => Assert.Equal("string?", Map("TEXT", nullable: true, nullableRefs: true).ClrType);

    [Fact]
    public void Nullable_reference_type_stays_bare_when_nrt_disabled()
        => Assert.Equal("string", Map("TEXT", nullable: true, nullableRefs: false).ClrType);

    [Fact]
    public void Non_nullable_reference_type_is_never_marked()
        => Assert.Equal("string", Map("TEXT", nullable: false, nullableRefs: true).ClrType);

    [Fact]
    public void Every_mapping_carries_a_reason()
        => Assert.False(string.IsNullOrWhiteSpace(Map("INTEGER").Reason));

    [Fact]
    public void Unknown_type_warns_and_falls_back_to_string()
    {
        var m = Map("GEOGRAPHY");
        Assert.Contains(_warnings, w => w.Code == WarningCode.UnmappableType);
        Assert.Equal("String", m.DbType);
    }

    /// <summary>
    /// The warning has to name the setting that actually changes the outcome. It used to say
    /// "set overrides.tables to correct it" while the only override that could correct a DbType
    /// was read by nothing — remediation advice for the tool's own warning that was a silent
    /// no-op, which is worse than no advice at all.
    /// </summary>
    [Fact]
    public void Unknown_type_warning_names_the_override_key_that_works()
    {
        Map("GEOGRAPHY");
        string message = _warnings.Single(w => w.Code == WarningCode.UnmappableType).Message;

        Assert.Contains("columns", message);
        Assert.Contains("dbType", message);
    }
}
