using System;
using System.Data;
using System.Data.Common;
using System.Linq.Expressions;
using System.Threading.Tasks;
using Zonkey.ObjectModel;

namespace Zonkey
{
    public partial class DataClassAdapter<T>
    {
        /// <summary>
        /// Gets the single item.
        /// </summary>
        /// <param name="filterExpression">The filter expression.</param>
        /// <returns>A value of type T.</returns>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1006:DoNotNestGenericTypesInMemberSignatures")]
        public Task<T> GetOne(Expression<Func<T, bool>> filterExpression)
        {
            if (Connection == null)
                throw new InvalidOperationException("must set connection before calling GetSingleItem()");

            var parser = new WhereExpressionParser<T>(DataMap, SqlDialect)
            {
                UseQuotedIdentifier = CommandBuilder.UseQuotedIdentifier,
                NoLock = this.NoLock
            };
            var result = parser.Parse(filterExpression);

            DbCommand command = PrepCommandForSelect(result.SqlText, FillMethod.FilterText, result.Parameters);
            SqlDialect.OptimizeSelectSingleCommand(command);

            return GetOne(command);
        }

        /// <summary>
        /// Gets the single item.
        /// </summary>
        /// <param name="filters">The filters.</param>
        /// <returns>A value of type T.</returns>
        public Task<T> GetOne(params SqlFilter[] filters)
        {
            if ((filters == null) || (filters.Length == 0))
                throw new ArgumentNullException(nameof(filters));

            if (Connection == null)
                throw new InvalidOperationException("must set connection before calling GetSingleItem()");

            DbCommand command = PrepCommandForSelect(string.Empty, FillMethod.FilterArray, filters);
            SqlDialect.OptimizeSelectSingleCommand(command);
            
            return GetOne(command);
        }

        /// <summary>
        /// Gets the single item.
        /// </summary>
        /// <param name="filter">The filter.</param>
        /// <returns>A value of type T.</returns>
        public Task<T> GetOne(string filter)
        {
            return GetOne(filter, null);
        }

        /// <summary>
        /// Gets the single item.
        /// </summary>
        /// <param name="filter">The filter.</param>
        /// <param name="parameters">The parameters.</param>
        /// <returns>A value of type T.</returns>
        public Task<T> GetOne(string filter, params object[] parameters)
        {
            if (string.IsNullOrEmpty(filter))
                throw new ArgumentNullException(nameof(filter));

            if (Connection == null)
                throw new InvalidOperationException("must set connection before calling GetSingleItem()");

            DbCommand command = PrepCommandForSelect(filter, FillMethod.FilterText, parameters);
            SqlDialect.OptimizeSelectSingleCommand(command);
            
            return GetOne(command);
        }

        /// <summary>
        /// Gets the single item.
        /// </summary>
        /// <param name="command">The command to execute.</param>
        /// <returns>A value of type T.</returns>
        public async Task<T> GetOne(DbCommand command) 
        {
            if (command == null) throw new ArgumentNullException(nameof(command));

            if ((command.Connection == null) || (command.Connection.State != ConnectionState.Open))
            {
                if (Connection != null)
                    command.Connection = Connection;
                else
                    throw new InvalidOperationException("Either the command or the adapter must have an open connection.");
            }

            using (DbDataReader reader = await ExecuteReaderInternal(command, CommandBehavior.SingleRow).ConfigureAwait(false))
            {
                if (await reader.ReadAsync())
                {
                    var item = CreateNewT();
                    if (item is ICustomFill itemCF)
                    {
                        itemCF.FillObject(reader);
                    }
                    else
                    {
                        PopulateSingleObject(item, reader, true);
                        (item as ISavable)?.CommitValues();
                    }

                    return item;
                }
                
                return default(T);
            }
        }
    }
}