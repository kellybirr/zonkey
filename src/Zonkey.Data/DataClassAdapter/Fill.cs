using System;
using System.Linq.Expressions;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Threading.Tasks;

namespace Zonkey
{
    public partial class DataClassAdapter<T>
    {
        /// <summary>
        /// Fills all.
        /// </summary>
        /// <param name="collection">The collection.</param>
        /// <returns>the number of objects added to the collection</returns>
        public Task<int> FillAll(ICollection<T> collection)
        {
            return FillInternal(collection, null, FillMethod.Unfiltered, null);
        }

        /// <summary>
        /// Fills the specified collection.
        /// </summary>
        /// <param name="collection">The collection.</param>
        /// <param name="filterExpression">The filter as a lambda expression</param>
        /// <returns>the number of objects added to the collection</returns>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1006:DoNotNestGenericTypesInMemberSignatures")]
        public Task<int> Fill(ICollection<T> collection, Expression<Func<T, bool>> filterExpression)
        {
            var parser = new ObjectModel.WhereExpressionParser<T>(DataMap, SqlDialect)
            {
                UseQuotedIdentifier = CommandBuilder.UseQuotedIdentifier,
                NoLock = this.NoLock
            };
            var result = parser.Parse(filterExpression);

            return FillInternal(collection, result.SqlText, FillMethod.FilterText, result.Parameters);
        }

        /// <summary>
        /// Fills the specified collection.
        /// </summary>
        /// <param name="collection">The collection.</param>
        /// <param name="filters">The filters.</param>
        /// <returns>the number of objects added to the collection</returns>
        public Task<int> Fill(ICollection<T> collection, params SqlFilter[] filters)
        {
            if ((filters == null) || (filters.Length == 0))
                throw new ArgumentNullException(nameof(filters));

            return FillInternal(collection, string.Empty, FillMethod.FilterArray, filters);
        }

        /// <summary>
        /// Fills the specified collection.
        /// </summary>
        /// <param name="collection">The collection.</param>
        /// <param name="filter">The filter.</param>
        /// <returns>the number of objects added to the collection</returns>
        public Task<int> Fill(ICollection<T> collection, string filter)
        {
            if (string.IsNullOrEmpty(filter))
                throw new ArgumentNullException(nameof(filter));

            return FillInternal(collection, filter, FillMethod.FilterText, null);
        }

        /// <summary>
        /// Fills the specified collection.
        /// </summary>
        /// <param name="collection">The collection.</param>
        /// <param name="filter">The filter.</param>
        /// <param name="parameters">The parameters.</param>
        /// <returns>the number of objects added to the collection</returns>
        public Task<int> Fill(ICollection<T> collection, string filter, params object[] parameters)
        {
            if (string.IsNullOrEmpty(filter))
                throw new ArgumentNullException(nameof(filter));

            return FillInternal(collection, filter, FillMethod.FilterText, parameters);
        }

        /// <summary>
        /// Fills the specified collection by executing the provided command.
        /// </summary>
        /// <param name="collection">The collection to fill.</param>
        /// <param name="command">The command ready to be executed for the fill operation.</param>
        /// <returns>the number of objects added to the collection</returns>
        public async Task<int> Fill(ICollection<T> collection, DbCommand command)
        {
            if (collection == null) throw new ArgumentNullException(nameof(collection));
            if (command == null) throw new ArgumentNullException(nameof(command));

            if ((command.Connection == null) || (command.Connection.State != ConnectionState.Open))
            {
                if (Connection != null)
                    command.Connection = Connection;
                else
                    throw new InvalidOperationException("Either the command or the adapter must have an open connection.");
            }

            using (DbDataReader reader = await ExecuteReaderInternal(command, CommandBehavior.SingleResult).ConfigureAwait(false))
                return await PopulateCollection(collection, reader);
        }

        /// <summary>
        /// Fills the specified collection.
        /// </summary>
        /// <param name="collection">The collection to fill.</param>
        /// <param name="reader">An open DbDataReader at the position to begin filling.</param>
        /// <returns>the number of objects added to the collection</returns>
        public Task<int> Fill(ICollection<T> collection, DbDataReader reader)
        {
            if (collection == null) throw new ArgumentNullException(nameof(collection));
            if (reader == null) throw new ArgumentNullException(nameof(reader));
            if (reader.IsClosed) throw new ArgumentException("supplied reader was previously closed");

            return PopulateCollection(collection, reader);
        }

        /// <summary>
        /// Fills the with SP.
        /// </summary>
        /// <param name="collection">The collection.</param>
        /// <param name="storedProcName">Name of the stored procedure.</param>
        /// <param name="parameters">The parameters.</param>
        /// <returns>the number of objects added to the collection</returns>
        public Task<int> FillWithSP(ICollection<T> collection, string storedProcName, params object[] parameters)
        {
            if (storedProcName == null)
                throw new ArgumentNullException(nameof(storedProcName));

            return FillInternal(collection, storedProcName, FillMethod.StoredProcedure, parameters);
        }

        private async Task<int> FillInternal(ICollection<T> collection, string text, FillMethod method, IList parameters)
        {
            if (collection == null)
                throw new ArgumentNullException(nameof(collection));

            if (Connection == null)
                throw new InvalidOperationException("must set connection before calling Fill()");

            DbCommand command = PrepCommandForSelect(text, method, parameters);
            using (DbDataReader reader = await ExecuteReaderInternal(command, CommandBehavior.SingleResult).ConfigureAwait(false))
                return await PopulateCollection(collection, reader);
        }

        private DbCommand PrepCommandForSelect(string text, FillMethod method, IList parameters)
        {
            DbCommand command = null;
            switch (method)
            {
                case FillMethod.StoredProcedure:
                    command = Connection.CreateCommand();
                    command.CommandText = text;
                    command.CommandType = CommandType.StoredProcedure;
                    DataManager.AddParamsToCommand(command, SqlDialect, parameters, ParameterPrefix);

                    break;
                case FillMethod.Unfiltered:
                    command = CommandBuilder.GetSelectCommand(String.Empty, _sortStr);

                    break;
                case FillMethod.FilterText:
                    command = CommandBuilder.GetSelectCommand(text, _sortStr);
                    DataManager.AddParamsToCommand(command, SqlDialect, parameters, ParameterPrefix);

                    break;
                case FillMethod.FilterArray:
                    command = CommandBuilder.GetSelectCommand((SqlFilter[])parameters, _sortStr);

                    break;
            }

            return command;
        }
    }
}