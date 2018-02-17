using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Reflection;
using System.Text;

namespace Zonkey.ObjectModel
{
    /// <summary>
    /// Provides methods to create SQL commands to interact with a <see cref="Zonkey.ObjectModel.DataClass"/>.
    /// </summary>
    public partial class DataClassCommandBuilder
    {
        /// <summary>
        /// Creates Update command with optimistic concurrency support
        /// </summary>
        /// <param name="obj">The <see cref="Zonkey.ObjectModel.DataClass"/> instance containing the values to be updated.</param>
        /// <param name="criteria">The <see cref="Zonkey.UpdateCriteria"/>.</param>
        /// <param name="doSelectBack">if set to <c>true</c> then returns the contents of the table row after update, otherwise just performs an update.</param>
        /// <returns>A <see cref="System.Data.Common.DbCommand"/> array.</returns>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Maintainability", "CA1502:AvoidExcessiveComplexity")]
        public DbCommand[] GetUpdate2Commands(ISavable obj, UpdateCriteria criteria, bool doSelectBack)
        {
            if (obj == null) throw new ArgumentNullException(nameof(obj));
            if (obj.GetType() != _dataObjectType)
                throw new ArgumentException("Type of 'obj' does not match type from constructor.");

            if (criteria == UpdateCriteria.AllFields)
                throw new ArgumentException("UpdateCriteria.AllFields is not supported by Update2 methods.");

            // set update criteria - default is changed fields
            if (criteria == UpdateCriteria.Default)
            {
                criteria = (_dataMap.DataItem.UpdateCriteria == UpdateCriteria.Default)
                               ? UpdateCriteria.ChangedFields : _dataMap.DataItem.UpdateCriteria;
            }
            if (criteria == UpdateCriteria.KeyAndVersion)
                criteria = UpdateCriteria.ChangedFields;

            // check for keys
            if (_dataMap.KeyFields.Count == 0)
                throw new InvalidOperationException(String.Format("Class '{0}' does not contain any properties with DataFieldAttributes or none are marked with IsKeyField.", _dataObjectType.FullName));

            // init commands
            DbCommand updateCommand = GetTextCommand("");
            DbCommand selectCommand = (doSelectBack) ? GetTextCommand("") : null;

            // create string builders and param lists
            var setString = new StringBuilder();
            var setParmList = new List<DbParameter>();

            var whereString = new StringBuilder();
            var whereParmList = new List<DbParameter>();

            foreach (KeyValuePair<string, object> changedField in obj.OriginalValues)
            {
                PropertyInfo pi = _dataObjectInfo.GetProperty(changedField.Key);
                if (pi == null) continue;

                IDataMapField field = _dataMap.GetFieldForProperty(pi);
                if ((field == null) || (field.AccessType == AccessType.ReadOnly)) continue;
                if ((field.IsAutoIncrement) || (field.IsRowVersion)) continue;

                string sFieldDescr = _dialect.FormatFieldName(field.FieldName, (field.UseQuotedIdentifier ?? UseQuotedIdentifier));

                if ( field.IsKeyField || field.IsPartitionKey
                     || (criteria >= UpdateCriteria.ChangedFields)
                     || (field.IsRowVersion && (criteria == UpdateCriteria.KeyAndVersion))
                    )
                {
                    // A primary key or row version
                    if (whereString.Length > 0) whereString.Append(" AND ");
                    object oParmValue = (changedField.Value ?? DBNull.Value);

                    // add to command
                    if (oParmValue == DBNull.Value)
                        whereString.AppendFormat("{0} IS NULL", sFieldDescr);
                    else if (! field.IsComparable)
                        whereString.AppendFormat("{0} IS NOT NULL", sFieldDescr);
                    else
                    {
                        DbParameter whereParam = CreateWhereParam(updateCommand, field);
                        whereString.Append(sFieldDescr);
                        whereString.Append(" = ");
                        whereString.Append(whereParam.ParameterName);

                        whereParam.Value = oParmValue;
                        whereParmList.Add(whereParam);
                    }
                }

                if (setString.Length > 0) setString.Append(", ");

                DbParameter setParm = CreateSetParam(updateCommand, field);
                setString.Append(sFieldDescr);
                setString.Append(" = ");
                setString.Append(setParm.ParameterName);

                if (pi.PropertyType == typeof(string))
                    SetStringParamValue(field, setParm, pi.GetValue(obj, null), false);
                else             
                    setParm.Value = (pi.GetValue(obj, null) ?? DBNull.Value);

                setParmList.Add(setParm);
            }
            if (setParmList.Count == 0) return null;

            var keyString = new StringBuilder();
            var keyParmList = new List<DbParameter>();

            foreach (IDataMapField field in _dataMap.AllKeys)
            {
                PropertyInfo pi = field.Property;
                if (pi == null) continue;

                string sFieldDescr = _dialect.FormatFieldName(field.FieldName, (field.UseQuotedIdentifier ?? UseQuotedIdentifier));

                if (doSelectBack)
                {
                    DbParameter keyParam = CreateWhereParam(selectCommand, field);
                    if (keyString.Length > 0) keyString.Append(" AND ");
                    keyString.Append(sFieldDescr);
                    keyString.Append(" = ");
                    keyString.Append(keyParam.ParameterName);


                    keyParam.Value = pi.GetValue(obj, null);
                    keyParmList.Add(keyParam);
                }

                // A primary key or row version
                if (whereString.Length > 0) whereString.Append(" AND ");

                // get value for parameter
                object oParmValue;
                if (obj.OriginalValues.ContainsKey(pi.Name))
                    oParmValue = (obj.OriginalValues[pi.Name] ?? DBNull.Value);
                else
                    oParmValue = (pi.GetValue(obj, null) ?? DBNull.Value);

                // add to command
                if (oParmValue == DBNull.Value)
                    whereString.AppendFormat("{0} IS NULL", sFieldDescr);
                else
                {
                    DbParameter whereParam = CreateWhereParam(updateCommand, field);
                    whereString.Append(sFieldDescr);
                    whereString.Append(" = ");
                    whereString.Append(whereParam.ParameterName);

                    whereParam.Value = oParmValue;
                    whereParmList.Add(whereParam);
                }
            }

            // setup update command
            updateCommand.CommandText = String.Format("UPDATE {0} SET {1} WHERE {2};", SaveToTable, setString, whereString);
            updateCommand.Parameters.AddRange(setParmList.ToArray());
            updateCommand.Parameters.AddRange(whereParmList.ToArray());

            if (_dialect.SupportsChangeContext && ChangeTrackingContext != null)
                _dialect.ApplyChangeTrackingContext(updateCommand, ChangeTrackingContext);

            if (! doSelectBack)	// if no select-back return now
                return new[] { updateCommand, null };

            // setup select command and return both
            selectCommand.CommandText = String.Format("SELECT {0} FROM {1} WHERE {2};", ColumnsString, TableName, keyString);
            selectCommand.Parameters.AddRange(keyParmList.ToArray());

            return new[] { updateCommand, selectCommand };
        }
    }
}