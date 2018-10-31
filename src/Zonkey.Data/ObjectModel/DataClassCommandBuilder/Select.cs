using System;
using System.Data.Common;
using System.Text;

namespace Zonkey.ObjectModel
{
	/// <summary>
	/// Provides methods to create SQL commands to interact with a <see cref="Zonkey.ObjectModel.DataClass"/>.
	/// </summary>
	public partial class DataClassCommandBuilder
	{
		/// <summary>
		/// Gets the SELECT command filtered by primary key fields.
		/// </summary>
		public DbCommand SelectByKeysCommand
		{
			get
			{
				if (_selectByKeysCommand == null)
				{
					_selectByKeysCommand = GetTextCommand("");

					string whereString = BuildWhereClauseFromKeys(_selectByKeysCommand);
					_selectByKeysCommand.CommandText = string.Format("SELECT {0} FROM {1} WHERE {2}", ColumnsString, SelectTableName, whereString);
				}
				return _selectByKeysCommand;
			}
		}

		private DbCommand _selectByKeysCommand;

		/// <summary>
		/// Gets the select command.
		/// </summary>
		/// <param name="filter">The filter.</param>
		/// <returns></returns>
		public DbCommand GetSelectCommand(string filter)
		{
			return GetSelectCommand(filter, null);
		}

		/// <summary>
		/// Gets the select command.
		/// </summary>
		/// <param name="filter">The filter.</param>
		/// <param name="sort">The sort.</param>
		/// <returns></returns>
		public DbCommand GetSelectCommand(string filter, string sort)
		{
			var commandText = new StringBuilder();
			commandText.AppendFormat("SELECT {0} FROM {1}", GetProjectionString(), SelectTableName);

			if (! string.IsNullOrEmpty(filter))
				commandText.AppendFormat(" WHERE {0}", filter);

			if ( (! string.IsNullOrEmpty(sort)) && (string.IsNullOrEmpty(filter) || (! filter.ToUpper().Contains("ORDER BY"))))
				commandText.AppendFormat(" ORDER BY {0}", sort);

			return GetTextCommand(commandText.ToString());
		}

		/// <summary>
		/// Gets the select command.
		/// </summary>
		/// <param name="filters">The array of SqlFilters.</param>
		/// <param name="sort">The sort.</param>
		/// <returns></returns>
		public DbCommand GetSelectCommand(SqlFilter[] filters, string sort)
		{
			var commandText = new StringBuilder();
			commandText.AppendFormat("SELECT {0} FROM {1}", ColumnsString, SelectTableName);

			DbCommand command = GetTextCommand("");
			commandText.Append(ProcessFilters(command, filters, true));

			if (! string.IsNullOrEmpty(sort))
				commandText.AppendFormat(" ORDER BY {0}", sort);

			command.CommandText = commandText.ToString();
			return command;
		}

		/// <summary>
		/// Gets the select command.
		/// </summary>
		/// <param name="filter">The filter.</param>
		/// <param name="sort">The sort.</param>
		/// <param name="start">The index to fill from</param>
		/// <param name="length">The number of items to get</param>
		/// <returns></returns>
		public DbCommand GetSelectRangeCommand(string filter, string sort, int start, int length)
		{
			var sql = _dialect.FormatLimitQuery(ColumnsString, SelectTableName, filter, sort, start, length);
			return GetTextCommand(sql);
		}

		/// <summary>
		/// Gets the select command.
		/// </summary>
		/// <param name="filters">The array of SqlFilters.</param>
		/// <param name="sort">The sort.</param>
		/// <param name="start">The index to fill from</param>
		/// <param name="length">The number of items to get</param>
		/// <returns></returns>
		public DbCommand GetSelectRangeCommand(SqlFilter[] filters, string sort, int start, int length)
		{            
			DbCommand command = GetTextCommand("");
			var whereString = ProcessFilters(command, filters, false);

			command.CommandText = _dialect.FormatLimitQuery(ColumnsString, SelectTableName, whereString, sort, start, length);
			return command;
		}

		/// <summary>
		/// Gets the count command.
		/// </summary>
		/// <param name="filter">The filter.</param>
		/// <returns></returns>
		public DbCommand GetCountCommand(string filter)
		{
			var commandText = new StringBuilder();
			commandText.AppendFormat("SELECT COUNT(*) AS ZONKEY_ROWCOUNT FROM {0}", SelectTableName);

			if (!string.IsNullOrEmpty(filter))
				commandText.AppendFormat(" WHERE {0}", filter);

			return GetTextCommand(commandText.ToString());            
		}

		/// <summary>
		/// Gets the count command.
		/// </summary>
		/// <param name="filters">The filters.</param>
		/// <returns></returns>
		public DbCommand GetCountCommand(SqlFilter[] filters)
		{
			var commandText = new StringBuilder();
			commandText.AppendFormat("SELECT COUNT(*) AS ZONKEY_ROWCOUNT FROM {0}", SelectTableName);

			DbCommand command = GetTextCommand("");
			commandText.Append(ProcessFilters(command, filters, true));

			command.CommandText = commandText.ToString();
			return command;            
		}

