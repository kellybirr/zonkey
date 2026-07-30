using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text.RegularExpressions;
using Zonkey.Extensions;

namespace Zonkey.ObjectModel.QueryTranslation
{
    internal delegate SqlNode MethodTranslator(ExpressionTranslator t, MethodCallExpression call);
    internal delegate SqlNode MemberTranslator(ExpressionTranslator t, MemberExpression member);

    internal static class MethodTranslators
    {
        private static readonly Dictionary<MethodInfo, MethodTranslator> Methods = new Dictionary<MethodInfo, MethodTranslator>();
        private static readonly Dictionary<MemberInfo, MemberTranslator> Members = new Dictionary<MemberInfo, MemberTranslator>();

        static MethodTranslators()
        {
            // registrations are added by feature area (strings, LIKE, IN, regex, date/math)
            Type str = typeof(string);

            MethodTranslator fn1(string name) => (t, c) =>
                new SqlFunction { Name = name, Args = new[] { t.Translate(c.Object) } };

            Register(str.GetMethod("ToUpper", Type.EmptyTypes), fn1("UPPER"));
            Register(str.GetMethod("ToLower", Type.EmptyTypes), fn1("LOWER"));
            Register(str.GetMethod("Trim", Type.EmptyTypes), fn1("TRIM"));

            Register(str.GetMethod("Replace", new[] { str, str }), (t, c) =>
                new SqlFunction { Name = "REPLACE", Args = new[] { t.Translate(c.Object), t.Translate(c.Arguments[0]), t.Translate(c.Arguments[1]) } });

            Register(str.GetMethod("Substring", new[] { typeof(int) }), (t, c) =>
                new SqlFunction { Name = "SUBSTRING2", Args = new[] { t.Translate(c.Object), PlusOne(t, c.Arguments[0]) } });
            Register(str.GetMethod("Substring", new[] { typeof(int), typeof(int) }), (t, c) =>
                new SqlFunction { Name = "SUBSTRING", Args = new[] { t.Translate(c.Object), PlusOne(t, c.Arguments[0]), t.Translate(c.Arguments[1]) } });

            Register(str.GetMethod("IndexOf", new[] { str }), (t, c) =>
                new SqlFunction { Name = "INDEXOF", Args = new[] { t.Translate(c.Object), t.Translate(c.Arguments[0]) } });

            Register(str.GetMethod("IsNullOrEmpty", new[] { str }), (t, c) =>
                new SqlFunction { Name = "ISNULLOREMPTY", Args = new[] { t.Translate(c.Arguments[0]) } });

            Register(str.GetMethod("Equals", new[] { str }), (t, c) =>
                EqualsNode(t.Translate(c.Object), t.Translate(c.Arguments[0])));
            Register(str.GetMethod("Equals", new[] { str, str }), (t, c) =>
                EqualsNode(t.Translate(c.Arguments[0]), t.Translate(c.Arguments[1])));

            Register(str.GetMethod("StartsWith", new[] { str }), (t, c) => BuildLike(t, c, false, true));
            Register(str.GetMethod("StartsWith", new[] { str, typeof(StringComparison) }), (t, c) => BuildLike(t, c, false, true));
            Register(str.GetMethod("EndsWith", new[] { str }), (t, c) => BuildLike(t, c, true, false));
            Register(str.GetMethod("EndsWith", new[] { str, typeof(StringComparison) }), (t, c) => BuildLike(t, c, true, false));
            Register(str.GetMethod("Contains", new[] { str }), (t, c) => BuildLike(t, c, true, true));
            Register(str.GetMethod("Contains", new[] { str, typeof(StringComparison) }), (t, c) => BuildLike(t, c, true, true));  // null on net48; Register ignores null

            // char overloads (StartsWith(char)/EndsWith(char)/Contains(char)): exist on modern .NET only;
            // GetMethod returns null on net48 and Register is null-tolerant, matching the pattern above.
            Register(str.GetMethod("StartsWith", new[] { typeof(char) }), (t, c) => BuildLike(t, c, false, true));
            Register(str.GetMethod("EndsWith", new[] { typeof(char) }), (t, c) => BuildLike(t, c, true, false));
            Register(str.GetMethod("Contains", new[] { typeof(char) }), (t, c) => BuildLike(t, c, true, true));

            Register(str.GetMethod("Equals", new[] { str, typeof(StringComparison) }), (t, c) => BuildEquals(t, c, t.Translate(c.Object), t.Translate(c.Arguments[0])));
            Register(str.GetMethod("Equals", new[] { str, str, typeof(StringComparison) }), (t, c) => BuildEquals(t, c, t.Translate(c.Arguments[0]), t.Translate(c.Arguments[1])));

            Register(typeof(SqlFilterExtensions).GetMethod("SqlLike"), (t, c) =>
                new SqlLike { Operand = t.Translate(c.Arguments[0]), Pattern = t.Translate(c.Arguments[1]), IgnoreCase = false, Escaped = false });
            Register(typeof(SqlFilterExtensions).GetMethod("SqlILike"), (t, c) =>
                new SqlLike { Operand = t.Translate(c.Arguments[0]), Pattern = t.Translate(c.Arguments[1]), IgnoreCase = true, Escaped = false });

            Register(typeof(Regex).GetMethod("IsMatch", new[] { str, str }), (t, c) =>
                new SqlRegexMatch { Operand = t.Translate(c.Arguments[0]), Pattern = t.Translate(c.Arguments[1]), IgnoreCase = false });
            Register(typeof(Regex).GetMethod("IsMatch", new[] { str, str, typeof(RegexOptions) }), (t, c) =>
            {
                if (!(c.Arguments[2] is ConstantExpression oc))
                    throw SqlExpressionException.ForNode(c, "the RegexOptions argument must be a constant");
                var opts = (RegexOptions)oc.Value;
                if (opts != RegexOptions.None && opts != RegexOptions.IgnoreCase)
                    throw SqlExpressionException.ForNode(c, "only RegexOptions.None and RegexOptions.IgnoreCase can be translated");
                return new SqlRegexMatch { Operand = t.Translate(c.Arguments[0]), Pattern = t.Translate(c.Arguments[1]), IgnoreCase = opts == RegexOptions.IgnoreCase };
            });

            // static Enumerable.Contains<T>(source, value)
            MethodInfo enumerableContains = typeof(System.Linq.Enumerable).GetMethods()
                .First(m => m.Name == "Contains" && m.GetParameters().Length == 2);
            Register(enumerableContains, (t, c) => t.TranslateInList(c.Arguments[1], c.Arguments[0], emptyThrows: false));

#if !NETFRAMEWORK
            // C# 14 first-class spans: array.Contains binds to MemoryExtensions.Contains(ReadOnlySpan<T>, T).
            // For element types that don't satisfy the plain IEquatable<T> constraint (e.g. Nullable<T>),
            // the compiler instead binds the 3-arg overload that takes a trailing IEqualityComparer<T>? (passed as null).
            foreach (MethodInfo m in typeof(MemoryExtensions).GetMethods())
            {
                if (m.Name != "Contains") continue;
                ParameterInfo[] ps = m.GetParameters();
                if (ps.Length != 2 && ps.Length != 3) continue;

                Type p0 = ps[0].ParameterType;
                if (!p0.IsGenericType) continue;
                Type def = p0.GetGenericTypeDefinition();
                if (def != typeof(ReadOnlySpan<>) && def != typeof(Span<>)) continue;

                if (ps.Length == 3)
                {
                    Type p2 = ps[2].ParameterType;
                    if (!p2.IsGenericType || p2.GetGenericTypeDefinition() != typeof(IEqualityComparer<>)) continue;
                }

                Register(m, (t, c) => t.TranslateInList(c.Arguments[1], UnwrapSpanConversion(c.Arguments[0]), emptyThrows: false));
            }
#endif

            // legacy markers: SqlIn(field, IEnumerable) — the IEnumerable overload only (lambda overloads are Task 7)
            foreach (MethodInfo m in typeof(SqlFilterExtensions).GetMethods())
            {
                if (m.Name == "SqlIn" && m.GetParameters().Length == 2
                    && typeof(System.Collections.IEnumerable).IsAssignableFrom(m.GetParameters()[1].ParameterType))
                {
                    Register(m, (t, c) => t.TranslateInList(c.Arguments[0], c.Arguments[1], emptyThrows: true));
                }
                if (m.Name == "SqlInInt" || m.Name == "SqlInGuid")
                {
                    Register(m, (t, c) => ForceInline(t.TranslateInList(c.Arguments[0], c.Arguments[1], emptyThrows: true)));
                }
                if (m.Name == "SqlIn" && m.GetParameters().Length == 3)
                    Register(m, TranslateSqlInSubquery);
                if (m.Name == "SqlIn" && m.GetParameters().Length == 2
                    && !typeof(System.Collections.IEnumerable).IsAssignableFrom(m.GetParameters()[1].ParameterType))
                    Register(m, TranslateSqlInSubquery);
            }

            RegisterMember(str.GetProperty("Length"), (t, m) =>
                new SqlFunction { Name = "LENGTH", Args = new[] { t.Translate(m.Expression) } });

            // Date/Math translations (Task 10)
            Type dt = typeof(DateTime);
            MemberTranslator datePart(string logical) => (t, m) =>
                new SqlFunction { Name = logical, Args = new[] { t.Translate(m.Expression) } };

            RegisterMember(dt.GetProperty("Year"), datePart("DATE_YEAR"));
            RegisterMember(dt.GetProperty("Month"), datePart("DATE_MONTH"));
            RegisterMember(dt.GetProperty("Day"), datePart("DATE_DAY"));
            RegisterMember(dt.GetProperty("Hour"), datePart("DATE_HOUR"));
            RegisterMember(dt.GetProperty("Minute"), datePart("DATE_MINUTE"));
            RegisterMember(dt.GetProperty("Second"), datePart("DATE_SECOND"));
            RegisterMember(dt.GetProperty("Date"), datePart("DATE_DATE"));

            foreach (MethodInfo m in typeof(Math).GetMethods())
            {
                if (m.GetParameters().Length == 1)
                {
                    if (m.Name == "Abs") Register(m, MathFn("ABS"));
                    if (m.Name == "Floor") Register(m, MathFn("FLOOR"));
                    if (m.Name == "Ceiling") Register(m, MathFn("CEILING"));
                    if (m.Name == "Round") Register(m, MathFn("ROUND1"));
                }
                if (m.GetParameters().Length == 2 && m.Name == "Round" && m.GetParameters()[1].ParameterType == typeof(int))
                {
                    Register(m, (t, c) => new SqlFunction { Name = "ROUND2", Args = new[] { t.Translate(c.Arguments[0]), t.Translate(c.Arguments[1]) } });
                }
            }
        }

