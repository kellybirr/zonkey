using System;
using System.Collections;
using System.Data;
using System.Data.Common;
using System.Globalization;
using System.Reflection;
using System.Threading.Tasks;
using Zonkey.Dialects;

namespace Zonkey
{
    /// <summary>
    /// Provides methods and properties for constructing and executing ad-hoc queries against a database.
    /// </summary>
    public class DataManager
    {
        private DbConnection _connection;
        private Dialects.SqlDialect _dialect;
        private char _parameterPrefix = '$';

        /// <summary>
        /// Perferred constructor
        /// </summary>
        /// <param name="connection">Database Connection to be used by DataManager</param>
        public DataManager(DbConnection connection)
        {
            _commandTimeout = _defaultCommandTimeout;
            Connection = connection;
        }

        /// <summary>
        /// Default Constructor
        /// </summary>
        public DataManager()
        {
            _commandTimeout = _defaultCommandTimeout;
        }

        /// <summary>
        /// Database Connection to be used by DataManager
        /// </summary>
        /// <value>The connection.</value>
        public DbConnection Connection
        {
            get { return _connection; }
            set
            {
                _connection = value;
                _dialect = Dialects.SqlDialect.Create(value);
            }
        }

        /// <summary>
        /// If set, overrides the default timout for command execution.
        /// </summary>
        /// <value>The command timeout.</value>
        public int? CommandTimeout
        {
            get { return _commandTimeout; }
            set { _commandTimeout = value; }
        }

        private int? _commandTimeout;

        /// <summary>
        /// If set, overrides the default timout for command execution on newly created DataManager objects
        /// </summary>
        /// <value>The default command timeout.</value>
        public static int? DefaultCommandTimeout
        {
            get { return _defaultCommandTimeout; }
            set { _defaultCommandTimeout = value; }
        }

        private static int? _defaultCommandTimeout;

        /// <summary>
        /// Gets or sets the SQL dialect.
        /// </summary>
        /// <value>The SQL dialect.</value>
        public Dialects.SqlDialect SqlDialect
        {
            get { return _dialect; }
            set { _dialect = value; }
        }

        /// <summary>
        /// Prefix used for command parameters
        /// Format: FilterValueA = $0 AND FilterValueB = $1
        /// where '$' is the default ParemeterPrefix
        /// </summary>
        /// <value>The parameter prefix.</value>
        public char ParameterPrefix
        {
            get { return _parameterPrefix; }
            set { _parameterPrefix = value; }
        }

        /// <summary>
        /// Create and initializes a new DbCommand object from the SP name and parameter list
        /// </summary>
        /// <param name="storedProcName">Name of the Stored Procedure to be called by the command</param>
        /// <param name="parameters">Parameter list required by the stored procedure</param>
        /// <returns>A <see cref="System.Data.Common.DbCommand"/> object with the command to execute</returns>
        public DbCommand GetCommandFromSP(string storedProcName, params object[] parameters)
        {
            if (storedProcName == null)
                throw new ArgumentNullException(nameof(storedProcName));

            if (_connection == null)
                throw new InvalidOperationException("must set connection before calling");

            DbCommand command = _connection.CreateCommand();
            command.CommandText = storedProcName;
            command.CommandType = CommandType.StoredProcedure;

            if (_commandTimeout.HasValue)
                command.CommandTimeout = _commandTimeout.Value;

            DbTransactionRegistry.SetCommandTransaction(command);

            if (parameters != null)
                AddParamsToCommand(command, _dialect, parameters, _parameterPrefix);

            return command;
        }

        /// <summary>
        /// Allow ad-hoc queries or stored procedures to return an open data reader.
        /// </summary>
        /// <param name="sql">The SQL code or stored procedure name to execute</param>
        /// <param name="commandType">Type of command to execute</param>
        /// <param name="parameters">Parameter list requied by the statement or stored procedure</param>
        /// <returns>An open data reader</returns>
        public Task<DbDataReader> GetDataReader(string sql, CommandType commandType, params object[] parameters)
        {
            if (sql == null)
                throw new ArgumentNullException(nameof(sql));

            if (_connection == null)
                throw new InvalidOperationException("must set connection before calling");

            DbCommand command = _connection.CreateCommand();
            command.CommandText = sql;
            command.CommandType = commandType;

            DbTransactionRegistry.SetCommandTransaction(command);

            if (_commandTimeout.HasValue)
                command.CommandTimeout = _commandTimeout.Value;

            if (parameters != null)
                AddParamsToCommand(command, _dialect, parameters, _parameterPrefix);

            return command.ExecuteReaderAsync();
        }


