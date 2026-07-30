using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using Zonkey.Dialects;

namespace Zonkey.ObjectModel.QueryTranslation
{
    internal sealed class SqlTextGenerator
    {
        internal const int InlineThreshold = 64;

        private readonly SqlDialect _dialect;
        private readonly StringBuilder _sb = new StringBuilder();
        private readonly ArrayList _parameters;

        public char ParameterPrefix { get; set; } = '$';
        public int ParameterIndexModifier { get; set; }
        public bool? UseQuotedIdentifier { get; set; }
        public bool QualifyColumns { get; set; }
        public bool ParameterizeLiterals { get; set; } = true;
        public bool NoLock { get; set; }

        public SqlTextGenerator(SqlDialect dialect, ArrayList parameters = null)
        {
            _dialect = dialect ?? throw new ArgumentNullException(nameof(dialect));
            _parameters = parameters ?? new ArrayList();
        }

        public SqlWhereClause Generate(SqlNode root)
        {
            Visit(root);

            int totalParameters = _parameters.Count + ParameterIndexModifier;
            if (totalParameters > _dialect.MaxParameters)
                throw new SqlExpressionException(
                    $"This command would use {totalParameters} parameters, exceeding the dialect's limit of " +
                    $"{_dialect.MaxParameters}. Split the list with SplitList() and run multiple fills.");

            return new SqlWhereClause { SqlText = _sb.ToString(), Parameters = _parameters.ToArray() };
        }

        private void Visit(SqlNode node)
        {
            switch (node)
            {
                case SqlColumn c:
                    _sb.Append(ColumnText(c));
                    break;
                case SqlBoolPredicate b:
                    _sb.Append(_dialect.FormatUnaryBoolean(ColumnText(b.Column)));
                    break;
                case SqlBoolExprPredicate p:
                    _sb.Append(_dialect.FormatUnaryBoolean(Render(p.Operand)));
                    break;
                case SqlValue v:
                    AppendValue(v.Value);
                    break;
                case SqlLiteral l:
                    _sb.Append(l.Text);
                    break;
                case SqlBinary bin:
                    _sb.Append('(');
                    Visit(bin.Left);
                    _sb.Append(OpText(bin.Op));
                    Visit(bin.Right);
                    _sb.Append(')');
                    break;
                case SqlNot n:
                    _sb.Append("(NOT ");
                    Visit(n.Operand);
                    _sb.Append(')');
                    break;
                case SqlNegate n:
                    _sb.Append("(-");
                    Visit(n.Operand);
                    _sb.Append(')');
                    break;
                case SqlIsNull inl:
                    _sb.Append('(');
                    Visit(inl.Operand);
                    _sb.Append(inl.Not ? " IS NOT NULL)" : " IS NULL)");
                    break;
                case SqlFunction f:
                    VisitFunction(f);
                    break;
                case SqlLike like:
                    _sb.Append(_dialect.RenderLike(Render(like.Operand), Render(like.Pattern), like.IgnoreCase, like.Escaped ? '\\' : (char?)null));
                    break;
                case SqlRegexMatch rx:
                    _sb.Append(_dialect.RenderRegexMatch(Render(rx.Operand), Render(rx.Pattern), rx.IgnoreCase));
                    break;
                case SqlInValues inv:
                    VisitInValues(inv);
                    break;
                case SqlInValuesInline invi:
                    VisitInValuesInline(invi);
                    break;
                case SqlInSubquery sub:
                    VisitInSubquery(sub);
                    break;
                default:
                    throw new SqlExpressionException($"Unknown SQL node type '{node?.GetType().Name}'");
            }
        }

        // renders a subtree to a string for dialect hooks, keeping parameter numbering shared
        private string Render(SqlNode node)
        {
            int mark = _sb.Length;
            Visit(node);
            string text = _sb.ToString(mark, _sb.Length - mark);
            _sb.Length = mark;
            return text;
        }

        private void VisitFunction(SqlFunction f)
        {
            var args = new string[f.Args.Count];
            for (int i = 0; i < args.Length; i++)
                args[i] = Render(f.Args[i]);

            _sb.Append(_dialect.RenderFunction(f.Name, args));
        }

        private void VisitInValues(SqlInValues n)
        {
            IReadOnlyList<object> values = n.Values;

            if (ParameterizeLiterals && values.Count > InlineThreshold
                && _dialect.SupportsInCollectionParameter(values[0].GetType())
                && TryBuildTypedArray(values, out Array typedArray))
            {
                VisitInValuesAsCollectionParameter(n, typedArray);
                return;
            }

            bool inline = !ParameterizeLiterals || (values.Count > InlineThreshold && AllSafeLiterals(values));
            if (!inline)
            {
                int projectedTotal = ParameterIndexModifier + _parameters.Count + values.Count;
                if (projectedTotal > _dialect.MaxParameters)
                    throw new SqlExpressionException(
                        $"IN list would bring the command to {projectedTotal} parameters of a type that cannot be inlined as literals; " +
                        $"the parameter limit is {_dialect.MaxParameters}. Split the list with SplitList() and run multiple fills.");
            }

            _sb.Append('(');
            Visit(n.Operand);
            _sb.Append(" IN (");
            for (int i = 0; i < values.Count; i++)
            {
                if (i > 0) _sb.Append(',');
                if (inline) _sb.Append(InlineLiteral(values[i]));
                else AppendValue(values[i]);
            }
            _sb.Append("))");
        }