        // C# Substring is 0-based; SQL is 1-based
        private static SqlNode PlusOne(ExpressionTranslator t, Expression e)
        {
            SqlNode n = t.Translate(e);
            if (n is SqlValue v && v.Value is int i) return new SqlValue { Value = i + 1 };
            return new SqlBinary { Op = SqlBinaryOp.Add, Left = n, Right = new SqlValue { Value = 1 } };
        }

        private static MethodTranslator MathFn(string logical)
        {
            return (t, c) => new SqlFunction { Name = logical, Args = new[] { t.Translate(c.Arguments[0]) } };
        }

        private static bool IsIgnoreCase(MethodCallExpression call)
        {
            var last = call.Arguments[call.Arguments.Count - 1];
            if (last.Type != typeof(StringComparison)) return false;
            if (!(last is ConstantExpression c))
                throw SqlExpressionException.ForNode(call, "the StringComparison argument must be a constant");
            var cmp = (StringComparison)c.Value;
            return cmp == StringComparison.OrdinalIgnoreCase
                || cmp == StringComparison.CurrentCultureIgnoreCase
                || cmp == StringComparison.InvariantCultureIgnoreCase;
        }

        private static string EscapeLikeValue(string s, out bool didEscape)
        {
            didEscape = s.IndexOfAny(new[] { '%', '_', '\\', '[' }) >= 0;
            if (!didEscape) return s;
            var sb = new System.Text.StringBuilder(s.Length + 4);
            foreach (char ch in s)
            {
                if (ch == '%' || ch == '_' || ch == '\\' || ch == '[') sb.Append('\\');
                sb.Append(ch);
            }
            return sb.ToString();
        }