        /// <summary>
        /// Calls sql command that doesn't return anything
        /// </summary>
        /// <param name="sql">The SQL.</param>
        /// <returns>The number of rows affected.</returns>
        public Task<int> ExecuteNonQuery(string sql)
        {
            return ExecuteNonQuery(sql, false, null);
        }

        /// <summary>
        /// Calls sql command that doesn't return anything
        /// </summary>
        /// <param name="sql">The SQL.</param>
        /// <param name="parameters">The parameters.</param>
        /// <returns>The number of rows affected.</returns>
        public Task<int> ExecuteNonQuery(string sql, params object[] parameters)
        {
            return ExecuteNonQuery(sql, false, parameters);
        }

        /// <summary>
        /// Calls sql command that doesn't return anything
        /// </summary>
        /// <param name="sql">The SQL.</param>
        /// <param name="isProc">if set to <c>true</c> [is proc].</param>
        /// <param name="parameters">The parameters.</param>
        /// <returns>The number of rows affected.</returns>
        public Task<int> ExecuteNonQuery(string sql, bool isProc, params object[] parameters)
        {
            if (sql == null)
                throw new ArgumentNullException(nameof(sql));

            using (DbCommand command = _connection.CreateCommand())
            {
                command.CommandText = sql;
                command.CommandType = (isProc) ? CommandType.StoredProcedure : CommandType.Text;

                if (_commandTimeout.HasValue)
                    command.CommandTimeout = _commandTimeout.Value;

                if (parameters != null)
                    AddParamsToCommand(command, _dialect, parameters, _parameterPrefix);

                return ExecuteNonQuery(command);
            }
        }

        /// <summary>
        /// Calls sql command that doesn't return anything
        /// </summary>
        /// <param name="command">The command.</param>
        /// <returns>The number of rows affected.</returns>
        public Task<int> ExecuteNonQuery(DbCommand command)
        {
            if (command == null)
                throw new ArgumentNullException(nameof(command));

            if (_connection == null)
                throw new InvalidOperationException("must set connection before calling");

            command.Connection = _connection;
            DbTransactionRegistry.SetCommandTransaction(command);

            return command.ExecuteNonQueryAsync();
        }

        /// <summary>
        /// Calls sql command that returns single scalar value
        /// </summary>
        /// <param name="sql">The SQL.</param>
        /// <returns>The first column of the first row in the result set.</returns>
        public Task<object> ExecuteScalar(string sql)
        {
            return ExecuteScalar(sql, false, null);
        }

        /// <summary>
        /// Calls sql command that returns single scalar value
        /// </summary>
        /// <param name="sql">The SQL.</param>
        /// <param name="parameters">The parameters.</param>
        /// <returns>The first column of the first row in the result set.</returns>
        public Task<object> ExecuteScalar(string sql, params object[] parameters)
        {
            return ExecuteScalar(sql, false, parameters);
        }

        /// <summary>
        /// Calls sql command that returns single scalar value
        /// </summary>
        /// <param name="sql">The SQL.</param>
        /// <param name="isProc">if set to <c>true</c> [is proc].</param>
        /// <param name="parameters">The parameters.</param>
        /// <returns>The first column of the first row in the result set.</returns>
        public Task<object> ExecuteScalar(string sql, bool isProc, params object[] parameters)
        {
            if (sql == null)
                throw new ArgumentNullException(nameof(sql));

            using (DbCommand command = _connection.CreateCommand())
            {
                command.CommandText = sql;
                command.CommandType = (isProc) ? CommandType.StoredProcedure : CommandType.Text;

                if (_commandTimeout.HasValue)
                    command.CommandTimeout = _commandTimeout.Value;

                if (parameters != null)
                    AddParamsToCommand(command, _dialect, parameters, _parameterPrefix);

                return ExecuteScalar(command);
            }
        }

        /// <summary>
        /// Calls sql command that returns single scalar value
        /// </summary>
        /// <param name="command">The command.</param>
        /// <returns>The first column of the first row in the result set.</returns>
        public Task<object> ExecuteScalar(DbCommand command)
        {
            if (command == null)
                throw new ArgumentNullException(nameof(command));

            if (_connection == null)
                throw new InvalidOperationException("must set connection before calling");

            command.Connection = _connection;
            DbTransactionRegistry.SetCommandTransaction(command);

            return command.ExecuteScalarAsync();
        }

        /// <summary>
        /// Adds the params to command.
        /// </summary>
        /// <param name="command">The command.</param>
        /// <param name="parameters">The parameters.</param>
        public void AddParamsToCommand(DbCommand command, IList parameters)
        {
            AddParamsToCommand(command, _dialect, parameters);
        }

