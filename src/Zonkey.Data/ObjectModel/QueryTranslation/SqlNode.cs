using System.Collections.Generic;

namespace Zonkey.ObjectModel.QueryTranslation
{
    internal enum SqlBinaryOp
    {
        And, Or,
        Equal, NotEqual, LessThan, LessThanOrEqual, GreaterThan, GreaterThanOrEqual,
        Add, Subtract, Multiply, Divide, Modulo,
        BitAnd, BitOr
    }

    internal abstract class SqlNode
    { }

    internal sealed class SqlColumn : SqlNode
    {
        public IDataMapField Field;
        public DataMap Map;
        public bool IsBoolean;
    }

    // a boolean column used in predicate position: renders via SqlDialect.FormatUnaryBoolean
    internal sealed class SqlBoolPredicate : SqlNode
    {
        public SqlColumn Column;
    }

    // a boolean-typed non-column expression (COALESCE/CASE) used in predicate position;
    // rendered via the dialect's boolean formatting (base: (x = 1), PostgreSql: (x))
    internal sealed class SqlBoolExprPredicate : SqlNode
    {
        public SqlNode Operand;
    }

    internal sealed class SqlValue : SqlNode
    {
        public object Value;
    }

    internal sealed class SqlLiteral : SqlNode
    {
        public string Text;
    }

    internal sealed class SqlBinary : SqlNode
    {
        public SqlBinaryOp Op;
        public SqlNode Left;
        public SqlNode Right;
    }

    internal sealed class SqlNot : SqlNode
    {
        public SqlNode Operand;
    }

    internal sealed class SqlNegate : SqlNode
    {
        public SqlNode Operand;
    }

    internal sealed class SqlIsNull : SqlNode
    {
        public SqlNode Operand;
        public bool Not;
    }

    internal sealed class SqlFunction : SqlNode
    {
        public string Name;                 // logical name, rendered by SqlDialect.RenderFunction
        public IReadOnlyList<SqlNode> Args;
    }

    internal sealed class SqlLike : SqlNode
    {
        public SqlNode Operand;
        public SqlNode Pattern;             // final pattern (wildcards already applied/escaped)
        public bool IgnoreCase;
        public bool Escaped;                // true => emit ESCAPE clause for '\'
    }

    internal sealed class SqlRegexMatch : SqlNode
    {
        public SqlNode Operand;
        public SqlNode Pattern;
        public bool IgnoreCase;
    }

    internal sealed class SqlInValues : SqlNode
    {
        public SqlNode Operand;
        public IReadOnlyList<object> Values;  // non-null, non-empty; enum values pass through untouched (PG native enums)
    }

    // legacy SqlInInt/SqlInGuid contract: always inline literals, never parameterize
    internal sealed class SqlInValuesInline : SqlNode
    {
        public SqlNode Operand;
        public IReadOnlyList<object> Values;
    }

    internal sealed class SqlInSubquery : SqlNode
    {
        public SqlNode Operand;
        public string SelectFieldRaw;       // unformatted column name
        public DataMap TargetMap;
        public SqlNode Where;
    }
}