        private static SqlNode BuildLike(ExpressionTranslator t, MethodCallExpression c, bool pctBefore, bool pctAfter)
        {
            bool ignoreCase = IsIgnoreCase(c);
            SqlNode operand = t.Translate(c.Object);
            SqlNode pattern = t.Translate(c.Arguments[0]);

            if (pattern is SqlValue pv && (pv.Value is string || pv.Value is char))
            {
                string s = pv.Value is char ch ? ch.ToString() : (string)pv.Value;
                string escaped = EscapeLikeValue(s, out bool didEscape);
                string final = (pctBefore ? "%" : "") + escaped + (pctAfter ? "%" : "");
                return new SqlLike { Operand = operand, Pattern = new SqlValue { Value = final }, IgnoreCase = ignoreCase, Escaped = didEscape };
            }

            // non-constant pattern (e.g. another column): concat wildcards, no escaping possible
            SqlNode concat = pattern;
            if (pctBefore) concat = new SqlFunction { Name = "CONCAT", Args = new[] { (SqlNode)new SqlValue { Value = "%" }, concat } };
            if (pctAfter) concat = new SqlFunction { Name = "CONCAT", Args = new[] { concat, (SqlNode)new SqlValue { Value = "%" } } };
            return new SqlLike { Operand = operand, Pattern = concat, IgnoreCase = ignoreCase, Escaped = false };
        }

        private static SqlNode BuildEquals(ExpressionTranslator t, MethodCallExpression c, SqlNode left, SqlNode right)
        {
            SqlNode nullCheck = EqualsNode(left, right);
            if (nullCheck is SqlIsNull) return nullCheck;

            if (!IsIgnoreCase(c))
                return nullCheck;
            return new SqlBinary
            {
                Op = SqlBinaryOp.Equal,
                Left = new SqlFunction { Name = "UPPER", Args = new[] { left } },
                Right = new SqlFunction { Name = "UPPER", Args = new[] { right } }
            };
        }

