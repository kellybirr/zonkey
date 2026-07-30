using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq.Expressions;
using System.Reflection;
using Zonkey.Dialects;

namespace Zonkey.ObjectModel.QueryTranslation
{
    internal sealed class ExpressionTranslator
    {
        private readonly Dictionary<string, DataMap> _maps;

        public SqlDialect Dialect { get; }

        public ExpressionTranslator(Dictionary<string, DataMap> maps, SqlDialect dialect)
        {
            _maps = maps ?? throw new ArgumentNullException(nameof(maps));
            Dialect = dialect ?? throw new ArgumentNullException(nameof(dialect));
        }

        public SqlNode TranslatePredicate(Expression e)
        {
            SqlNode node = Translate(e);

            if (node is SqlColumn c && c.IsBoolean)
                return new SqlBoolPredicate { Column = c };
            if (node is SqlValue v && v.Value is bool b)
                return new SqlLiteral { Text = b ? "1 = 1" : "1 = 0" };
            // only COALESCE/CASE_WHEN produce a scalar value that needs "= 1" style boolean coercion;
            // other bool-returning functions (e.g. ISNULLOREMPTY) already render a complete predicate.
            if (node is SqlFunction sf && (sf.Name == "COALESCE" || sf.Name == "CASE_WHEN") && IsBooleanType(e.Type))
                return new SqlBoolExprPredicate { Operand = node };
            return node;
        }

        public SqlNode Translate(Expression e)
        {
            switch (e.NodeType)
            {
                case ExpressionType.AndAlso:
                    return Logical((BinaryExpression)e, SqlBinaryOp.And);
                case ExpressionType.And:
                    return IsBooleanType(e.Type)
                        ? Logical((BinaryExpression)e, SqlBinaryOp.And)
                        : Arithmetic((BinaryExpression)e, SqlBinaryOp.BitAnd);
                case ExpressionType.OrElse:
                    return Logical((BinaryExpression)e, SqlBinaryOp.Or);
                case ExpressionType.Or:
                    return IsBooleanType(e.Type)
                        ? Logical((BinaryExpression)e, SqlBinaryOp.Or)
                        : Arithmetic((BinaryExpression)e, SqlBinaryOp.BitOr);
                case ExpressionType.Equal:
                    return Comparison((BinaryExpression)e, SqlBinaryOp.Equal);
                case ExpressionType.NotEqual:
                    return Comparison((BinaryExpression)e, SqlBinaryOp.NotEqual);
                case ExpressionType.LessThan:
                    return Comparison((BinaryExpression)e, SqlBinaryOp.LessThan);
                case ExpressionType.LessThanOrEqual:
                    return Comparison((BinaryExpression)e, SqlBinaryOp.LessThanOrEqual);
                case ExpressionType.GreaterThan:
                    return Comparison((BinaryExpression)e, SqlBinaryOp.GreaterThan);
                case ExpressionType.GreaterThanOrEqual:
                    return Comparison((BinaryExpression)e, SqlBinaryOp.GreaterThanOrEqual);
                case ExpressionType.Add:
                case ExpressionType.AddChecked:
                    return Arithmetic((BinaryExpression)e, SqlBinaryOp.Add);
                case ExpressionType.Subtract:
                case ExpressionType.SubtractChecked:
                    return Arithmetic((BinaryExpression)e, SqlBinaryOp.Subtract);
                case ExpressionType.Multiply:
                case ExpressionType.MultiplyChecked:
                    return Arithmetic((BinaryExpression)e, SqlBinaryOp.Multiply);
                case ExpressionType.Divide:
                    return Arithmetic((BinaryExpression)e, SqlBinaryOp.Divide);
                case ExpressionType.Modulo:
                    return Arithmetic((BinaryExpression)e, SqlBinaryOp.Modulo);
                case ExpressionType.Not:
                    return new SqlNot { Operand = TranslatePredicate(((UnaryExpression)e).Operand) };
                case ExpressionType.Negate:
                case ExpressionType.NegateChecked:
                    return new SqlNegate { Operand = Translate(((UnaryExpression)e).Operand) };
                case ExpressionType.Convert:
                case ExpressionType.ConvertChecked:
                    return Translate(((UnaryExpression)e).Operand);
                case ExpressionType.Coalesce:
                {
                    var b = (BinaryExpression)e;
                    return new SqlFunction { Name = "COALESCE", Args = new[] { Translate(b.Left), Translate(b.Right) } };
                }
                case ExpressionType.Conditional:
                {
                    var c = (ConditionalExpression)e;
                    return new SqlFunction
                    {
                        Name = "CASE_WHEN",
                        Args = new[] { TranslatePredicate(c.Test), Translate(c.IfTrue), Translate(c.IfFalse) }
                    };
                }
                case ExpressionType.Constant:
                    return FromConstant(((ConstantExpression)e).Value);
                case ExpressionType.MemberAccess:
                    return TranslateMember((MemberExpression)e);
                case ExpressionType.Call:
                    return TranslateCall((MethodCallExpression)e);
                default:
                    throw SqlExpressionException.ForNode(e, $"expression type '{e.NodeType}' has no SQL translation");
            }
        }

