using System;
using System.Data;
using System.Data.Common;
using System.Linq;
using System.Threading.Tasks;

namespace Zonkey.Ado
{
    /// <summary>
    /// An fairly close analog of an ADO Recordset object
    /// </summary>
    public class Recordset : IDisposable
    {
        private DataTable _dt;
        private QueryInfo _lastQuery;

        /// <summary>
        /// Initializes a new instance of the <see cref="Recordset"/> class.
        /// </summary>
        /// <param name="connection">The connection.</param>
        public Recordset(DbConnection connection)
        {
            Position = -1;
            Connection = connection;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Recordset"/> class.
        /// </summary>
        /// <param name="connectionName">Name of the connection.</param>
        public Recordset(string connectionName) : 
            this(DbConnectionFactory.CreateConnection(connectionName))
        { }

        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
        /// </summary>
        public void Dispose()
        {
            Connection?.Close();
            _dt?.Dispose();

            _dt = null;
            Connection = null;

            Position = -1;
        }

        /// <summary>
        /// Closes this instance.
        /// </summary>
        public void Close()
        {
            Dispose();
        }

        /// <summary>
        /// Opens the specified SQL.
        /// </summary>
        /// <param name="sql">The SQL.</param>
        /// <returns></returns>
        public Task<int> Open(string sql)
        {
            return Open(sql, CommandType.Text, null);
        }

        /// <summary>
        /// Opens the specified SQL.
        /// </summary>
        /// <param name="sql">The SQL.</param>
        /// <param name="parameters">The parameters.</param>
        /// <returns></returns>
        public Task<int> Open(string sql, params object[] parameters)
        {
            return Open(sql, CommandType.Text, parameters);
        }

        /// <summary>
        /// Opens the specified SQL.
        /// </summary>
        /// <param name="sql">The SQL.</param>
        /// <param name="commandType">Type of the command.</param>
        /// <param name="parameters">The parameters.</param>
        /// <returns></returns>
        public async Task<int> Open(string sql, CommandType commandType, params object[] parameters)
        {
            // check valid state
            if (Connection == null)
                throw new InvalidOperationException("Must provide a Connection before opening Recordset");

            // open connection if necessary
            if (Connection.State != ConnectionState.Open)
                await Connection.OpenAsync().ConfigureAwait(false);

            // save last query
            _lastQuery = new QueryInfo {Sql = sql, CommandType = commandType, Parameters = parameters};

            // query for data
            _dt = new DataTable();
            var dm = new AdoDataManager(Connection);
            await dm.FillDataTable(_dt, sql, commandType, parameters).ConfigureAwait(false);

            // reset to first position
            Position = 0;

            // return record count
            return RecordCount;
        }

        /// <summary>
        /// Re-queries this instance.
        /// </summary>
        /// <returns></returns>
        public Task<int> Requery()
        {
            if (_lastQuery == null)
                throw new InvalidOperationException("Must `Open' before you can `Requery`.");

            return Open(_lastQuery.Sql, _lastQuery.CommandType, _lastQuery.Parameters);
        }

        /// <summary>
        /// Gets or sets the column value with the specified name.
        /// </summary>
        /// <value></value>
        public object this[string fieldName]
        {
            get
            {
                if (BOF || EOF)
                    throw new InvalidOperationException("Either BOF or EOF is true.");

                return _dt.Rows[Position][fieldName];
            }
            set
            {
                if (BOF || EOF)
                    throw new InvalidOperationException("Either BOF or EOF is true.");

                _dt.Rows[Position][fieldName] = value;
            }
        }

        /// <summary>
        /// Gets or sets the column value with the specified ordinal position.
        /// </summary>
        /// <value></value>
        public object this[int index]
        {
            get
            {
                if (BOF || EOF)
                    throw new InvalidOperationException("Either BOF or EOF is true.");

                return _dt.Rows[Position][index];
            }
            set
            {
                if (BOF || EOF)
                    throw new InvalidOperationException("Either BOF or EOF is true.");

                _dt.Rows[Position][index] = value;
            }
        }

        /// <summary>
        /// Gets or sets the connection.
        /// </summary>
        /// <value>The connection.</value>
        public DbConnection Connection { get; set; }

        /// <summary>
        /// Gets or sets the current record position.
        /// </summary>
        /// <value>The position.</value>
        public int Position { get; set; }

        /// <summary>
        /// Gets the record count.
        /// </summary>
        /// <value>The record count.</value>
        public int RecordCount
        {
            get { return _dt?.Rows.Count ?? -1; }
        }

        /// <summary>
        /// Gets a value indicating whether this <see cref="Recordset"/> is at the beginning of the data.
        /// </summary>
        /// <value><c>true</c> if BOF; otherwise, <c>false</c>.</value>
        public bool BOF
        {
            get { return ((_dt == null) || (_dt.Rows.Count == 0) || (Position < 0)); }
        }

        /// <summary>
        /// Gets a value indicating whether this <see cref="Recordset"/> is at the end of the data.
        /// </summary>
        /// <value><c>true</c> if EOF; otherwise, <c>false</c>.</value>
        public bool EOF
        {
            get { return ((_dt == null) || (_dt.Rows.Count == 0) || (Position >= _dt.Rows.Count)); }
        }

        /// <summary>
        /// Gets the fields.
        /// </summary>
        /// <value>The fields.</value>
        public DataColumnCollection Fields
        {
            get
            {
                if (_dt == null)
                    throw new InvalidOperationException("Must `Open` Before getting fields.");

                return _dt.Columns;
            }
        }

        /// <summary>
        /// Moves the record pointer by the specified offset.
        /// </summary>
        /// <param name="offset">The offset.</param>
        /// <returns></returns>
        public bool Move(int offset)
        {
            if (_dt == null)
                throw new InvalidOperationException("Must `Open` Before moving.");

            Position += offset;
            return (!(BOF || EOF));
        }

        /// <summary>
        /// Moves to the first record.
        /// </summary>
        /// <returns></returns>
        public bool MoveFirst()
        {
            if (_dt == null)
                throw new InvalidOperationException("Must `Open` Before moving.");

            Position = 0;
            return (!(BOF || EOF));
        }

        /// <summary>
        /// Moves to the last record.
        /// </summary>
        /// <returns></returns>
        public bool MoveLast()
        {
            if (_dt == null)
                throw new InvalidOperationException("Must `Open` Before moving.");

            Position = _dt.Rows.Count;
            return (!(BOF || EOF));
        }

        /// <summary>
        /// Moves to the previous record.
        /// </summary>
        /// <returns></returns>
        public bool MovePrevious()
        {
            if (_dt == null)
                throw new InvalidOperationException("Must `Open` Before moving.");

            Position--;
            return (!(BOF || EOF));
        }

        /// <summary>
        /// Moves to the next record.
        /// </summary>
        /// <returns></returns>
        public bool MoveNext()
        {
            if (_dt == null)
                throw new InvalidOperationException("Must `Open` Before moving.");

            Position++;
            return (!(BOF || EOF));
        }

        /// <summary>
        /// Finds the specified filter expression.
        /// </summary>
        /// <param name="filterExpression">The filter expression.</param>
        /// <returns></returns>
        public bool FindNext(string filterExpression)
        {
            if (_dt == null)
                throw new InvalidOperationException("Must `Open` Before finding.");

            var rows = _dt.Select(filterExpression);
            if (rows.Length == 0) return false;

            for (int n = Position; n < _dt.Rows.Count; n++ )
            {
                // ReSharper disable once LoopCanBeConvertedToQuery
                foreach (var dataRow in rows)
                {
                    if (_dt.Rows[n] == dataRow)
                    {
                        Position = n;
                        return true;
                    }
                }
            }

            return false;
        }

        /// <summary>
        /// Creates a new Row with the table schema
        /// </summary>
        public DataRow NewRow()
        {
            if (_dt == null)
                throw new InvalidOperationException("Must `Open` Before creating new rows.");

            return _dt.NewRow();
        }

        /// <summary>
        /// Adds a new row into to the table
        /// </summary>
        public int AddRow(DataRow row)
        {
            if (row == null)
                throw new ArgumentNullException(nameof(row));

            if (_dt == null)
                throw new InvalidOperationException("Must `Open` Before inserting.");

            _dt.Rows.Add(row);
            Position = _dt.Rows.IndexOf(row);

            return Position;
        }


        /// <summary>
        /// Deletes the current record.
        /// </summary>
        public void Delete()
        {
            if (BOF || EOF)
                throw new InvalidOperationException("Either BOF or EOF is true.");

            _dt.Rows[Position].Delete();
        }

        /// <summary>
        /// Updates the batch.
        /// </summary>
        /// <returns></returns>
        public Task<int> UpdateBatch()
        {
            // check valid state
            if ((Connection == null) || (Connection.State != ConnectionState.Open))
                throw new InvalidOperationException("Must provide an open Connection before updating Recordset");

            if (_dt == null)
                throw new InvalidOperationException("Must `Open` Before updating.");

            if (_dt.PrimaryKey.Length == 0)
                throw new InvalidOperationException("Must call `InitUpdate' to set table and primary key before updating");

            var dta = new DataTableAdapter(Connection)
            {
                // force use of quoted identifiers?
                CreateCommandBuilder = (t, c) => new DataTableCommandBuilder(t, c)
                {
                    UseQuotedIdentifier = this.UseQuotedIdentifier
                }
            };

            // do the update
            int rows = dta.SaveChanges(_dt);

            return Task.FromResult( rows );
        }

        public bool UseQuotedIdentifier { get; set; } = true;

        /// <summary>
        /// Initialized the Recordset for updating the table.
        /// </summary>
        /// <param name="tableName">Name of the table.</param>
        /// <param name="primaryKey">The primary key.</param>
        public void InitUpdate(string tableName, params string[] primaryKey)
        {
            if (_dt == null)
                throw new InvalidOperationException("Must `Open` Before updating.");

            _dt.TableName = tableName;
            _dt.PrimaryKey = primaryKey.Select(s => _dt.Columns[s]).ToArray();
        }

        private class QueryInfo
        {
            public string Sql;
            public CommandType CommandType;
            public object[] Parameters;
        }
    }
}