        private static SqlNode EqualsNode(SqlNode left, SqlNode right)
        {
            if (right is SqlValue rv && rv.Value == null) return new SqlIsNull { Operand = left };
            if (left is SqlValue lv && lv.Value == null) return new SqlIsNull { Operand = right };
            return new SqlBinary { Op = SqlBinaryOp.Equal, Left = left, Right = right };
        }

        public static void Register(MethodInfo method, MethodTranslator translator)
        {
            if (method != null) Methods[method] = translator;
        }

        public static void RegisterMember(MemberInfo member, MemberTranslator translator)
        {
            if (member != null) Members[member] = translator;
        }

        public static bool TryGetMethod(MethodInfo method, out MethodTranslator translator)
        {
            if (Methods.TryGetValue(method, out translator)) return true;
            if (method.IsGenericMethod && Methods.TryGetValue(method.GetGenericMethodDefinition(), out translator)) return true;
            return false;
        }

        public static bool TryGetMember(MemberInfo member, out MemberTranslator translator)
        {
            return Members.TryGetValue(member, out translator);
        }

#if !NETFRAMEWORK
        // array -> span implicit conversions appear as op_Implicit calls or Convert nodes
        private static Expression UnwrapSpanConversion(Expression e)
        {
            if (e is MethodCallExpression mc && mc.Method.Name == "op_Implicit" && mc.Arguments.Count == 1)
                return mc.Arguments[0];
            if (e is UnaryExpression u && (u.NodeType == ExpressionType.Convert || u.NodeType == ExpressionType.ConvertChecked))
                return u.Operand;
            return e;
        }
#endif

        private static SqlNode ForceInline(SqlNode node)
        {
            if (node is SqlInValues inv) return new SqlInValuesInline { Operand = inv.Operand, Values = inv.Values };
            return node;
        }

        private static SqlNode TranslateSqlInSubquery(ExpressionTranslator t, MethodCallExpression c)
        {
            SqlNode operand = t.Translate(c.Arguments[0]);

            var whereLambda = (LambdaExpression)StripQuotes(c.Arguments[c.Arguments.Count - 1]);
            Type targetType = whereLambda.Parameters[0].Type;
            DataMap map = DataMap.GenerateCached(targetType);

            string rawField;
            if (c.Arguments.Count == 3)
            {
                var fieldLambda = (LambdaExpression)StripQuotes(c.Arguments[1]);
                Expression body = fieldLambda.Body;
                while (body is UnaryExpression u && (u.NodeType == ExpressionType.Convert || u.NodeType == ExpressionType.ConvertChecked))
                    body = u.Operand;

                if (!(body is MemberExpression member) || !(member.Member is PropertyInfo property))
                    throw SqlExpressionException.ForNode(fieldLambda, "the SqlIn field selector must be a simple mapped property access");

                IDataMapField f = map.GetFieldForProperty(property);
                if (f == null)
                    throw SqlExpressionException.ForNode(fieldLambda, $"property '{member.Member.Name}' is not mapped on '{targetType.Name}'");
                rawField = f.FieldName;
            }
            else
            {
                Expression opExpr = c.Arguments[0];
                while (opExpr is UnaryExpression u && (u.NodeType == ExpressionType.Convert || u.NodeType == ExpressionType.ConvertChecked))
                    opExpr = u.Operand;
                string memberName = ((MemberExpression)opExpr).Member.Name;

                PropertyInfo targetProp = targetType.GetProperty(memberName);
                IDataMapField targetField = (targetProp != null) ? map.GetFieldForProperty(targetProp) : null;
                if (targetField != null)
                {
                    rawField = targetField.FieldName;
                }
                else if (map.ContainsField(memberName))
                {
                    rawField = memberName;
                }
                else
                {
                    throw new ArgumentOutOfRangeException("fieldExpression",
                        $"Field `{memberName}` not found on object `{map.DataItem.TableName}`");
                }
            }

            var childMaps = new Dictionary<string, DataMap> { { whereLambda.Parameters[0].Name, map } };
            var child = new ExpressionTranslator(childMaps, t.Dialect);
            SqlNode where = child.TranslatePredicate(PartialEvaluator.Reduce(whereLambda.Body));

            return new SqlInSubquery { Operand = operand, SelectFieldRaw = rawField, TargetMap = map, Where = where };
        }

        private static Expression StripQuotes(Expression e)
        {
            while (e.NodeType == ExpressionType.Quote)
                e = ((UnaryExpression)e).Operand;
            return e;
        }
    }
}
