using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Data.SqlClient;
using System.Reflection;

namespace Zonkey
{
    /// <summary>
    /// Provides methods and properties for defining a generic SQL parameter.
    /// </summary>
    public class GenericParameter
    {
        private DbType _dbType = DbType.Object;
        private ParameterDirection _direction = ParameterDirection.Input;
        private string _parameterName;
        private string _sourceColumn;
        private object _value;
        private int _size;

        private DbParameter _nativeParam;

        /// <summary>
        /// Initializes a new instance of the <see cref="GenericParameter"/> class.
        /// </summary>
        public GenericParameter()
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="GenericParameter"/> class.
        /// </summary>
        /// <param name="dbType">Type of the db.</param>
        /// <param name="value">The value.</param>
        public GenericParameter(DbType dbType, object value)
        {
            _dbType = dbType;
            _value = value;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="GenericParameter"/> class.
        /// </summary>
        /// <param name="parameterName">Name of the parameter.</param>
        /// <param name="value">The value.</param>
        [Obsolete("Please use the constructor that specifies parameter type", false)]
        public GenericParameter(string parameterName, object value)
        {
            _parameterName = parameterName;
            _value = value;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="GenericParameter"/> class.
        /// </summary>
        /// <param name="parameterName">Name of the parameter.</param>
        /// <param name="dbType">Type of the db.</param>
        /// <param name="value">The value.</param>
        public GenericParameter(string parameterName, DbType dbType, object value)
        {
            _parameterName = parameterName;
            _dbType = dbType;
            _value = value;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="GenericParameter"/> class.
        /// </summary>
        /// <param name="parameterName">Name of the parameter.</param>
        /// <param name="dbType">Type of the db.</param>
        /// <param name="direction">The parameter direction</param>
        [Obsolete("This constructor overload creates potential ambiguity, please use the newer version", true)]
        public GenericParameter(string parameterName, DbType dbType, ParameterDirection direction)
        {
            _parameterName = parameterName;
            _dbType = dbType;
            _direction = direction;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="GenericParameter"/> class.
        /// </summary>
        /// <param name="parameterName">Name of the parameter.</param>
        /// <param name="direction">The parameter direction</param>
        /// <param name="dbType">Type of the db.</param>
        /// <param name="value">The value.</param>
        public GenericParameter(string parameterName, ParameterDirection direction, DbType dbType, object value = null)
        {
            _parameterName = parameterName;
            _direction = direction;
            _dbType = dbType;
            _value = value;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="GenericParameter"/> class.
        /// </summary>
        /// <param name="entry">The dictionary entry with the key (name) and value.</param>
        [Obsolete("Please use the constructor that specifies parameter type", false)]
        public GenericParameter(KeyValuePair<string, object> entry)
        {
            _parameterName = entry.Key;
            _value = entry.Value;
        }

        /// <summary>
        /// Gets or sets the type of the db.
        /// </summary>
        /// <value>The type of the db.</value>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1706:ShortAcronymsShouldBeUppercase", MessageId = "Member")]
        public DbType DbType
        {
            get => _dbType;
            set => _dbType = value;
        }

        /// <summary>
        /// Gets or sets the direction.
        /// </summary>
        /// <value>The direction.</value>
        public ParameterDirection Direction
        {
            get => _direction;
            set => _direction = value;
        }

        /// <summary>
        /// Gets or sets the name of the parameter.
        /// </summary>
        /// <value>The name of the parameter.</value>
        public string ParameterName
        {
            get => _parameterName;
            set => _parameterName = value;
        }

        /// <summary>
        /// Gets or sets the size.
        /// </summary>
        /// <value>The size.</value>
        public int Size
        {
            get => _size;
            set => _size = value;
        }

        /// <summary>
        /// Gets or sets the source column.
        /// </summary>
        /// <value>The source column.</value>
        public string SourceColumn
        {
            get => _sourceColumn;
            set => _sourceColumn = value;
        }

        /// <summary>
        /// Gets or sets the value.
        /// </summary>
        /// <value>The value.</value>
        public object Value
        {
            get
            {
                // ReSharper disable once ArrangeAccessorOwnerBody
                return (_nativeParam != null) ? _nativeParam.Value : _value;
            }
            set
            {
                _value = value;

                if (_nativeParam != null)
                    _nativeParam.Value = value;
            }
        }

        /// <summary>
        /// Returns a <see cref="T:System.String"></see> that represents the current <see cref="T:System.Object"></see>.
        /// </summary>
        /// <returns>
        /// A <see cref="T:System.String"></see> that represents the current <see cref="T:System.Object"></see>.
        /// </returns>
        public override string ToString()
        {
            return _parameterName;
        }

        /// <summary>
        /// Serves as a hash function for a particular type.
        /// </summary>
        /// <returns>
        /// A hash code for the current <see cref="T:System.Object"></see>.
        /// </returns>
        public override int GetHashCode()
        {
            // ReSharper disable once NonReadonlyMemberInGetHashCode
            return _parameterName.GetHashCode();
        }

        /// <summary>
        /// Adds to command.
        /// </summary>
        /// <param name="command">The command.</param>
        /// <param name="dialect">The dialect.</param>
        public void AddToCommand(DbCommand command, Dialects.SqlDialect dialect)
        {
            // ReSharper disable once JoinNullCheckWithUsage
            if (command == null) throw new ArgumentNullException(nameof(command));
            if (dialect == null) throw new ArgumentNullException(nameof(dialect));

            _nativeParam = command.CreateParameter();

            if ((_parameterName != null) || (dialect.UseNamedParameters) || (command.CommandType == CommandType.StoredProcedure))
                _nativeParam.ParameterName = dialect.FormatParameterName(_parameterName, command.CommandType);

            _nativeParam.Direction = _direction;
            _nativeParam.SmartSetType(_dbType);

            if (_size != 0) _nativeParam.Size = _size;
            _nativeParam.SourceColumn = _sourceColumn;
            _nativeParam.Value = (_value ?? DBNull.Value);

            dialect.FixParameter(_nativeParam);
            command.Parameters.Add(_nativeParam);
        }
    }

    /// <summary>
    /// Provides methods and properties for defining a generic SQL parameter with a strongly typed Value.
    /// </summary>
    public class GenericParameter<T> : GenericParameter
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="GenericParameter"/> class.
        /// </summary>
        public GenericParameter()
        {
            DbType = DataManager.GetDbType(typeof (T));
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="GenericParameter"/> class.
        /// </summary>
        /// <param name="dbType">Type of the db.</param>
        /// <param name="value">The value.</param>
        public GenericParameter(DbType dbType, T value)
            : base(dbType, value)
        { }

        /// <summary>
        /// Initializes a new instance of the <see cref="GenericParameter"/> class.
        /// </summary>
        /// <param name="parameterName">Name of the parameter.</param>
        /// <param name="value">The value.</param>
        public GenericParameter(string parameterName, T value)
            : base(parameterName, DataManager.GetDbType(typeof(T)), value)
        {}

        /// <summary>
        /// Initializes a new instance of the <see cref="GenericParameter"/> class.
        /// </summary>
        /// <param name="parameterName">Name of the parameter.</param>
        /// <param name="dbType">Type of the db.</param>
        /// <param name="value">The value.</param>
        public GenericParameter(string parameterName, DbType dbType, T value)
            : base(parameterName, dbType, value)
        {}

        /// <summary>
        /// Initializes a new instance of the <see cref="GenericParameter"/> class.
        /// </summary>
        /// <param name="parameterName">Name of the parameter.</param>
        /// <param name="direction">The parameter direction</param>
        /// <param name="dbType">Type of the db.</param>
        /// <param name="value">The value.</param>
        public GenericParameter(string parameterName, ParameterDirection direction, DbType dbType, T value = default(T))
            : base(parameterName, direction, dbType, value)
        {}

        /// <summary>
        /// Gets or sets the value.
        /// </summary>
        /// <value>The value.</value>
        public new T Value
        {
            get => (T) base.Value;
            set => base.Value = value;
        }
    }

    static class DbParameterExtensions
    {
        internal static void SmartSetType(this DbParameter parameter, DbType dbType)
        {
            parameter.DbType = dbType;

            // This is a hack for a MS defect in the SqlParameter Class when using Time types.
            if ( (dbType == DbType.Time) && (parameter is SqlParameter sqlParameter) )
                sqlParameter.SqlDbType = SqlDbType.Time;
        }

#if (false)
        internal static void SmartSetType(this DbParameter parameter, DbType dbType)
        {
            parameter.DbType = dbType;

            if (dbType == DbType.Time)
                FixSqlServerTime(parameter);
        }

        /// <summary>
        /// This is a hack for a MS defect in the SqlParameter Class when using Time types.
        /// </summary>
        /// <param name="parameter"></param>
        private static void FixSqlServerTime(DbParameter parameter)
        {
            Type objType = parameter.GetType();
            if (objType.FullName == "System.Data.SqlClient.SqlParameter")
            {
                PropertyInfo propInfo = objType.GetTypeInfo().GetProperty("SqlDbType");
                propInfo?.SetValue(parameter, SqlDbType_Time);
            }
        }

        // I hate this, but i don't want to have a reference to System.Data.SqlClient in Zonkey Core
        private const int SqlDbType_Time = 32;
#endif
    }
}