        /// <summary>
        /// Gets the exists command.
        /// </summary>
        /// <param name="filter">The filter.</param>
        /// <returns></returns>
        public DbCommand GetExistsCommand(string filter)
        {
            var commandText = new StringBuilder();
            commandText.AppendFormat("IF EXISTS(SELECT * FROM {0}", SelectTableName);

            if (!string.IsNullOrEmpty(filter))
                commandText.AppendFormat(" WHERE {0}", filter);

            commandText.Append(") SELECT 1 AS ZONKEY_EXISTS ELSE SELECT 0 AS ZONKEY_EXISTS");

            return GetTextCommand(commandText.ToString());
        }

        /// <summary>
        /// Gets the exists command.
        /// </summary>
        /// <param name="filters">The filters.</param>
        /// <returns></returns>
        public DbCommand GetExistsCommand(SqlFilter[] filters)
        {
            var commandText = new StringBuilder();
            commandText.AppendFormat("IF EXISTS(SELECT * FROM {0}", SelectTableName);

            DbCommand command = GetTextCommand("");
            commandText.Append(ProcessFilters(command, filters, true));

            commandText.Append(") SELECT 1 AS ZONKEY_EXISTS ELSE SELECT 0 AS ZONKEY_EXISTS");

            command.CommandText = commandText.ToString();
            return command;
        }

		/// <summary>
		/// Processes the filters.
		/// </summary>
		/// <param name="command">The command.</param>
		/// <param name="filters">The filters.</param>
		/// <param name="prependWhere">if set to <c>true</c> [prepend where].</param>
		/// <returns></returns>
		private string ProcessFilters(DbCommand command, SqlFilter[] filters, bool prependWhere)
		{
			var whereText = new StringBuilder();
			if ((filters != null) && (filters.Length > 0))
			{
				if (prependWhere) 
					whereText.Append(" WHERE ");

				for (int i = 0; i < filters.Length; i++)
				{
					if (i > 0) whereText.Append(" AND ");
					whereText.Append(filters[i].ToString(_dialect, i));
					filters[i].AddToCommandParams(command, _dialect, i);
				}
			}

			return whereText.ToString();
		}


		/// <summary>
		/// Gets the select command.
		/// </summary>
		/// <param name="obj">The obj.</param>
		/// <returns></returns>
		[System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", MessageId = "Requery")]
		public DbCommand GetRequeryCommand(object obj)
		{
			if (obj == null) throw new ArgumentNullException(nameof(obj));
			if (obj.GetType() != _dataObjectType) throw new ArgumentException("Supplied Object is not a " + _dataObjectType.Name);

			DbCommand command = GetTextCommand("");

			var selectString = new StringBuilder();
			var whereString = new StringBuilder();

			// build command text and where params
			foreach (IDataMapField field in _dataMap.ReadableFields)
			{
				// add to select string
				if (selectString.Length > 0) selectString.Append(", ");
				selectString.Append(field.FieldName);

				if (field.IsKeyField)
				{
					// add to where clause if key field
					DbParameter whereParam = CreateWhereParam(command, field);
					if (whereString.Length > 0) whereString.Append(" AND ");
					whereString.AppendFormat("{0} = {1}", field.FieldName, whereParam.ParameterName);

					whereParam.Value = field.Property.GetValue(obj, null);
					command.Parameters.Add(whereParam);
				}
			}
			if ((selectString.Length == 0) || (whereString.Length == 0))
				throw new InvalidOperationException("Specified type has no properties with DataFieldAttributes or none are marked with IsKeyField.");

			command.CommandText = string.Format("SELECT {0} FROM {1} WHERE {2}; ", selectString, SelectTableName, whereString);

			return command;
		}


		/// <summary>
		/// Gets the columns string.
		/// </summary>
		/// <value>The columns string.</value>
		private string ColumnsString
		{
			get
			{
				if (_builtColumnsStr == null)
				{
					var columnsSb = new StringBuilder();
					foreach (IDataMapField field in _dataMap.ReadableFields)
					{
						if (columnsSb.Length > 0) columnsSb.Append(", ");

						if (_useTableWithFieldNames) columnsSb.Append(TableName + ".");

						columnsSb.Append(_dialect.FormatFieldName(field.FieldName, (field.UseQuotedIdentifier ?? UseQuotedIdentifier)));
					}

					if (columnsSb.Length == 0)
						throw new InvalidOperationException(String.Format("Class '{0}' does not contain any properties with DataFieldAttribute(s) or all fields are WriteOnly.", _dataObjectType.FullName));

					_builtColumnsStr = columnsSb.ToString();
				}

				return _builtColumnsStr;
			}
		}

		private string _builtColumnsStr;

	    private string GetProjectionString()
	    {
	        var projection = _projectionBuilder.FromDataMap(_dataMap);
	        return _projectionParser.GetQueryString(projection);
	    }
	}
}
