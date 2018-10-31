using System;
using System.Data;
using System.Data.Common;
using System.Reflection;
using System.Text;
using Zonkey.ObjectModel.Projection;

namespace Zonkey.ObjectModel
{
    /// <summary>
    /// Provides methods to create SQL commands from a <see cref="Zonkey.ObjectModel.DataClass"/>.
    /// </summary>
    public partial class DataClassCommandBuilder
    {
        private readonly IProjectionMapBuilder _projectionBuilder;
        private readonly IProjectionParser _projectionParser;
        private readonly Type _dataObjectType;
        private readonly TypeInfo _dataObjectInfo;
        private readonly DataMap _dataMap;

        private readonly DbConnection _connection;
        private readonly Dialects.SqlDialect _dialect;

        /// <summary>
        /// Initializes a new instance of the <see cref="DataClassCommandBuilder"/> class.
        /// </summary>
        /// <param name="type">The type of object.</param>
        /// <param name="map">The data map</param>
        /// <param name="connection">The connection.</param>
        public DataClassCommandBuilder(Type type, DataMap map, DbConnection connection) : this(type, map, connection, null)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="DataClassCommandBuilder"/> class.
        /// </summary>
        /// <param name="type">The type of object.</param>
        /// <param name="map">The data map</param>
        /// <param name="connection">The connection.</param>
        /// <param name="dialect">The sql dialect.</param>
        public DataClassCommandBuilder(Type type, DataMap map, DbConnection connection, Dialects.SqlDialect dialect)
        {
            if (type == null) throw new ArgumentNullException(nameof(type));
            if (connection == null) throw new ArgumentNullException(nameof(connection));
            if (map == null) throw new ArgumentNullException(nameof(map));

            if (map.DataItem == null)
                throw new ArgumentException("Invalid Data Map: Missing DataItem");

            _connection = connection;
            _dialect = (dialect ?? Dialects.SqlDialect.Create(connection));

            _dataObjectType = type;
            _dataObjectInfo = type.GetTypeInfo();
            _dataMap = map;

            _projectionBuilder = new ProjectionMapBuilder();
            _projectionParser = new ProjectionParser(_dialect);
        }

        /// <summary>
        /// Gets or sets a value indicating whether [no lock].
        /// </summary>
        /// <value><c>true</c> if [no lock]; otherwise, <c>false</c>.</value>
        public bool NoLock { get; set; }

        /// <summary>
        /// Gets or Sets the change context data used by SQL
        /// </summary>
        public object ChangeTrackingContext { get; set; }

        /// <summary>
        /// Creates the set param.
        /// </summary>
        /// <param name="command">The command.</param>
        /// <param name="field">The field.</param>
        /// <returns></returns>
        private DbParameter CreateSetParam(DbCommand command, IDataMapField field)
        {
            DbParameter param = command.CreateParameter();
            param.ParameterName = _dialect.FormatParameterName(field.FieldName, command.CommandType);
            param.SourceColumn = field.FieldName;
            param.SmartSetType(field.DataType);

            if (field.Length >= 0) param.Size = field.Length;

            return param;
        }

        /// <summary>
        /// Creates the where param.
        /// </summary>
        /// <param name="command">The command.</param>
        /// <param name="field">The field.</param>
        /// <returns></returns>
        private DbParameter CreateWhereParam(DbCommand command, IDataMapField field)
        {
            DbParameter param = command.CreateParameter();
            param.ParameterName = _dialect.FormatParameterName("old_" + field.FieldName, command.CommandType);
            param.SourceColumn = field.FieldName;
            param.SmartSetType(field.DataType);

            if (field.Length >= 0) param.Size = field.Length;

            return param;
        }

        /// <summary>
        /// Determines whether the field value has changed.
        /// </summary>
        /// <param name="pi">The property to check.</param>
        /// <param name="obj">The object instance.</param>
        /// <returns>
        /// 	<c>true</c> if field value has changed; otherwise, <c>false</c>.
        /// </returns>
        private static bool HasFieldChanged(PropertyInfo pi, ISavable obj)
        {
            object originalValue;
            if (! obj.OriginalValues.TryGetValue(pi.Name, out originalValue)) 
                return false;
            
            object currentValue = pi.GetValue(obj, null);
            if (currentValue == null) return (originalValue != null);
            
            return (! currentValue.Equals(originalValue));
        }

