using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Linq.Expressions;
using System.Reflection;
using System.Threading.Tasks;
using Zonkey.ObjectModel;

namespace Zonkey
{
    public partial class DataClassAdapter<T>
    {
        /// <summary>
        /// Updates the rows in the database matching the where expression
        /// </summary>
        /// <param name="setClause">The name/value pairs to set.</param>
        /// <param name="whereExpression">The where expression for the update statement.</param>
        /// <returns></returns>
        public Task<int> UpdateRows(object setClause, Expression<Func<T, bool>> whereExpression)
        {
            var collection = new Dictionary<string, object>();

            PropertyInfo[] properties = setClause.GetType().GetTypeInfo().GetProperties(BindingFlags.Public | BindingFlags.Instance);			
            foreach (PropertyInfo pi in properties)
                collection[pi.Name] = pi.GetValue(setClause, null);

            return UpdateRows(collection, whereExpression);
        }

        /// <summary>
        /// Updates the rows in the database matching the where expression
        /// </summary>
        /// <param name="setClause">The name/value pairs to set.</param>
        /// <param name="whereExpression">The where expression for the update statement.</param>
        /// <returns></returns>
        public Task<int> UpdateRows(IDictionary<string, object> setClause, Expression<Func<T, bool>> whereExpression)
        {
            if (Connection == null) throw new InvalidOperationException("must set connection before calling UpdateRows()");
            if (setClause == null) throw new ArgumentNullException(nameof(setClause));
            if (setClause.Count == 0) throw new ArgumentException("Empty Set Clause");

            var parser = new WhereExpressionParser<T>(DataMap, SqlDialect)
                            {
                                UseQuotedIdentifier = CommandBuilder.UseQuotedIdentifier
                            };
            var parsedWhere = parser.Parse(whereExpression);

            DbCommand command = CommandBuilder.GetUpdateRowsCommand(setClause, parsedWhere.SqlText);
            DataManager.AddParamsToCommand(command, SqlDialect, parsedWhere.Parameters, ParameterPrefix);

            return ExecuteNonQueryInternal(command);
        }
    }
}