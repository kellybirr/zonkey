using Zonkey.Scaffold.Diagnostics;
using Zonkey.Scaffold.Schema;

namespace Zonkey.Scaffold.Mapping;

/// <summary>
/// Maps MySQL/MariaDB <c>information_schema.columns.data_type</c> strings (e.g. "varchar",
/// "tinyint", "decimal") to DbType/CLR pairs.
/// </summary>
public sealed class MySqlTypeMapper : ITypeMapper
{
    public ColumnMapping Map(TableInfo table, ColumnInfo column, bool nullableRefs,
        ICollection<ScaffoldWarning> warnings)
    {
        string native = column.NativeType.ToLowerInvariant();

        (string dbType, string clr, bool isReference, string reason) = native switch
        {
            // information_schema.columns.data_type normalizes INT and INTEGER (they are exact
            // synonyms in MySQL DDL) to the single string "int" -- confirmed against a live 8.4
            // server -- so only that spelling needs an arm here. data_type carries no sign,
            // though (INT UNSIGNED reports data_type "int" too), so the unsigned branches below
            // key on ColumnInfo.IsUnsigned, which the reader derives separately from column_type.
            // An unsigned column's true range exceeds its signed CLR counterpart -- e.g. INT
            // UNSIGNED's max (~4.29e9) is roughly double Int32.MaxValue -- so getting this wrong
            // isn't cosmetic, it's silent overflow on real data above the signed half of the range.
            "int" when column.IsUnsigned => ("UInt32", "uint", false,
                "int unsigned holds values up to ~4.29e9, beyond Int32.MaxValue; mapped to UInt32 " +
                "to avoid overflowing on the upper half of the column's actual range."),
            "int" => ("Int32", "int", false, "int maps to Int32."),
            "bigint" when column.IsUnsigned => ("UInt64", "ulong", false,
                "bigint unsigned holds values up to ~1.8e19, beyond Int64.MaxValue; mapped to " +
                "UInt64 to avoid overflowing on the upper half of the column's actual range."),
            "bigint" => ("Int64", "long", false, "bigint maps to Int64."),
            // Unlike int/bigint, mediumint's unsigned ceiling (16,777,215) still fits comfortably
            // inside Int32.MaxValue, so there is no overflow to guard against and no unsigned arm
            // is needed here -- signed and unsigned mediumint both map to Int32.
            "mediumint" => ("Int32", "int", false, "mediumint fits within Int32."),
            "smallint" when column.IsUnsigned => ("UInt16", "ushort", false,
                "smallint unsigned holds values up to 65,535, beyond Int16.MaxValue; mapped to " +
                "UInt16 to avoid overflowing on the upper half of the column's actual range."),
            "smallint" => ("Int16", "short", false, "smallint maps to Int16."),

            // MySQL has no BOOLEAN storage type: BOOLEAN/BOOL is sugar for TINYINT(1), so a
            // genuine one-digit TINYINT column is indistinguishable from a flag. See BoolFromTinyint.
            // The signed, width-1 guard below is deliberately narrow: unsigned tinyint never
            // reports a display width at all (see UnsignedTinyint), so it can never satisfy this
            // arm and always falls through to the unsigned arm beneath it.
            "tinyint" when column.Precision == 1 && !column.IsUnsigned
                => BoolFromTinyint(table, column, warnings),
            "tinyint" when column.IsUnsigned => UnsignedTinyint(),
            "tinyint" => ("SByte", "sbyte", false, "tinyint with a width above 1 is a small integer."),

            "varchar" or "text" or "tinytext" or "mediumtext" or "longtext" or "enum" or "set"
                => ("String", "string", true, $"{native} maps to DbType.String."),
            "char" => ("StringFixedLength", "string", true,
                "char(n) is fixed width; DbType.StringFixedLength preserves padding semantics."),
            "blob" or "tinyblob" or "mediumblob" or "longblob" or "binary" or "varbinary"
                => ("Binary", "byte[]", true, $"{native} maps to DbType.Binary."),

            // MySQL's datetime carries no zone -- the same reasoning as PostgreSQL's plain
            // "timestamp without time zone" applies, so this is DbType.DateTime2, not DbType.DateTime.
            "datetime" => ("DateTime2", "DateTime", false,
                "datetime has no zone; DbType.DateTime2 is the un-zoned DateTime mapping."),
            // MySQL's TIMESTAMP *does* store an instant (converted to/from the session time zone on
            // read/write), unlike datetime, but Zonkey has no rowversion concept for MySQL's
            // ON UPDATE CURRENT_TIMESTAMP -- that is SQL Server-specific -- so this column is just
            // an ordinary DateTime, not IsRowVersion.
            "timestamp" => ("DateTime", "DateTime", false,
                "timestamp is an instant recorded by the server; mapped to DbType.DateTime."),
            "date" => ("Date", "DateTime", false, "date maps to DbType.Date."),
            "time" => ("Time", "TimeSpan", false, "time maps to DbType.Time."),
            "year" => ("Int32", "int", false, "year maps to Int32."),

            "float" => ("Single", "float", false, "float is a 4-byte IEEE float."),
            // DOUBLE PRECISION and (under default sql_mode) REAL are DDL synonyms for DOUBLE; data_type
            // reports plain "double" for all three -- confirmed live -- so, as with int/integer above,
            // only that one spelling is reachable. (With sql_mode=REAL_AS_FLOAT, REAL instead becomes
            // a synonym for FLOAT and reports data_type "float", already handled above.)
            "double" => ("Double", "double", false, "double is an 8-byte IEEE float."),
            // NUMERIC, DEC, and FIXED are all DDL synonyms for DECIMAL; data_type reports "decimal"
            // for all of them -- confirmed live -- so "numeric" is never actually emitted here either.
            "decimal" => MapNumeric(column),

            "json" => ("String", "string", true,
                "json has no dedicated DbType; mapped to DbType.String."),

            _ => Unsupported(table, column, warnings,
                $"Unrecognized MySQL type '{column.NativeType}'.")
        };

        return new ColumnMapping
        {
            DbType = dbType,
            ClrType = TypeMapperSupport.Decorate(clr, isReference, column.IsNullable, nullableRefs),
            Reason = reason
        };
    }

