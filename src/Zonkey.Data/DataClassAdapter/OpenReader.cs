using System;
using System.Collections;
using System.Data;
using System.Data.Common;
using System.Linq.Expressions;
using System.Threading.Tasks;
using Zonkey.ObjectModel;

namespace Zonkey
{
    public partial class DataClassAdapter<T>
    {
        public Task<DataClassReader<T>> OpenReader(Expression<Func<T, bool>> filterExpression)
        {
            var parser = new WhereExpressionParser<T>(DataMap, SqlDialect) { UseQuotedIdentifier = CommandBuilder.UseQuotedIdentifier };
            var result = parser.Parse(filterExpression);

            return OpenReaderInternal(result.SqlText, FillMethod.FilterText, result.Parameters);
        }

        public Task<DataClassReader<T>> OpenReader(params SqlFilter[] filters)
        {
            if ((filters == null) || (filters.Length == 0))
                throw new ArgumentNullException(nameof(filters));

            return OpenReaderInternal(string.Empty, FillMethod.FilterArray, filters);
        }

        public Task<DataClassReader<T>> OpenReader(string filter)
        {
            if (string.IsNullOrEmpty(filter))
                throw new ArgumentNullException(nameof(filter));

            return OpenReaderInternal(filter, FillMethod.FilterText, null);
        }

        public Task<DataClassReader<T>> OpenReader(string filter, params object[] parameters)
        {
            if (string.IsNullOrEmpty(filter))
                throw new ArgumentNullException(nameof(filter));

            return OpenReaderInternal(filter, FillMethod.FilterText, parameters);
        }

        public async Task<DataClassReader<T>> OpenReader(DbCommand command)
        {
            if (command == null) throw new ArgumentNullException(nameof(command));

            if ((command.Connection == null) || (command.Connection.State != ConnectionState.Open))
            {
                if (Connection != null)
                    command.Connection = Connection;
                else
                    throw new InvalidOperationException("Either the command or the adapter must have an open connection.");
            }

            DbDataReader reader = await ExecuteReaderInternal(command, CommandBehavior.SingleResult).ConfigureAwait(false);

            return new DataClassReader<T>(reader, DataMap) { ObjectFactory = ObjectFactory };
        }

        private async Task<DataClassReader<T>> OpenReaderInternal(string text, FillMethod method, IList parameters)
        {
            if (Connection == null)
                throw new InvalidOperationException("must set connection before calling OpenReader()");

            DbCommand command = PrepCommandForSelect(text, method, parameters);
            DbDataReader reader = await ExecuteReaderInternal(command, CommandBehavior.SingleResult).ConfigureAwait(false);
            
            return new DataClassReader<T>(reader, DataMap) { ObjectFactory = ObjectFactory };
        }

        public DataClassReader<T> CreateReader(DbDataReader reader)
        {
            if (reader == null) throw new ArgumentNullException(nameof(reader));
            if (reader.IsClosed) throw new ArgumentException("supplied reader was previously closed");

            return new DataClassReader<T>(reader, DataMap) { ObjectFactory = ObjectFactory };
        }
    }
}