        /// <summary>
        /// Gets the text command.
        /// </summary>
        /// <param name="text">The text.</param>
        /// <returns></returns>
        private DbCommand GetTextCommand(string text)
        {
            DbCommand command = _connection.CreateCommand();
            command.CommandType = CommandType.Text;
            command.CommandText = text;
            command.Connection = _connection;
            return command;
        }

        private string BuildWhereClauseFromKeys(DbCommand command)
        {
            var whereString = new StringBuilder();
            foreach (IDataMapField field in _dataMap.AllKeys)
            {
                DbParameter whereParam = CreateWhereParam(command, field);

                if (whereString.Length > 0) whereString.Append(" AND ");
                whereString.Append(_dialect.FormatFieldName(field.FieldName, (field.UseQuotedIdentifier ?? UseQuotedIdentifier)));
                whereString.Append(" = ");
                whereString.Append(whereParam.ParameterName);

                command.Parameters.Add(whereParam);
            }
            return whereString.ToString();
        }

        /// <summary>
        /// Gets the name of the table.
        /// </summary>
        /// <value>The name of the table.</value>
        public string TableName
        {
            get
            {
                if (string.IsNullOrEmpty(_tableName))
                {
                    _tableName = _dialect.FormatTableName(
                        _dataMap.DataItem.TableName, 
                        _dataMap.DataItem.SchemaName,
                        (_dataMap.DataItem.UseQuotedIdentifier ?? UseQuotedIdentifier)
                        );
                }
                return _tableName;
            }
            set { _tableName = value; }
        }
        private string _tableName;

        /// <summary>
        /// Gets the name of the table for a SELECT.
        /// </summary>
        /// <value>The name of the select table.</value>
        private string SelectTableName
        {
            get
            {
                return (NoLock && _dialect.SupportsNoLock)
                        ? TableName + " WITH (NOLOCK)"
                        : TableName;
            }
        }

        /// <summary>
        /// Gets the name of the table to perform the save operation on.
        /// </summary>
        /// <value>The save to table.</value>
        public string SaveToTable
        {
            get
            {
                if (string.IsNullOrEmpty(_saveToTable))
                    _saveToTable = _dialect.FormatTableName(_dataMap.DataItem.SaveToTable, _dataMap.DataItem.SchemaName, 
                                        (_dataMap.DataItem.UseQuotedIdentifier ?? UseQuotedIdentifier) );

                return _saveToTable;
            }
            set { _saveToTable = value; }
        }
        private string _saveToTable;

        /// <summary>
        /// Gets or sets the default value used when inserting null reference strings.
        /// </summary>
        /// <value>The null string default.</value>
        public object NullStringDefault
        {
            get { return _nullStringDefault; }
            set { _nullStringDefault = value; }
        }
        private object _nullStringDefault = DBNull.Value;

        /// <summary>
        /// Gets or sets the use quoted identifiers.
        /// </summary>
        /// <value>The use quoted identifiers.</value>
        public bool? UseQuotedIdentifier { get; set; }

        /// <summary>
        /// Gets or sets the use table with field names.
        /// </summary>
        /// <value>The use table with field names.</value>
        public bool UseTableWithFieldNames
        {
            get { return _useTableWithFieldNames; }
            set
            {
                _useTableWithFieldNames = value;
                _builtColumnsStr = null;
            }
        }
        private bool _useTableWithFieldNames;

        /// <summary>
        /// Sets a string parameter value.
        /// </summary>
        /// <param name="field">The field.</param>
        /// <param name="parm">The parm.</param>
        /// <param name="value">The value.</param>
        /// <param name="inserting">if set to <c>true</c> [inserting].</param>
        private void SetStringParamValue(IDataMapField field, IDataParameter parm, object value, bool inserting)
        {			
            if (value == null)
            {
                parm.Value = (inserting) ? _nullStringDefault : DBNull.Value;
                return;
            }

            string sValue = value.ToString();
            if ( (field.Length > 0) && (sValue.Length > field.Length))
            {
                if (! field.TrimToFit)
                    throw new ArgumentOutOfRangeException(string.Format("Data Field `{0}` has a maximum length of {1} and was supplied a value with a length of {2}.", field.FieldName, field.Length, sValue.Length));

                parm.Value = sValue.Substring(0, field.Length);
            }
            else
            {
                parm.Value = sValue;	
            }
        }
    }
}