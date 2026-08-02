using Zonkey.Scaffold.Diagnostics;
using Zonkey.Scaffold.Schema;

namespace Zonkey.Scaffold.Mapping;

/// <summary>
/// Maps PostgreSQL <c>information_schema.columns.data_type</c> strings (e.g. "timestamp with
/// time zone", "character varying", "double precision") to DbType/CLR pairs.
/// </summary>
public sealed class PostgreSqlTypeMapper : ITypeMapper
{
    public ColumnMapping Map(TableInfo table, ColumnInfo column, bool nullableRefs,
        ICollection<string> warnings)
    {
        string native = column.NativeType.ToLowerInvariant();

        string? dateTimeKind = null;
        (string dbType, string clr, bool isReference, string reason) = native switch
        {
            // See docs/postgresql.md and AGENTS.md rule 8. Npgsql 6+ maps DbType.DateTime to
            // timestamptz and DbType.DateTime2 to plain timestamp -- the inverse of what the
            // DbType names suggest. Swapping either arm below compiles, generates text fine, and
            // silently shifts every stored value by the server's UTC offset.
            "timestamp with time zone" => Timestamptz(ref dateTimeKind),
            "timestamp without time zone" or "timestamp" => ("DateTime2", "DateTime", false,
                "Plain timestamp has no zone; DbType.DateTime2 is what Npgsql maps back to " +
                "'timestamp without time zone' (DbType.DateTime would target timestamptz instead)."),

            "integer" or "int" or "int4" => ("Int32", "int", false, "integer maps to Int32."),
            "bigint" or "int8" => ("Int64", "long", false, "bigint maps to Int64."),
            "smallint" or "int2" => ("Int16", "short", false, "smallint maps to Int16."),
            "boolean" or "bool" => ("Boolean", "bool", false, "boolean maps to Boolean."),
            "uuid" => ("Guid", "Guid", false, "uuid maps to DbType.Guid."),
            "date" => ("Date", "DateTime", false, "date maps to DbType.Date."),
            "time without time zone" or "time" => ("Time", "TimeSpan", false,
                "time maps to DbType.Time."),
            "real" or "float4" => ("Single", "float", false, "real is a 4-byte float."),
            "double precision" or "float8" => ("Double", "double", false,
                "double precision is an 8-byte float."),
            "money" => ("Currency", "decimal", false, "money maps to DbType.Currency."),
            "character" or "bpchar" => ("StringFixedLength", "string", true,
                "character(n) is fixed width; DbType.StringFixedLength preserves padding semantics."),
            "character varying" or "varchar" or "text" or "citext" or "name"
                => ("String", "string", true, $"{native} maps to DbType.String."),
            "bytea" => ("Binary", "byte[]", true, "bytea maps to DbType.Binary."),
            "xml" => ("Xml", "string", true, "xml maps to DbType.Xml."),
            "numeric" or "decimal" => MapNumeric(column),

            // information_schema.columns.data_type reports every array column as the literal
            // string "ARRAY" (element type is only recoverable from udt_name, which this reader
            // doesn't select) -- there's no single DbType for an array regardless of element type,
            // so this gets its own arm rather than falling into the generic unknown-type bucket.
            "array" => Unsupported(table, column, warnings,
                "Array columns have no single DbType. Use DbType.Object with a NativeType " +
                "override (see docs/postgresql.md#arrays-and-jsonb), or exclude the column."),

            _ => Unsupported(table, column, warnings,
                $"Unrecognized PostgreSQL type '{column.NativeType}'.")
        };

        return new ColumnMapping
        {
            DbType = dbType,
            ClrType = TypeMapperSupport.Decorate(clr, isReference, column.IsNullable, nullableRefs),
            Reason = reason,
            DateTimeKind = dateTimeKind
        };
    }

    private static (string, string, bool, string) Timestamptz(ref string? kind)
    {
        kind = "Utc";
        return ("DateTime", "DateTime", false,
            "timestamptz is an instant, not a wall-clock time; Npgsql maps DbType.DateTime " +
            "(not DateTime2) to timestamptz, so this is DbType.DateTime with DateTimeKind.Utc " +
            "-- values must be populated and read as UTC.");
    }

    private static (string, string, bool, string) MapNumeric(ColumnInfo column)
    {
        // Scale 0 means the column holds whole numbers; an integral CLR type is both more
        // accurate and cheaper than decimal. Precision decides how wide it needs to be.
        if (column.Scale == 0 && column.Precision is int precision)
        {
            return precision >= 10
                ? ("Int64", "long", false,
                   $"numeric({precision},0) holds whole numbers wider than Int32; mapped to Int64.")
                : ("Int32", "int", false,
                   $"numeric({precision},0) holds whole numbers within Int32 range.");
        }

        return ("Decimal", "decimal", false, "numeric with a fractional scale maps to Decimal.");
    }

    private static (string, string, bool, string) Unsupported(
        TableInfo table, ColumnInfo column, ICollection<string> warnings, string detail)
    {
        warnings.Add(
            $"Column '{table.QualifiedName}.{column.Name}' has unrecognized type " +
            $"'{column.NativeType}'. {detail} Mapped to string / DbType.String — " +
            "change it in the generated file if that is wrong.");

        return ("String", "string", true,
            $"Unrecognized PostgreSQL type '{column.NativeType}'; defaulted to DbType.String.");
    }
}