        private static bool IsBooleanType(Type t)
        {
            return t == typeof(bool) || t == typeof(bool?);
        }

        private SqlNode Logical(BinaryExpression e, SqlBinaryOp op)
        {
            return new SqlBinary { Op = op, Left = TranslatePredicate(e.Left), Right = TranslatePredicate(e.Right) };
        }

        private SqlNode Arithmetic(BinaryExpression e, SqlBinaryOp op)
        {
            return new SqlBinary { Op = op, Left = Translate(e.Left), Right = Translate(e.Right) };
        }

        private SqlNode Comparison(BinaryExpression e, SqlBinaryOp op)
        {
            SqlNode left = Translate(e.Left);
            SqlNode right = Translate(e.Right);

            if (op == SqlBinaryOp.Equal || op == SqlBinaryOp.NotEqual)
            {
                bool not = op == SqlBinaryOp.NotEqual;
                if (right is SqlValue rv && rv.Value == null)
                    return new SqlIsNull { Operand = left, Not = not };
                if (left is SqlValue lv && lv.Value == null)
                    return new SqlIsNull { Operand = right, Not = not };
            }

            return new SqlBinary { Op = op, Left = left, Right = right };
        }

        internal static SqlNode FromConstant(object value)
        {
            // enums pass through untouched — PostgreSqlDialect.FixParameter / native enum mapping decides their wire type
            return new SqlValue { Value = value };
        }

        private SqlNode TranslateMember(MemberExpression m)
        {
            Type declaring = m.Member.DeclaringType;
            if (declaring != null && declaring.IsGenericType && declaring.GetGenericTypeDefinition() == typeof(Nullable<>))
            {
                // NOTE: the legacy parser emitted "IS NULL" for HasValue — that was a bug; HasValue means NOT NULL.
                if (m.Member.Name == "HasValue")
                    return new SqlIsNull { Operand = Translate(m.Expression), Not = true };
                if (m.Member.Name == "Value")
                    return Translate(m.Expression);
            }

            if (MethodTranslators.TryGetMember(m.Member, out MemberTranslator mt))
                return mt(this, m);

            if (m.Expression is ParameterExpression pex)
            {
                DataMap map = _maps[pex.Name];
                IDataMapField field = map.GetFieldForProperty((PropertyInfo)m.Member);
                if (field == null)
                    throw SqlExpressionException.ForNode(m, $"property '{m.Member.Name}' is not mapped to a column on '{map.ObjectType.Name}'");

                Type propType = field.Property.PropertyType;
                bool isBoolean = propType == typeof(bool) || propType == typeof(bool?);
                return new SqlColumn { Field = field, Map = map, IsBoolean = isBoolean };
            }

            throw SqlExpressionException.ForNode(m,
                "member access could not be translated",
                "if it produces a value, it must not reference the lambda parameter");
        }

        private SqlNode TranslateCall(MethodCallExpression call)
        {
            if (MethodTranslators.TryGetMethod(call.Method, out MethodTranslator t))
                return t(this, call);

            // instance Contains on a collection type (List<T>.Contains etc.) => IN
            if (call.Method.Name == "Contains" && call.Object != null
                && call.Method.DeclaringType != typeof(string)
                && typeof(IEnumerable).IsAssignableFrom(call.Method.DeclaringType))
            {
                return TranslateInList(call.Arguments[0], call.Object, emptyThrows: false);
            }

            throw SqlExpressionException.ForNode(call,
                $"method '{call.Method.DeclaringType?.Name}.{call.Method.Name}' has no SQL translation");
        }

        internal SqlNode TranslateInList(Expression operandExpr, Expression listExpr, bool emptyThrows)
        {
            SqlNode operand = Translate(operandExpr);

            SqlNode listNode = Translate(listExpr);
            if (!(listNode is SqlValue lv) || lv.Value is string || !(lv.Value is IEnumerable seq))
                throw SqlExpressionException.ForNode(listExpr, "the IN list must be a client-side sequence of values");

            var values = new List<object>();
            bool sawNull = false;
            foreach (object v in seq)
            {
                if (v == null) { sawNull = true; continue; }
                values.Add(v);
            }

            if (values.Count == 0)
            {
                if (emptyThrows)
                    throw new ArgumentException("Attempted to translate an IN over a sequence that contained zero values");

                // new surface: a list containing only null(s) matches NULL rows (C#/EF Contains semantics)
                if (sawNull)
                    return new SqlIsNull { Operand = operand };

                return new SqlLiteral { Text = "1 = 0" };
            }

            SqlNode inValues = new SqlInValues { Operand = operand, Values = values };

            // new surface: a null in the list also matches NULL rows; legacy SqlIn/SqlInInt/SqlInGuid keep skip-nulls behavior
            if (!emptyThrows && sawNull)
                return new SqlBinary { Op = SqlBinaryOp.Or, Left = inValues, Right = new SqlIsNull { Operand = operand } };

            return inValues;
        }
    }
}