        // single typed-array parameter (e.g. PostgreSql = ANY): one stable plan, no parameter-count limit
        private void VisitInValuesAsCollectionParameter(SqlInValues n, Array array)
        {
            string operandSql = Render(n.Operand);   // render operand FIRST so any operand parameters keep their order
            _parameters.Add(array);
            string placeholder = ParameterPrefix + (_parameters.Count + ParameterIndexModifier - 1).ToString(CultureInfo.InvariantCulture);
            _sb.Append(_dialect.RenderInCollectionParameter(operandSql, placeholder));
        }

        // builds a homogeneous typed array from values for the collection-parameter path; returns false
        // (without throwing) on a mixed-runtime-type list, letting the caller fall through to the
        // existing individual-parameter / literal-inline logic instead.
        private static bool TryBuildTypedArray(IReadOnlyList<object> values, out Array array)
        {
            Type elementType = values[0].GetType();
            var candidate = Array.CreateInstance(elementType, values.Count);
            for (int i = 0; i < values.Count; i++)
            {
                object value = values[i];
                if (value.GetType() != elementType)
                {
                    array = null;
                    return false;
                }

                candidate.SetValue(value, i);
            }

            array = candidate;
            return true;
        }

        private void VisitInValuesInline(SqlInValuesInline n)
        {
            _sb.Append('(');
            Visit(n.Operand);
            _sb.Append(" IN (");
            for (int i = 0; i < n.Values.Count; i++)
            {
                if (i > 0) _sb.Append(',');
                _sb.Append(InlineLiteral(n.Values[i]));
            }
            _sb.Append("))");
        }

        private void VisitInSubquery(SqlInSubquery n)
        {
            IDataMapItem item = n.TargetMap.DataItem;
            string field = _dialect.FormatFieldName(n.SelectFieldRaw, UseQuotedIdentifier);
            string table = _dialect.FormatTableName(item.TableName, item.SchemaName, item.UseQuotedIdentifier ?? UseQuotedIdentifier);
            if (NoLock && _dialect.SupportsNoLock)
                table += " WITH (NOLOCK)";

            _sb.Append('(');
            Visit(n.Operand);
            _sb.Append(" IN (SELECT ").Append(field).Append(" FROM ").Append(table).Append(" WHERE ");
            Visit(n.Where);
            _sb.Append("))");
        }

        private string ColumnText(SqlColumn c)
        {
            IDataMapField field = c.Field;
            string name = _dialect.FormatFieldName(field.FieldName, field.UseQuotedIdentifier ?? UseQuotedIdentifier);

            if (QualifyColumns)
            {
                IDataMapItem item = c.Map.DataItem;
                string table = _dialect.FormatTableName(item.TableName, item.SchemaName, item.UseQuotedIdentifier ?? UseQuotedIdentifier);
                name = table + "." + name;
            }

            return name;
        }

        private void AppendValue(object value)
        {
            if (ParameterizeLiterals)
            {
                _parameters.Add(value);
                _sb.Append(ParameterPrefix)
                   .Append((_parameters.Count + ParameterIndexModifier - 1).ToString(CultureInfo.InvariantCulture));
            }
            else
            {
                _sb.Append(InlineLiteral(value));
            }
        }

        private string InlineLiteral(object value)
        {
            switch (value)
            {
                case null: return "NULL";
                case string s: return "'" + s.Replace("'", "''") + "'";
                case char ch: return "'" + (ch == '\'' ? "''" : ch.ToString()) + "'";
                case DateTime dt: return "'" + dt.ToString("yyyy-MM-dd HH:mm:ss.fff", CultureInfo.InvariantCulture) + "'";
                case Guid g: return _dialect.FormatGuidLiteral(g);
                case bool b: return b ? "1" : "0";
                case Enum e: return Convert.ToString(Convert.ChangeType(e, Enum.GetUnderlyingType(e.GetType()), CultureInfo.InvariantCulture), CultureInfo.InvariantCulture);
                default: return Convert.ToString(value, CultureInfo.InvariantCulture);
            }
        }

        private static bool AllSafeLiterals(IReadOnlyList<object> values)
        {
            foreach (object v in values)
            {
                switch (v)
                {
                    case byte _: case short _: case int _: case long _: case Guid _:
                    case sbyte _: case ushort _: case uint _: case ulong _:
                        continue;
                    default:
                        return false;
                }
            }
            return true;
        }

        private static string OpText(SqlBinaryOp op)
        {
            switch (op)
            {
                case SqlBinaryOp.And: return " AND ";
                case SqlBinaryOp.Or: return " OR ";
                case SqlBinaryOp.Equal: return " = ";
                case SqlBinaryOp.NotEqual: return " != ";
                case SqlBinaryOp.LessThan: return " < ";
                case SqlBinaryOp.LessThanOrEqual: return " <= ";
                case SqlBinaryOp.GreaterThan: return " > ";
                case SqlBinaryOp.GreaterThanOrEqual: return " >= ";
                case SqlBinaryOp.Add: return " + ";
                case SqlBinaryOp.Subtract: return " - ";
                case SqlBinaryOp.Multiply: return " * ";
                case SqlBinaryOp.Divide: return " / ";
                case SqlBinaryOp.Modulo: return " % ";
                case SqlBinaryOp.BitAnd: return " & ";
                case SqlBinaryOp.BitOr: return " | ";
                default: throw new SqlExpressionException($"Unknown binary operator '{op}'");
            }
        }
    }
}