    private static (string, string, bool, string) BoolFromTinyint(
        TableInfo table, ColumnInfo column, ICollection<ScaffoldWarning> warnings)
    {
        // MySQL has no boolean type: BOOLEAN is an alias for TINYINT(1), so a genuine one-digit
        // integer is indistinguishable from a flag. Mapping to bool is right far more often than
        // not, but it is a guess, and a guess the caller must be told about.
        warnings.Add(ScaffoldWarning.For(
            WarningCode.UnmappableType,
            $"Column '{table.QualifiedName}.{column.Name}' is TINYINT(1), which MySQL uses for both " +
            "BOOLEAN and a one-digit integer. Mapped to bool; override it if that is wrong.",
            table: table.QualifiedName, column: column.Name));

        return ("Boolean", "bool", false, "TINYINT(1) is MySQL's BOOLEAN alias.");
    }

    /// <summary>
    /// Unsigned tinyint is deliberately never mapped to bool, and deliberately does not raise a
    /// warning either -- both decisions need explaining, because the obvious-looking alternatives
    /// are each wrong for a different reason.
    ///
    /// Why not bool: the width-1 convention that makes bool the right guess for signed TINYINT(1)
    /// does not exist for the unsigned form. MySQL never preserves a display width for UNSIGNED
    /// TINYINT (confirmed live: "TINYINT(1) UNSIGNED" and plain "TINYINT UNSIGNED" both report
    /// column_type "tinyint unsigned", with no parenthesized width at all), so there is no signal
    /// left to guess *from* -- guessing bool here would not be "less certain than the signed case",
    /// it would be picking an answer with literally nothing behind it. byte is also the type that
    /// actually matches the column's declared range (0-255); sbyte would additionally be wrong on
    /// range for any value above 127, which is the exact silent-overflow failure this change exists
    /// to close.
    ///
    /// Why not a warning: the warnings channel exists for a fork in the road the tool had to choose
    /// at -- signed TINYINT(1) truly could be either a flag or a number, and bool is a guess in one
    /// direction. Unsigned tinyint isn't a fork: byte is simply the correct numeric type for the
    /// storage, not a guess between two plausible readings. Warning on every unsigned tinyint column
    /// would fire for the common case (small unsigned counters/levels/percentages, which are not
    /// boolean) far more often than the rare case (an unsigned column someone genuinely intended as
    /// a flag), training users to ignore the warnings channel. That rare case is not left unexplained,
    /// though -- the reason string below states the ambiguity plainly, and Reason is surfaced
    /// verbatim by `inspect --json`, so anyone auditing schema output (human or agent) can still see
    /// it without every schema's ordinary unsigned counters tripping a warning.
    /// </summary>
    private static (string, string, bool, string) UnsignedTinyint()
        => ("Byte", "byte", false,
            "TINYINT UNSIGNED never preserves a display width in MySQL, unlike its signed " +
            "counterpart, so boolean intent can't be distinguished from an ordinary small unsigned " +
            "number here; mapped to byte (0-255), which is what the column actually stores. " +
            "Override the DbType/ClrType manually if this column is really a flag.");

    private static (string, string, bool, string) MapNumeric(ColumnInfo column)
    {
        // Scale 0 means the column holds whole numbers; an integral CLR type is both more
        // accurate and cheaper than decimal. Precision decides how wide it needs to be.
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
            $"Unrecognized MySQL type '{column.NativeType}'; defaulted to DbType.String.");
    }
}