        /// <summary>
        /// Adds the params to command.
        /// </summary>
        /// <param name="command">The command.</param>
        /// <param name="dialect">The dialect.</param>
        /// <param name="parameters">The parameters.</param>
        public static void AddParamsToCommand(DbCommand command, SqlDialect dialect, IList parameters)
        {
            AddParamsToCommand(command, dialect, parameters, '$');
        }

        /// <summary>
        /// Adds the params to command.
        /// </summary>
        /// <param name="command">The command.</param>
        /// <param name="parameters">The parameters.</param>
        /// <param name="placeholderPrefix">The placeholder prefix.</param>
        public void AddParamsToCommand(DbCommand command, IList parameters, char placeholderPrefix)
        {
            AddParamsToCommand(command, _dialect, parameters, placeholderPrefix);
        }

        /// <summary>
        /// Adds the params to command.
        /// </summary>
        /// <param name="command">The command.</param>
        /// <param name="dialect">The dialect.</param>
        /// <param name="parameters">The parameters.</param>
        /// <param name="placeholderPrefix">The placeholder prefix.</param>
        public static void AddParamsToCommand(DbCommand command, SqlDialect dialect, IList parameters, char placeholderPrefix)
        {
            if (command == null)
                throw new ArgumentNullException(nameof(command));

            if (parameters == null) return;

            for (int i = 0; i < parameters.Count; i++)
            {
                if (parameters[i] is DbParameter)
                {
                    command.Parameters.Add(parameters[i]);
                }
                else if (parameters[i] is GenericParameter)
                {
                    var genericParam = ((GenericParameter)parameters[i]);

                    if ((genericParam.ParameterName == null) && (command.CommandType == CommandType.Text))
                        AddIndexedParameter(command, dialect, placeholderPrefix, i, genericParam.Value);
                    else
                        genericParam.AddToCommand(command, dialect);
                }
                else if (command.CommandType == CommandType.Text)
                {
                    AddIndexedParameter(command, dialect, placeholderPrefix, i, parameters[i]);
                }
                else
                {
                    DbParameter newParm = command.CreateParameter();
                    newParm.Value = (parameters[i] ?? DBNull.Value);

                    dialect.FixParameter(newParm);
                    command.Parameters.Add(newParm);
                }
            }
        }

        /// <summary>
        /// Adds the indexed parameter.
        /// </summary>
        /// <param name="command">The command.</param>
        /// <param name="dialect">The dialect.</param>
        /// <param name="placeholderPrefix">The placeholder prefix.</param>
        /// <param name="index">The index.</param>
        /// <param name="value">The value.</param>
        public static void AddIndexedParameter(DbCommand command, SqlDialect dialect, char placeholderPrefix, int index, object value)
        {
            if (command == null) throw new ArgumentNullException(nameof(command));
            if (dialect == null) throw new ArgumentNullException(nameof(dialect));

            DbParameter newParm = command.CreateParameter();
            newParm.Value = (value ?? DBNull.Value);
            newParm.ParameterName = dialect.FormatParameterName(index, command.CommandType);

            string sParmHolder = string.Concat(placeholderPrefix, index);
            command.CommandText = command.CommandText.Replace(sParmHolder, newParm.ParameterName);

            dialect.FixParameter(newParm);
            command.Parameters.Add(newParm);
        }

        ///<summary>
        /// Gets the db type for the native type
        ///</summary>
        ///<param name="type"></param>
        ///<returns></returns>
        public static DbType GetDbType(Type type)
        {
            TypeInfo info = type.GetTypeInfo();
            if ( (type == typeof(Byte)) || (type == typeof(Byte?)) ) 
                return DbType.Byte;
            if ( (type == typeof(Int16)) || (type == typeof(Int16?)) ) 
                return DbType.Int16;
            if ( (type == typeof(Int32)) || (type == typeof(Int32?)) || (info.IsEnum) ) 
                return DbType.Int32;
            if ( (type == typeof(Int64)) || (type == typeof(Int64?)) ) 
                return DbType.Int64;
            if ( (type == typeof(Single)) || (type == typeof(Single?)) ) 
                return DbType.Single;
            if ( (type == typeof(Double)) || (type == typeof(Double?)) ) 
                return DbType.Double;
            if ( (type == typeof(Decimal)) || (type == typeof(Decimal?)) ) 
                return DbType.Decimal;
            if ( (type == typeof(DateTime)) || (type == typeof(DateTime?)) ) 
                return DbType.DateTime;
            if ( (type == typeof(Boolean)) || (type == typeof(Boolean?)) ) 
                return DbType.Boolean;
            if ( (type == typeof(Guid)) || (type == typeof(Guid?)) ) 
                return DbType.Guid;            
            if (type == typeof(Byte[])) 
                return DbType.Binary;

            return DbType.String;
        }		
    }
}