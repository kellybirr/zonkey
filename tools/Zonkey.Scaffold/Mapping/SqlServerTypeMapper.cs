using Zonkey.Scaffold.Diagnostics;
using Zonkey.Scaffold.Schema;

namespace Zonkey.Scaffold.Mapping;

/// <summary>
/// Maps SQL Server <c>sys.types.name</c> strings (e.g. "nvarchar", "tinyint", "rowversion") to
/// DbType/CLR pairs. Every arm here was checked against a live SQL Server 2022 container's
/// sys.types (34 built-in, non-user-defined rows) to confirm the spelling is one the reader can
/// actually produce -- see SqlServerSchemaReader's ColumnSql comment for the one surprise that
/// turned up (ROWVERSION columns report type name "timestamp", never "rowversion").
/// </summary>
public sealed class SqlServerTypeMapper : ITypeMapper
{
    public ColumnMapping Map(TableInfo table, ColumnInfo column, bool nullableRefs,
        ICollection<ScaffoldWarning> warnings)
    {
        string native = column.NativeType.ToLowerInvariant();

        (string dbType, string clr, bool isReference, string reason) = native switch
        {
            "int" => ("Int32", "int", false, "int maps to Int32."),
            "bigint" => ("Int64", "long", false, "bigint maps to Int64."),
            "smallint" => ("Int16", "short", false, "smallint maps to Int16."),

            // Unlike every other supported provider, SQL Server's tinyint is UNSIGNED (0-255), not
            // signed (-128 to 127). Mapping it to sbyte would silently misread any stored value
            // above 127 -- the exact class of overflow bug MySqlTypeMapper's unsigned handling
            // exists to avoid, just triggered by the native type name alone here rather than a
            // separate unsigned flag, since SQL Server has no signed/unsigned axis at all.
            "tinyint" => ("Byte", "byte", false, "tinyint is unsigned (0-255) in SQL Server; " +
                "mapped to Byte, not SByte, to match its actual range."),

            "bit" => ("Boolean", "bool", false, "bit maps to Boolean."),
            "uniqueidentifier" => ("Guid", "Guid", false, "uniqueidentifier maps to DbType.Guid."),

            // varchar is single-byte; DbType.AnsiString avoids widening parameters to nvarchar,
            // which would prevent index seeks on varchar columns -- a performance bug with no
            // visible symptom until a query plan is inspected.
            "varchar" or "text" => ("AnsiString", "string", true,
                "varchar is single-byte; DbType.AnsiString avoids widening parameters to nvarchar, " +
                "which would prevent index seeks on varchar columns."),
            "nvarchar" or "ntext" => ("String", "string", true, $"{native} maps to DbType.String."),
            "char" => ("AnsiStringFixedLength", "string", true,
                "char is fixed-width single-byte."),
            "nchar" => ("StringFixedLength", "string", true, "nchar is fixed-width Unicode."),

            "binary" or "varbinary" or "image" => ("Binary", "byte[]", true,
                $"{native} maps to DbType.Binary."),

            // rowversion is an 8-byte binary concurrency token; IsRowVersion is set separately by
            // the reader (see SqlServerSchemaReader.ColumnSql) and drives Zonkey's optimistic
            // concurrency check. The "rowversion" spelling is never actually produced by the live
            // reader (sys.types reports "timestamp" for both), but is matched here too since this
            // mapper is also driven directly by ColumnInfo built by hand (tests, or overrides).
            "rowversion" or "timestamp" => ("Binary", "byte[]", true,
                "rowversion is an 8-byte binary concurrency token; IsRowVersion is set separately."),

            "datetime" => ("DateTime", "DateTime", false, "datetime maps to DbType.DateTime."),
            "smalldatetime" => ("DateTime", "DateTime", false,
                "smalldatetime maps to DbType.DateTime."),
            "datetime2" => ("DateTime2", "DateTime", false, "datetime2 maps to DbType.DateTime2."),
            "datetimeoffset" => ("DateTimeOffset", "DateTimeOffset", false,
                "datetimeoffset carries a zone."),
            "date" => ("Date", "DateTime", false, "date maps to DbType.Date."),
            "time" => ("Time", "TimeSpan", false, "time maps to DbType.Time."),

            "real" => ("Single", "float", false, "real is a 4-byte float."),
            "float" => ("Double", "double", false, "float(53) is an 8-byte float."),

            "money" or "smallmoney" => ("Currency", "decimal", false,
                "money maps to DbType.Currency."),
            "decimal" or "numeric" => MapNumeric(column),

            "xml" => ("Xml", "string", true, "xml maps to DbType.Xml."),

            _ => Unsupported(table, column, warnings,
                $"Unrecognized SQL Server type '{column.NativeType}'.")
        };

        return new ColumnMapping
        {
            DbType = dbType,
            ClrType = TypeMapperSupport.Decorate(clr, isReference, column.IsNullable, nullableRefs),
            Reason = reason
        };
    }

    private static (string, string, bool, string) MapNumeric(ColumnInfo column)
    {
        // Scale 0 means the column holds whole numbers; an integral CLR type is both more
        // accurate and cheaper than decimal. Precision decides how wide it needs to be. Matches
        // MySqlTypeMapper.MapNumeric / PostgreSqlTypeMapper.MapNumeric exactly.
        if (column.Scale == 0 && column.Precision is int precision)
        {
            return precision >= 10
                ? ("Int64", "long", false,
                   $"decimal({precision},0) holds whole numbers wider than Int32; mapped to Int64.")
                : ("Int32", "int", false,
                   $"decimal({precision},0) holds whole numbers within Int32 range.");
        }

        return ("Decimal", "decimal", false, "decimal with a fractional scale maps to Decimal.");
    }

    private static (string, string, bool, string) Unsupported(
        TableInfo table, ColumnInfo column, ICollection<ScaffoldWarning> warnings, string detail)
    {
        warnings.Add(ScaffoldWarning.For(
            WarningCode.UnmappableType,
            $"Column '{table.QualifiedName}.{column.Name}' has unrecognized type " +
            $"'{column.NativeType}'. {detail} Mapped to string / DbType.String; set " +
            $"overrides.tables.{table.Name}.columns.{column.Name}.dbType to change the DbType, " +
            "or declare the column with a type this provider recognizes.",
            table: table.QualifiedName, column: column.Name));

        return ("String", "string", true,
            $"Unrecognized SQL Server type '{column.NativeType}'; defaulted to DbType.String.");
    }
}
