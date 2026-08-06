using Zonkey.Scaffold.Diagnostics;
using Zonkey.Scaffold.Schema;

namespace Zonkey.Scaffold.Mapping;

public sealed class SqliteTypeMapper : ITypeMapper
{
    public ColumnMapping Map(TableInfo table, ColumnInfo column, bool nullableRefs,
        ICollection<string> warnings)
    {
        string native = column.NativeType.ToUpperInvariant();

        (string dbType, string clr, bool isReference, string reason) = native switch
        {
            "INTEGER" => ("Int64", "long", false,
                "SQLite INTEGER is a 64-bit storage class; Int64 avoids silent truncation."),
            "INT" or "MEDIUMINT" => ("Int32", "int", false, "Declared INT maps to Int32."),
            "BIGINT" or "UNSIGNED BIG INT" => ("Int64", "long", false, "Declared BIGINT maps to Int64."),
            "SMALLINT" => ("Int16", "short", false, "Declared SMALLINT maps to Int16."),
            "TINYINT" => ("Byte", "byte", false, "Declared TINYINT maps to Byte."),
            "REAL" or "DOUBLE" or "FLOAT" => ("Double", "double", false,
                "SQLite REAL is an 8-byte IEEE float; Double is the exact match."),
            "BOOLEAN" or "BOOL" => ("Boolean", "bool", false,
                "Declared BOOLEAN maps to Boolean; SQLite stores it as 0/1."),
            "DATE" => ("Date", "DateTime", false, "Declared DATE maps to DbType.Date."),
            "DATETIME" or "TIMESTAMP" => ("DateTime", "DateTime", false,
                "Declared DATETIME maps to DbType.DateTime."),
            "GUID" or "UNIQUEIDENTIFIER" => ("Guid", "Guid", false,
                "Declared GUID maps to DbType.Guid."),
            "CHAR" or "NCHAR" => ("StringFixedLength", "string", true,
                "Fixed-width character type maps to DbType.StringFixedLength."),
            "TEXT" or "VARCHAR" or "NVARCHAR" or "CLOB" or "VARYING CHARACTER"
                => ("String", "string", true, "Character type maps to DbType.String."),
            "BLOB" or "BINARY" or "VARBINARY" => ("Binary", "byte[]", true,
                "SQLite BLOB maps to DbType.Binary."),
            "NUMERIC" or "DECIMAL" or "MONEY" => MapDecimal(column),
            _ => Unmappable(table, column, warnings)
        };

        return new ColumnMapping
        {
            DbType = dbType,
            ClrType = TypeMapperSupport.Decorate(clr, isReference, column.IsNullable, nullableRefs),
            Reason = reason
        };
    }

    private static (string, string, bool, string) MapDecimal(ColumnInfo column)
    {
        // Scale 0 means the column holds whole numbers; an integral CLR type is both more
        // accurate and cheaper than decimal. Precision decides how wide it needs to be.
        if (column.Scale == 0 && column.Precision is int precision)
        {
            return precision >= 10
                ? ("Int64", "long", false,
                   $"NUMERIC({precision},0) holds whole numbers wider than Int32; mapped to Int64.")
                : ("Int32", "int", false,
                   $"NUMERIC({precision},0) holds whole numbers within Int32 range.");
        }

        return ("Decimal", "decimal", false, "NUMERIC with a fractional scale maps to Decimal.");
    }

    private static (string, string, bool, string) Unmappable(
        TableInfo table, ColumnInfo column, ICollection<string> warnings)
    {
        warnings.Add(            $"Column '{table.Name}.{column.Name}' has unrecognized type '{column.NativeType}'. " +
            "Mapped to string / DbType.String — change it in the generated file if that is wrong.");

        return ("String", "string", true,
            $"Unrecognized declared type '{column.NativeType}'; defaulted to DbType.String.");
    }
}
