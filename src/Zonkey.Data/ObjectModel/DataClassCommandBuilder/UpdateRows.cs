using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Reflection;
using System.Text;
using Zonkey.Dialects;

namespace Zonkey.ObjectModel
{
    /// <summary>
    /// Provides methods to create SQL commands to interact with a <see cref="Zonkey.ObjectModel.DataClass"/>.
    /// </summary>
    public partial class DataClassCommandBuilder
    {
        public DbCommand GetUpdateRowsCommand(IDictionary<string, object> setClause, string filterText)
        {
            DbCommand command = GetTextCommand("");

            var setString = new StringBuilder();
            var setParmList = new List<DbParameter>();
            foreach (KeyValuePair<string, object> pair in setClause)
            {
                PropertyInfo pi = _dataObjectInfo.GetProperty(pair.Key);
                if (pi == null)
                    throw new ArgumentException(string.Format("Cannot match property '{0}'", pair.Key));

                IDataMapField field = _dataMap.GetFieldForProperty(pi);
                if (field == null)
                    throw new ArgumentException(string.Format("Cannot match DataMap field '{0}'", pair.Key));

                string sFieldDescr = _dialect.FormatFieldName(field.FieldName, (field.UseQuotedIdentifier ?? UseQuotedIdentifier));

                if (setString.Length > 0) setString.Append(", ");

                DbParameter setParm = CreateSetParam(command, field);
                setString.Append(sFieldDescr);
                setString.Append(" = ");
                setString.Append(setParm.ParameterName);

                if (pi.PropertyType == typeof(string))
                    SetStringParamValue(field, setParm, pair.Value, false);
                else
                    setParm.Value = (pair.Value ?? DBNull.Value);

                setParmList.Add(setParm);
            }
            if (setParmList.Count == 0) return null;

            command.CommandText = String.Format("UPDATE {0} SET {1} WHERE {2};", SaveToTable, setString, filterText);
            command.Parameters.AddRange(setParmList.ToArray());

            if (_dialect.SupportsChangeContext && ChangeTrackingContext != null)
                _dialect.ApplyChangeTrackingContext(command, ChangeTrackingContext);

            return command;
        }
    }
}