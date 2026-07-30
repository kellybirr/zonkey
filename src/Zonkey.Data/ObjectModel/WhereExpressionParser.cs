using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq.Expressions;
using Zonkey.Dialects;
using Zonkey.ObjectModel.QueryTranslation;

namespace Zonkey.ObjectModel
{
    class WhereExpressionParser<T> : WhereExpressionParser
    {
        public WhereExpressionParser(SqlDialect dialect) : base(dialect)
        { }

        public WhereExpressionParser(DataMap map, SqlDialect dialect) : base(new[] { map }, dialect)
        { }

        public SqlWhereClause Parse(Expression<Func<T, bool>> expression)
        {
            return base.Parse(expression);
        }
    }

    /// <summary>
    /// Facade over the three-stage translation pipeline:
    /// PartialEvaluator (client-value folding) -> ExpressionTranslator (SQL AST) -> SqlTextGenerator (dialect text).
    /// </summary>
    class WhereExpressionParser
    {
        private readonly IEnumerable<DataMap> _mapHints;

        public bool? UseQuotedIdentifier { get; set; }

        public SqlDialect SqlDialect { get; set; }

        public bool ParameterizeLiterals { get; set; }

        public char ParameterPrefix { get; set; }

        public bool? UseTableWithFieldNames { get; set; }

        public int ParameterIndexModifier { get; set; }

        public bool NoLock { get; set; }

        public WhereExpressionParser(SqlDialect dialect) : this(new DataMap[0], dialect)
        { }

        public WhereExpressionParser(IEnumerable<DataMap> maps, SqlDialect dialect)
        {
            _mapHints = maps ?? throw new ArgumentNullException(nameof(maps));
            SqlDialect = dialect ?? throw new ArgumentNullException(nameof(dialect));

            ParameterizeLiterals = true;
            ParameterPrefix = '$';
        }

        public SqlWhereClause Parse(LambdaExpression expression)
        {
            return Parse(expression, new ArrayList());
        }

        internal SqlWhereClause Parse(LambdaExpression expression, ArrayList paramList)
        {
            var maps = new Dictionary<string, DataMap>();
            foreach (ParameterExpression p in expression.Parameters)
            {
                DataMap map = null;
                foreach (DataMap hint in _mapHints)
                {
                    if (hint.ObjectType != p.Type) continue;
                    map = hint;
                    break;
                }

                maps.Add(p.Name, map ?? DataMap.GenerateCached(p.Type));
            }

            Expression body = PartialEvaluator.Reduce(expression.Body);

            var translator = new ExpressionTranslator(maps, SqlDialect);
            SqlNode root = translator.TranslatePredicate(body);

            var generator = new SqlTextGenerator(SqlDialect, paramList)
            {
                ParameterPrefix = ParameterPrefix,
                ParameterIndexModifier = ParameterIndexModifier,
                UseQuotedIdentifier = UseQuotedIdentifier,
                QualifyColumns = UseTableWithFieldNames ?? (maps.Count > 1),
                ParameterizeLiterals = ParameterizeLiterals,
                NoLock = NoLock
            };

            return generator.Generate(root);
        }
    }

    class SqlWhereClause
    {
        public string SqlText { get; set; }
        public object[] Parameters { get; set; }
    }
}
