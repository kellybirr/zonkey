using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Data.SqlClient;
using System.Text;
using Zonkey.Dialects;

namespace Zonkey.Ado
{
    /// <summary>
    /// Provides methods to create SQL commands to interact with a <see cref="System.Data.DataTable"/>.
    /// </summary>
    public class DataTableCommandBuilder
    {
        private readonly DataTable dataTable;

        /// <summary>
        /// Initializes a new instance of the <see cref="DataTableCommandBuilder"/> class.
        /// </summary>
        /// <param name="ds">The <see cref="System.Data.DataSet"/> containing the specified <see cref="System.Data.DataTable"/>.</param>
        /// <param name="tableName">Name of the DataTable.</param>
        /// <param name="connection">The database connection.</param>
        /// <param name="dialect">The type of SQL dialect.</param>
        public DataTableCommandBuilder(DataSet ds, string tableName, DbConnection connection, SqlDialect dialect)
        {
            if (ds == null) throw new ArgumentNullException(nameof(ds));
            if (String.IsNullOrEmpty(tableName)) throw new ArgumentNullException(nameof(tableName));
            if (connection == null) throw new ArgumentNullException(nameof(connection));

            _connection = connection;
            SqlDialect = dialect ?? SqlDialect.Create(connection);
            dataTable = ds.Tables[tableName];
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="DataTableCommandBuilder"/> class.
        /// </summary>
        /// <param name="ds">The <see cref="System.Data.DataSet"/> containing the specified <see cref="System.Data.DataTable"/>.</param>
        /// <param name="tableName">Name of the DataTable.</param>
        /// <param name="connection">The database connection.</param>
        public DataTableCommandBuilder(DataSet ds, string tableName, DbConnection connection)
            : this(ds, tableName, connection, null)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="DataTableCommandBuilder"/> class.
        /// </summary>
        /// <param name="dt">The <see cref="System.Data.DataTable"/>.</param>
        /// <param name="connection">The database connection.</param>
        /// <param name="dialect">The type of SQL dialect.</param>
        public DataTableCommandBuilder(DataTable dt, DbConnection connection, SqlDialect dialect)
        {
            if (dt == null) throw new ArgumentNullException(nameof(dt));
            if (connection == null) throw new ArgumentNullException(nameof(connection));

            dataTable = dt;
            _connection = connection;
            SqlDialect = dialect ?? SqlDialect.Create(connection);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="DataTableCommandBuilder"/> class.
        /// </summary>
        /// <param name="dt">The <see cref="System.Data.DataTable"/>.</param>
        /// <param name="connection">The database connection.</param>
        public DataTableCommandBuilder(DataTable dt, DbConnection connection)
            : this(dt, connection, null)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="DataTableCommandBuilder"/> class.
        /// </summary>
        /// <param name="dt">The <see cref="System.Data.DataTable"/>.</param>
        public DataTableCommandBuilder(DataTable dt)
        {
            if (dt == null) throw new ArgumentNullException(nameof(dt));
            dataTable = dt;
        }

        /// <summary>
        /// Database Connection to be used by the adapter
        /// </summary>
        public DbConnection Connection
        {
            get { return _connection; }
            set
            {
                _connection = value;
                SqlDialect = (_connection != null) ? SqlDialect.Create(_connection) : null;
            }
        }

        private DbConnection _connection;

        /// <summary>
        /// Gets or sets the SQL dialect.
        /// </summary>
        /// <value>The SQL dialect.</value>
        public SqlDialect SqlDialect { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether use quoted identifiers.
        /// </summary>
        /// <value><c>true</c> if using quoted identifiers; otherwise, <c>false</c>.</value>
        public bool? UseQuotedIdentifier { get; set; }

        /// <summary>
        /// Gets or sets the name of the sequence for an auto-increment column.
        /// </summary>
        /// <value>The name of the sequence.</value>
        public string SequenceName { get; set; }

        /// <summary>
        /// Gets the SQL SELECT command with no filters.
        /// </summary>
        public DbCommand SelectAllCommand
        {
            get
            {
                string commandText = string.Format("SELECT {0} FROM {1}", ColumnsString, TableName);
                return GetTextCommand(commandText);
            }
        }

        /// <summary>
        /// Gets the SQL SELECT command.
        /// </summary>
        /// <param name="filter">The SELECT statement filter (WHERE clause).</param>
        /// <returns>An instance of a <see cref="System.Data.Common.DbCommand"/> object</returns>
        public DbCommand GetSelectCommand(string filter)
        {
            return GetSelectCommand(filter, null);
        }

        /// <summary>
        /// Gets the SQL SELECT command.
        /// </summary>
        /// <param name="filter">The SELECT statement filter (WHERE clause).</param>
        /// <param name="sort">The SELECT statement sort (ORDER BY clause).</param>
        /// <returns>An instance of a <see cref="System.Data.Common.DbCommand"/> object</returns>
        public DbCommand GetSelectCommand(string filter, string sort)
        {
            var commandText = new StringBuilder();
            commandText.AppendFormat("SELECT {0} FROM {1}", ColumnsString, TableName);

            if (! string.IsNullOrEmpty(filter))
                commandText.AppendFormat(" WHERE {0}", filter);

            if ((! string.IsNullOrEmpty(sort)) && ((filter == null) || (filter.ToUpper().Contains("ORDER BY"))))
                commandText.AppendFormat(" ORDER BY {0}", sort);

            return GetTextCommand(commandText.ToString());
        }

        /// <summary>
        /// Gets the SQL SELECT command.
        /// </summary>
        /// <param name="filters">The SELECT statement <see cref="Zonkey.SqlFilter"/> filter array (WHERE clause).</param>
        /// <returns>An instance of a <see cref="System.Data.Common.DbCommand"/> object</returns>
        public DbCommand GetSelectCommand(SqlFilter[] filters)
        {
            var commandText = new StringBuilder();
            commandText.AppendFormat("SELECT {0} FROM {1}", ColumnsString, TableName);

            DbCommand command = GetTextCommand("");
            if ((filters != null) && (filters.Length > 0))
            {
                commandText.Append(" WHERE ");
                for (int i = 0; i < filters.Length; i++)
                {
                    if (i > 0) commandText.Append(" AND ");
                    commandText.Append(filters[i].ToString(SqlDialect, i));
                    filters[i].AddToCommandParams(command, SqlDialect, i);
                }
            }

            command.CommandText = commandText.ToString();
            return command;
        }

        /// <summary>
        /// Gets the SQL DELETE command based on the <see cref="System.Data.DataTable"/>'s primary key(s).
        /// </summary>
        public DbCommand DeleteCommand
        {
            get
            {
                DbCommand command = GetTextCommand("");
                var whereString = new StringBuilder();
                foreach (DataColumn column in dataTable.PrimaryKey)
                {
                    if (whereString.Length > 0)
                        whereString.Append(" AND ");

                    DbParameter whereParam = CreateWhereParam(command, column);

                    whereString.Append(SqlDialect.FormatFieldName(column.ColumnName, UseQuotedIdentifier));
                    whereString.Append(" = ");
                    whereString.Append(whereParam.ParameterName);

                    command.Parameters.Add(whereParam);
                }
                command.CommandText = string.Format("DELETE FROM {0} WHERE {1}", TableName, whereString);
                return command;
            }
        }

        /// <summary>
        /// Creates Insert command with support for Auto increment (Identity) tables
        /// </summary>
        public DbCommand InsertCommand
        {
            get
            {
                DbCommand command = GetTextCommand("");

                var intoString = new StringBuilder();
                var valuesString = new StringBuilder();
                List<DataColumn> identityColumns = IdentityKeyColumns;
                foreach (DataColumn column in dataTable.Columns)
                {
                    if (! (identityColumns.Contains(column) || column.ReadOnly))
                    {
                        // Not an auto-increment column
                        if (intoString.Length > 0)
                        {
                            intoString.Append(", ");
                            valuesString.Append(", ");
                        }

                        intoString.AppendFormat(SqlDialect.FormatFieldName(column.ColumnName, UseQuotedIdentifier));

                        DbParameter setParam = CreateSetParam(command, column);
                        valuesString.AppendFormat(setParam.ParameterName);
                        command.Parameters.Add(setParam);
                    }
                }
                string commandText = string.Format("INSERT INTO {0} ({1}) VALUES ({2});", TableName, intoString, valuesString);
                if (identityColumns.Count > 0)
                    commandText += string.Format(" SELECT {0} AS {1}", SqlDialect.FormatAutoIncrementSelect(SequenceName), SqlDialect.FormatFieldName(identityColumns[0].ColumnName, UseQuotedIdentifier));

                command.CommandText = commandText;
                return command;
            }
        }

        /// <summary>
        /// Creates Update command with optimistic concurrency support
        /// </summary>
        public DbCommand UpdateCommand
        {
            get
            {
                DbCommand command = GetTextCommand("");
                var setString = new StringBuilder();
                var setParamList = new List<DbParameter>();

                var whereString = new StringBuilder();
                var whereParamList = new List<DbParameter>();

                DataColumn[] primaryKeyColumns = dataTable.PrimaryKey;
                List<DataColumn> identityColumns = IdentityKeyColumns;

                foreach (DataColumn column in dataTable.Columns)
                {
                    if ((Array.IndexOf(primaryKeyColumns, column) != -1) /* || ( column.ColumnName.ToLower() == "rowversion" ) */)
                    {
                        // A primary key
                        if (whereString.Length > 0) whereString.Append(" AND ");
                        DbParameter whereParam = CreateWhereParam(command, column);
                        whereParamList.Add(whereParam);

                        whereString.Append(SqlDialect.FormatFieldName(column.ColumnName, UseQuotedIdentifier));
                        whereString.Append(" = ");
                        whereString.Append(whereParam.ParameterName);
                    }

                    if (! (identityColumns.Contains(column) || column.ReadOnly))
                    {
                        if (setString.Length > 0) setString.Append(", ");
                        DbParameter setParam = CreateSetParam(command, column);
                        setParamList.Add(setParam);

                        setString.Append(SqlDialect.FormatFieldName(column.ColumnName, UseQuotedIdentifier));
                        setString.Append(" = ");
                        setString.Append(setParam.ParameterName);
                    }
                }

                command.CommandText = String.Format("UPDATE {0} SET {1} WHERE {2};", TableName, setString, whereString);
                command.Parameters.AddRange(setParamList.ToArray());
                command.Parameters.AddRange(whereParamList.ToArray());
                return command;
            }
        }

        private List<DataColumn> IdentityKeyColumns
        {
            get
            {
                var identityKeys = new List<DataColumn>();
                foreach (DataColumn primaryKeyColumn in dataTable.PrimaryKey)
                {
                    if (primaryKeyColumn.AutoIncrement)
                        identityKeys.Add(primaryKeyColumn);
                }

                return identityKeys;
            }
        }

        /// <summary>
        /// Creates the set param.
        /// </summary>
        /// <param name="command">The command.</param>
        /// <param name="column">The column.</param>
        /// <returns></returns>
        private DbParameter CreateSetParam(DbCommand command, DataColumn column)
        {
            DbParameter param = command.CreateParameter();
            param.ParameterName = SqlDialect.FormatParameterName(column.ColumnName, command.CommandType);
            param.SourceColumn = column.ColumnName;

            if ( (param is SqlParameter) && (column.DataType == typeof(TimeSpan)) )   // hack M$ issue
                ((SqlParameter) param).SqlDbType = SqlDbType.Time;
            else
                param.DbType = DataManager.GetDbType(column.DataType);
            
            if (column.MaxLength > 0) param.Size = column.MaxLength;

            return param;
        }

        /// <summary>
        /// Creates the where param.
        /// </summary>
        /// <param name="command">The command.</param>
        /// <param name="column">The column.</param>
        /// <returns></returns>
        private DbParameter CreateWhereParam(DbCommand command, DataColumn column)
        {
            DbParameter param = command.CreateParameter();
            param.ParameterName = SqlDialect.FormatParameterName("old_" + column.ColumnName, command.CommandType);
            param.SourceColumn = column.ColumnName;
            param.SourceVersion = DataRowVersion.Original;

            if ((param is SqlParameter) && (column.DataType == typeof(TimeSpan)))   // hack M$ issue
                ((SqlParameter)param).SqlDbType = SqlDbType.Time;
            else
                param.DbType = DataManager.GetDbType(column.DataType);
            
            if (column.MaxLength > 0) param.Size = column.MaxLength;

            return param;
        }        

        private DbCommand GetTextCommand(string text)
        {
            DbCommand command = _connection.CreateCommand();
            command.CommandType = CommandType.Text;
            command.CommandText = text;
            command.Connection = _connection;
            return command;
        }

        /// <summary>
        /// Gets the name of the table.
        /// </summary>
        /// <value>The name of the table.</value>
        private string TableName
        {
            get { return SqlDialect.FormatTableName(dataTable.TableName, null, UseQuotedIdentifier); }
        }

        /// <summary>
        /// Gets the columns string.
        /// </summary>
        /// <value>The columns string.</value>
        private string ColumnsString
        {
            get
            {
                if (_columnsString == null)
                {
                    var sb = new StringBuilder();
                    foreach (DataColumn column in dataTable.Columns)
                    {
                        if (sb.Length > 0) sb.Append(", ");
                        sb.Append(SqlDialect.FormatFieldName(column.ColumnName, UseQuotedIdentifier));
                    }

                    _columnsString = sb.ToString();
                }

                return _columnsString;
            }
        }

        private string _columnsString;
    }
}