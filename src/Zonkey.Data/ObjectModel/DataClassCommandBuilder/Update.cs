using System;
using System.Collections.Generic;
using System.Data;
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
        /// <param name="obj">The obj.</param>
        /// <param name="criteria">The criteria.</param>
        /// <param name="affect">The affect.</param>
        /// <param name="selectBack">The select back.</param>
        /// <returns></returns>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Maintainability", "CA1502:AvoidExcessiveComplexity")]
        public DbCommand[] GetUpdateCommands(object obj, UpdateCriteria criteria, UpdateAffect affect, SelectBack selectBack)
        {
            if (obj == null)
                throw new ArgumentNullException(nameof(obj));

            if (obj.GetType() != _dataObjectType)
                throw new ArgumentException("Type of 'obj' does not match type from constructor.");

            // set update criteria - default is changed fields
            if (criteria == UpdateCriteria.Default)
            {
                criteria = (_dataMap.DataItem.UpdateCriteria == UpdateCriteria.Default)
                               ? UpdateCriteria.ChangedFields : _dataMap.DataItem.UpdateCriteria;
            }
            if ((criteria == UpdateCriteria.KeyAndVersion) && (!_dialect.SupportsRowVersion))
                criteria = UpdateCriteria.ChangedFields;

            // set select back default
            if (selectBack == SelectBack.Default)
            {
                selectBack = (_dataMap.DataItem.SelectBack == SelectBack.Default)
                                 ? SelectBack.UnchangedFields : _dataMap.DataItem.SelectBack;
            }

            // get savable obj
            var objSV = obj as ISavable;
            if (objSV == null)
            {
                affect = UpdateAffect.AllFields;

                if (selectBack == SelectBack.UnchangedFields)
                    selectBack = SelectBack.AllFields;

                if (criteria == UpdateCriteria.ChangedFields)
                    criteria = UpdateCriteria.KeyOnly;
            }

            // init command array
            DbCommand updateCommand = GetTextCommand("");

            // create string builders and param lists
            var setString = new StringBuilder();
            var setParmList = new List<DbParameter>();

            var whereString = new StringBuilder();
            var whereParmList = new List<DbParameter>();

            var selectString = new StringBuilder();
            var keyString = new StringBuilder();
            var keyParmList = new List<DbParameter>();

            foreach (IDataMapField field in _dataMap.DataFields)
            {
                PropertyInfo pi = field.Property;
                if (pi == null) continue;

                bool hasChanged = (objSV == null) || HasFieldChanged(pi, objSV);
                string sFieldDescr = _dialect.FormatFieldName(field.FieldName, (field.UseQuotedIdentifier ?? UseQuotedIdentifier));

                if (selectBack > SelectBack.None)
                {
                    if (field.IsKeyField || field.IsPartitionKey)
                    {
                        DbParameter keyParam = CreateWhereParam(updateCommand, field);
                        if (keyString.Length > 0) keyString.Append(" AND ");
                        keyString.Append(sFieldDescr);
                        keyString.Append(" = ");
                        keyString.Append(keyParam.ParameterName);


                        keyParam.Value = pi.GetValue(obj, null);
                        keyParmList.Add(keyParam);
                    }
                    else if ((selectBack == SelectBack.AllFields)
                             || ((selectBack == SelectBack.UnchangedFields) && (!hasChanged))
                             || ((selectBack >= SelectBack.IdentityOrVersion) && (field.IsRowVersion)))
                    {
                        if (selectString.Length > 0) selectString.Append(", ");
                        selectString.Append(sFieldDescr);
                    }
                }

                if ( field.IsKeyField || field.IsPartitionKey
                     || (hasChanged && (criteria >= UpdateCriteria.ChangedFields))
                     || (field.IsRowVersion && (criteria == UpdateCriteria.KeyAndVersion))
                    )
                {
                    // A primary key or row version
                    if (whereString.Length > 0) whereString.Append(" AND ");

                    // get value for parameter
                    object oParmValue;
                    if ((objSV != null) && (objSV.OriginalValues.ContainsKey(pi.Name)))
                        oParmValue = (objSV.OriginalValues[pi.Name] ?? DBNull.Value);
                    else
                        oParmValue = (pi.GetValue(obj, null) ?? DBNull.Value);

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

                if ((field.IsAutoIncrement) || (field.IsRowVersion) || (field.AccessType == AccessType.ReadOnly)) continue;
                if ((affect == UpdateAffect.ChangedFields) && (!hasChanged)) continue;

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

            if (whereString.Length == 0)
                throw new InvalidOperationException(String.Format("Class '{0}' does not contain any properties with DataFieldAttributes or none are marked with IsKeyField.", _dataObjectType.FullName));

            updateCommand.CommandText = String.Format("UPDATE {0} SET {1} WHERE {2};", SaveToTable, setString, whereString);
            updateCommand.Parameters.AddRange(setParmList.ToArray());
            updateCommand.Parameters.AddRange(whereParmList.ToArray());

            if (_dialect.SupportsChangeContext && ChangeTrackingContext != null)
                _dialect.ApplyChangeTrackingContext(updateCommand, ChangeTrackingContext);

            if ( (selectBack > SelectBack.None) && (selectString.Length > 0) )
            {
                if (keyString.Length == 0)
                    throw new InvalidOperationException(String.Format("Class '{0}' does not contain any properties with DataFieldAttributes or none are marked with IsKeyField.", _dataObjectType.FullName));

                DbCommand selectCommand = GetTextCommand("");
                selectCommand.CommandText = String.Format("SELECT {0} FROM {1} WHERE {2};", selectString, TableName, keyString);
                selectCommand.Parameters.AddRange(keyParmList.ToArray());

                return new[] { updateCommand, selectCommand };
            }

            // return null index 1 to indicate no select-back
            return new[] { updateCommand, null };
        }

        /// <summary>
        /// Gets the bulk update info.
        /// </summary>
        /// <param name="updateKeys">whether to update key fields</param>
        /// <param name="command">The command.</param>
        /// <param name="sortedProps">The sorted props.</param>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1021:AvoidOutParameters", MessageId = "1#")]
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1021:AvoidOutParameters", MessageId = "2#")]
        public void GetBulkUpdateInfo(bool updateKeys, out DbCommand command, out PropertyInfo[] sortedProps)
        {
            command = GetTextCommand("");

            // create string builders and param lists
            var setString = new StringBuilder();
            var setParmList = new List<DbParameter>();

            var whereString = new StringBuilder();
            var whereParmList = new List<DbParameter>();

            var sortedPropList1 = new List<PropertyInfo>();
            var sortedPropList2 = new List<PropertyInfo>();

            foreach (IDataMapField field in _dataMap.DataFields)
            {
                string sFieldDescr = _dialect.FormatFieldName(field.FieldName, (field.UseQuotedIdentifier ?? UseQuotedIdentifier));

                if (field.IsKeyField || field.IsPartitionKey)
                {
                    // A primary key or row version
                    if (whereString.Length > 0) whereString.Append(" AND ");

                    // add to command
                    DbParameter whereParam = CreateWhereParam(command, field);
                    whereString.Append(sFieldDescr);
                    whereString.Append(" = ");
                    whereString.Append(whereParam.ParameterName);

                    whereParmList.Add(whereParam);
                    sortedPropList2.Add(field.Property);

                    if (! updateKeys) continue;
                }

                if ((field.IsAutoIncrement) || (field.IsRowVersion)
                    || (field.AccessType == AccessType.ReadOnly)) continue;

                if (setString.Length > 0) setString.Append(", ");

                DbParameter setParm = CreateSetParam(command, field);
                setString.Append(sFieldDescr);
                setString.Append(" = ");
                setString.Append(setParm.ParameterName);

                setParmList.Add(setParm);
                sortedPropList1.Add(field.Property);
            }

            if (setString.Length == 0)
                throw new InvalidOperationException(String.Format("Class '{0}' does not contain any properties with DataFieldAttribute(s) or all fields are ReadOnly.", _dataObjectType.FullName));

            if (whereString.Length == 0)
                throw new InvalidOperationException(String.Format("Class '{0}' does not contain any properties with DataFieldAttributes or none are marked with IsKeyField.", _dataObjectType.FullName));

            command.CommandText = String.Format("UPDATE {0} SET {1} WHERE {2};", SaveToTable, setString, whereString);
            command.Parameters.AddRange(setParmList.ToArray());
            command.Parameters.AddRange(whereParmList.ToArray());

            sortedPropList1.AddRange(sortedPropList2);

            sortedProps = sortedPropList1.ToArray();
        }
    }
}