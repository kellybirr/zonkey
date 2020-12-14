using System;
using System.Data;
using System.Data.Common;
using System.Collections;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Threading.Tasks;

namespace Zonkey
{
    public partial class DataClassAdapter<T>
    {
        /// <summary>
        /// Fills the specified collection.
        /// </summary>
        /// <param name="collection">The collection.</param>
        /// <param name="start">The row index to start filling from</param>
        /// <param name="length">The number of rows to fill</param>
        /// <param name="filterExpression">the filter as a lambda expression</param>
        /// <returns></returns>
        public Task<int> FillRange(ICollection<T> collection, int start, int length, Expression<Func<T, bool>> filterExpression)
        {
            var parser = new ObjectModel.WhereExpressionParser<T>(DataMap, SqlDialect)
            {
                UseQuotedIdentifier = CommandBuilder.UseQuotedIdentifier,
                NoLock = this.NoLock
            };
            var result = parser.Parse(filterExpression);

            return FillRangeInternal(collection, start, length, result.SqlText, FillMethod.FilterText, result.Parameters);
        }

        /// <summary>
        /// Fills the specified collection.
        /// </summary>
        /// <param name="collection">The collection.</param>
        /// <param name="start">The row index to start filling from</param>
        /// <param name="length">The number of rows to fill</param>
        /// <param name="filter">The filter.</param>
        /// <param name="parameters">The parameters.</param>
        /// <returns>the number of objects added to the collection</returns>
        public Task<int> FillRange(ICollection<T> collection, int start, int length, string filter, params object[] parameters)
        {
            if (filter == null)
                throw new ArgumentNullException(nameof(filter));

            return FillRangeInternal(collection, start, length, filter, FillMethod.FilterText, parameters);
        }

        /// <summary>
        /// Fills the range.
        /// </summary>
        /// <param name="collection">The collection.</param>
        /// <param name="start">The start.</param>
        /// <param name="length">The length.</param>
        /// <param name="filters">The filters.</param>
        /// <returns></returns>
        public Task<int> FillRange(ICollection<T> collection, int start, int length, params SqlFilter[] filters)
        {
            if ((filters == null) || (filters.Length == 0))
                throw new ArgumentNullException(nameof(filters));

            return FillRangeInternal(collection, start, length, string.Empty, FillMethod.FilterArray, filters);
        }

        /// <summary>
        /// Fills the range internal.
        /// </summary>
        /// <param name="collection">The collection.</param>
        /// <param name="start">The start.</param>
        /// <param name="length">The length.</param>
        /// <param name="text">The text.</param>
        /// <param name="method">The method.</param>
        /// <param name="parameters">The parameters.</param>
        /// <returns></returns>
        private async Task<int> FillRangeInternal(ICollection<T> collection, int start, int length, string text, FillMethod method, IList parameters)
        {
            if (collection == null)
                throw new ArgumentNullException(nameof(collection));

            if (Connection == null)
                throw new InvalidOperationException("must set connection before calling Fill()");

            if (string.IsNullOrEmpty(_sortStr))
                throw new InvalidOperationException("must set OrderBy before using FillRange");

            if (! SqlDialect.SupportsLimit)
                throw new InvalidOperationException("this database dialect does not support using FillRange");

            DbCommand command;            
            switch (method)
            {
                case FillMethod.FilterText:
                    command = CommandBuilder.GetSelectRangeCommand(text, _sortStr, start, length);
                    DataManager.AddParamsToCommand(command, SqlDialect, parameters, ParameterPrefix);
                    break;
                case FillMethod.FilterArray:
                    command = CommandBuilder.GetSelectRangeCommand((SqlFilter[])parameters, _sortStr, start, length);
                    break;
                default:
                    throw new NotSupportedException("this fill method is not supported by FillRange");
            }

            using (DbDataReader reader = await ExecuteReaderInternal(command, CommandBehavior.SingleResult).ConfigureAwait(false))
                return await PopulateCollection(collection, reader);
        }


    }
}
