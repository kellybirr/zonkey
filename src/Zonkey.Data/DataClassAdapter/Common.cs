using System;
using System.Data;
using System.Data.Common;
using System.Reflection;
using System.Threading.Tasks;
using Zonkey.ObjectModel;

namespace Zonkey
{
	public partial class DataClassAdapter<T> : DataClassAdapter where T: class 
	{
		private readonly Type _objectType;
	    private readonly TypeInfo _typeInfo;

		/// <summary>
		/// Initializes a new instance of the <see cref="DataClassAdapter&lt;T&gt;"/> class.
		/// </summary>
		/// <param name="connection">Database Connection to be used by the adapter</param>
		public DataClassAdapter(DbConnection connection)
		{
			_objectType = typeof (T);
		    _typeInfo = _objectType.GetTypeInfo();

			DataMap = DataMap.GenerateCached(_objectType, null, null, DefaultSchemaVersion ?? 0);
			Connection = connection;
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="DataClassAdapter&lt;T&gt;"/> class.
		/// </summary>
		/// <param name="connection">Database Connection to be used by the adapter</param>
		/// <param name="schemaVersion">Version of table schema to use</param>
		public DataClassAdapter(DbConnection connection, int schemaVersion)
		{
			_objectType = typeof(T);
		    _typeInfo = _objectType.GetTypeInfo();

            DataMap = DataMap.GenerateCached(_objectType, null, null, schemaVersion);
			Connection = connection;
		}


		/// <summary>
		/// Initializes a new instance of the <see cref="DataClassAdapter&lt;T&gt;"/> class.
		/// </summary>
		/// <param name="connection">Database Connection to be used by the adapter</param>
		/// <param name="map">the data map</param>
		public DataClassAdapter(DbConnection connection, DataMap map)
		{
			_objectType = typeof (T);
		    _typeInfo = _objectType.GetTypeInfo();

            DataMap = map;
			Connection = connection;
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="DataClassAdapter&lt;T&gt;"/> class.
		/// </summary>
		/// <param name="connection">Database Connection to be used by the adapter</param>
		/// <param name="tableName">Name of the table to use for the data mapping.</param>
		public DataClassAdapter(DbConnection connection, string tableName)
		{
			_objectType = typeof(T);
		    _typeInfo = _objectType.GetTypeInfo();

            DataMap = DataMap.GenerateCached(_objectType, tableName, null, DefaultSchemaVersion ?? 0);
			Connection = connection;
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="DataClassAdapter&lt;T&gt;"/> class.
		/// </summary>
		/// <param name="connection">Database Connection to be used by the adapter</param>
		/// <param name="tableName">Name of the table to use for the data mapping.</param>
		/// <param name="schemaVersion">Version of table schema to use</param>
		public DataClassAdapter(DbConnection connection, string tableName, int schemaVersion)
		{
			_objectType = typeof(T);
		    _typeInfo = _objectType.GetTypeInfo();

            DataMap = DataMap.GenerateCached(_objectType, tableName, null, schemaVersion);
			Connection = connection;
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="DataClassAdapter&lt;T&gt;"/> class.
		/// </summary>
		/// <param name="connection">Database Connection to be used by the adapter</param>
		/// <param name="tableName">Name of the table to use for the data mapping.</param>
		/// <param name="keyFields">Names of the key fields in the table/object</param>
		public DataClassAdapter(DbConnection connection, string tableName, string[] keyFields)
		{
			_objectType = typeof(T);
		    _typeInfo = _objectType.GetTypeInfo();

            DataMap = DataMap.GenerateCached(_objectType, tableName, keyFields, DefaultSchemaVersion ?? 0);
			Connection = connection;
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="DataClassAdapter&lt;T&gt;"/> class.
		/// </summary>
		/// <param name="connection">Database Connection to be used by the adapter</param>
		/// <param name="tableName">Name of the table to use for the data mapping.</param>
		/// <param name="keyFields">Names of the key fields in the table/object</param>
		/// <param name="schemaVersion">Version of table schema to use</param>
		public DataClassAdapter(DbConnection connection, string tableName, string[] keyFields, int schemaVersion)
		{
			_objectType = typeof(T);
		    _typeInfo = _objectType.GetTypeInfo();

            DataMap = DataMap.GenerateCached(_objectType, tableName, keyFields, schemaVersion);
			Connection = connection;
		}

		/// <summary>
		/// Default constructor - must assign connection before use
		/// </summary>
		public DataClassAdapter()
		{
			_objectType = typeof(T);
		    _typeInfo = _objectType.GetTypeInfo();

            DataMap = DataMap.GenerateCached(_objectType, null, null, 0);
		}

		/// <summary>
		/// The command builder object
		/// </summary>
		/// <value>The command builder.</value>
		protected internal DataClassCommandBuilder CommandBuilder
		{
			get
			{
				return _commandBuilder ??
				       (_commandBuilder = new DataClassCommandBuilder(_objectType, DataMap, Connection, SqlDialect)
				                          	{
				                          		UseQuotedIdentifier = DefaultQuotedIdentifier
				                          	});
			}
		}
		private DataClassCommandBuilder _commandBuilder;

		/// <summary>
		/// Gets or Sets a value that controls the record sorting for Fill operations.
		/// </summary>
		/// <value>The SQL ORDER BY clause.</value>
		public string OrderBy
		{
			get { return _sortStr; }
			set { _sortStr = value; }
		}
		private string _sortStr;

		/// <summary>
		/// Gets or sets the default value used when inserting null reference strings.
		/// </summary>
		/// <value>The null string default.</value>
		public object NullStringDefault
		{
			get { return _nullStringDefault; }
			set
			{
				_nullStringDefault = value;
				CommandBuilder.NullStringDefault = value;
			}
		}
		private object _nullStringDefault = DBNull.Value;

		/// <summary>
		/// Gets or sets a value indicating whether [no lock].
		/// </summary>
		/// <value><c>true</c> if [no lock]; otherwise, <c>false</c>.</value>
		public bool NoLock 
		{
			get { return CommandBuilder.NoLock; }
			set { CommandBuilder.NoLock = value; } 
		}

        /// <summary>
        /// Ask Ionut
        /// </summary>
	    public object ChangeTrackingContext
	    {
            get { return CommandBuilder.ChangeTrackingContext; }
            set { CommandBuilder.ChangeTrackingContext = value; }
	    }

		/// <summary>
		/// Called when SqlDialect changes.
		/// </summary>
		protected override void OnDialectChanged()
		{
			_commandBuilder = null;
		}

		protected virtual T CreateNewT()
		{
			return ObjectFactory();
		}

		/// <summary>
		/// get or sets the Object factory used for creating new objects
		/// </summary>
		public Func<T> ObjectFactory
		{
			get
			{
				if (_objectFactory == null)
				{
					lock (this)
					{
						if (_objectFactory != null)
							return _objectFactory;

						_objectFactory = ClassFactory.GetFactory<T>();
					}
				}

				return _objectFactory;
			}
			set { _objectFactory = value; }
		}
		private Func<T> _objectFactory;

		/// <summary>
		/// Sets a non-public property for advanced scenarios.
		/// </summary>
		/// <param name="propertyName">Name of the property.</param>
		/// <param name="value">The value to assign.</param>
		public void SetProperty(AdapterProperty propertyName, object value)
		{
			switch (propertyName)
			{
				case AdapterProperty.TableName:
					CommandBuilder.TableName = (string)value;
					return;
				case AdapterProperty.SaveToTable:
					CommandBuilder.SaveToTable = (string)value;
					return;
				case AdapterProperty.UseQuotedIdentifiers:
					CommandBuilder.UseQuotedIdentifier = (bool?)value;
					return;
			}
		}

		private void SetCommandTimeout(IDbCommand command)
		{
			if (CommandTimeout.HasValue)
				command.CommandTimeout = CommandTimeout.Value;
			else if (DefaultCommandTimeout.HasValue)
				command.CommandTimeout = DefaultCommandTimeout.Value;
		}

		private Task<DbDataReader> ExecuteReaderInternal(DbCommand command, CommandBehavior behavior)
		{
			SetCommandTimeout(command);
			EnrollInTransaction(command);

			DoBeforeExecuteCommand(command);
			return command.ExecuteReaderAsync(behavior, CancellationToken);
		}

		private Task<int> ExecuteNonQueryInternal(DbCommand command)
		{
			SetCommandTimeout(command);
			EnrollInTransaction(command);

			DoBeforeExecuteCommand(command);
			return command.ExecuteNonQueryAsync(CancellationToken);
		}

        private Task<object> ExecuteScalerInternal(DbCommand command)
        {
            SetCommandTimeout(command);
            EnrollInTransaction(command);

            DoBeforeExecuteCommand(command);
            return command.ExecuteScalarAsync(CancellationToken);
        }
    }
}