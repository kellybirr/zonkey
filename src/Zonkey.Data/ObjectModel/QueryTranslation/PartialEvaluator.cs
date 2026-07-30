using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Reflection;
using System.Runtime.ExceptionServices;

namespace Zonkey.ObjectModel.QueryTranslation
{
    /// <summary>
    /// Replaces every maximal subtree that does not reference a lambda parameter
    /// with a ConstantExpression holding its runtime value (EF-style "funcletizer").
    /// </summary>
    internal static class PartialEvaluator
    {
        public static Expression Reduce(Expression expression)
        {
            var nominator = new Nominator();
            nominator.Visit(expression);
            return new SubtreeEvaluator(nominator.Candidates).Visit(expression);
        }

        private sealed class Nominator : ExpressionVisitor
        {
            public readonly HashSet<Expression> Candidates = new HashSet<Expression>();
            private bool _cannotBeEvaluated;

            public override Expression Visit(Expression node)
            {
                if (node == null) return null;

                bool saved = _cannotBeEvaluated;
                _cannotBeEvaluated = false;
                base.Visit(node);

                if (!_cannotBeEvaluated)
                {
                    if (CanBeEvaluatedLocally(node))
                        Candidates.Add(node);
                    else
                        _cannotBeEvaluated = true;
                }

                _cannotBeEvaluated |= saved;
                return node;
            }

            private static bool CanBeEvaluatedLocally(Expression e)
            {
                if (e.NodeType == ExpressionType.Parameter) return false;
                if (e.NodeType == ExpressionType.Lambda) return false;   // subquery/marker lambdas must survive
                if (e is MethodCallExpression m && m.Method.DeclaringType == typeof(Extensions.SqlFilterExtensions))
                    return false;                                        // marker methods throw if invoked
                if (IsByRefLike(e.Type)) return false;                   // spans etc. cannot be reflection-evaluated
                if (IsEnumConvert(e)) return false;                      // e.g. nullable-enum equality emits Convert(enumConst, int?);
                                                                          // DynamicInvoke-ing it here would collapse the enum to its
                                                                          // underlying integer before ExpressionTranslator ever sees it.
                                                                          // Leave the Convert node in place -- ExpressionTranslator
                                                                          // already unwraps Convert nodes and ignores the target type,
                                                                          // so the untouched enum operand still gets folded on its own.
                return true;
            }

            // true for Convert/ConvertChecked nodes whose operand is an enum (nullable or not) -- these are the
            // nodes the C#/Roslyn expression-tree compiler inserts around enum literals in nullable-enum
            // comparisons, converting to the underlying integral type rather than to the nullable enum type.
            private static bool IsEnumConvert(Expression e)
            {
                if (e.NodeType != ExpressionType.Convert && e.NodeType != ExpressionType.ConvertChecked)
                    return false;

                Type operandType = ((UnaryExpression)e).Operand.Type;
                Type underlying = Nullable.GetUnderlyingType(operandType) ?? operandType;
                return underlying.IsEnum;
            }

            private static bool IsByRefLike(System.Type t)
            {
#if NETFRAMEWORK
                foreach (CustomAttributeData attr in t.CustomAttributes)
                {
                    if (attr.AttributeType.FullName == "System.Runtime.CompilerServices.IsByRefLikeAttribute")
                        return true;
                }
                return false;
#else
                return t.IsByRefLike;
#endif
            }
        }

        private sealed class SubtreeEvaluator : ExpressionVisitor
        {
            private readonly HashSet<Expression> _candidates;

            public SubtreeEvaluator(HashSet<Expression> candidates)
            {
                _candidates = candidates;
            }

            public override Expression Visit(Expression node)
            {
                if (node == null) return null;
                if (_candidates.Contains(node)) return Evaluate(node);
                return base.Visit(node);
            }

            private static Expression Evaluate(Expression e)
            {
                if (e.NodeType == ExpressionType.Constant) return e;

                try
                {
                    if (TryEvaluateMemberChain(e, out object chainValue))
                        return Expression.Constant(chainValue, e.Type);
                }
                catch (TargetInvocationException tie) when (tie.InnerException != null)
                {
                    // PropertyInfo.GetValue (used by the fast member-chain path) wraps getter
                    // exceptions in TargetInvocationException just like DynamicInvoke below.
                    ExceptionDispatchInfo.Capture(tie.InnerException).Throw();
                    throw;   // unreachable
                }

                object value;
                try
                {
                    value = Expression.Lambda(e).Compile().DynamicInvoke();
                }
                catch (TargetInvocationException tie) when (tie.InnerException != null)
                {
                    ExceptionDispatchInfo.Capture(tie.InnerException).Throw();
                    throw;   // unreachable
                }
                return Expression.Constant(value, e.Type);
            }

            // fast path: constant-rooted field/property chains, avoiding Compile()
            private static bool TryEvaluateMemberChain(Expression e, out object value)
            {
                value = null;

                if (e is ConstantExpression c)
                {
                    value = c.Value;
                    return true;
                }

                if (e is MemberExpression m)
                {
                    object instance = null;
                    if (m.Expression != null && !TryEvaluateMemberChain(m.Expression, out instance))
                        return false;

                    switch (m.Member)
                    {
                        case FieldInfo fi:
                            value = fi.GetValue(instance);
                            return true;
                        case PropertyInfo pi:
                            value = pi.GetValue(instance, null);
                            return true;
                        default:
                            return false;
                    }
                }

                return false;
            }
        }
    }
}
