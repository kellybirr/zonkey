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
        /// Creates Insert command with support for Auto increment (Identity) tables
        /// </summary>
        /// <param name="obj">The obj.</param>
        /// <param name="selectBack">The select back.</param>
        /// <returns></returns>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Maintainability", "CA1502:AvoidExcessiveComplexity")]
        public DbCommand[] GetInsertCommands(object obj, SelectBack selectBack)
        {
            DbCommand command1 = GetTextCommand("");
            var intoString = new StringBuilder();
            var valuesString = new StringBuilder();
            var setParmList = new List<DbParameter>();

            var whereString = new StringBuilder();
            var whereParmList = new List<DbParameter>();

            var selectString = new StringBuilder();

            // set select back default
            if (selectBack == SelectBack.Default)
            {
                selectBack = (_dataMap.DataItem.SelectBack == SelectBack.Default)
                                 ? SelectBack.UnchangedFields : _dataMap.DataItem.SelectBack;
            }

            foreach (IDataMapField field in _dataMap.DataFields)
            {
                PropertyInfo pi = field.Property;
                if (pi == null) continue;

                string sFieldDescr = _dialect.FormatFieldName(field.FieldName, (field.UseQuotedIdentifier ?? UseQuotedIdentifier));

                if (field.IsAutoIncrement)
                {
                    if (selectBack > SelectBack.None)
                    {
                        if (field.IsKeyField)
                        {
                            if (whereString.Length > 0) whereString.Append(" AND ");
                            whereString.Append(sFieldDescr);
                            whereString.Append(" = ");
                            whereString.Append(_dialect.FormatAutoIncrementSelect(field.SequenceName));

                            // special case to handle bad access behavior on identity keys
                            if ((_dialect is Dialects.AccessSqlDialect) && (selectBack > SelectBack.IdentityOrVersion))
                                selectBack = SelectBack.IdentityOrVersion;
                        }

                        // supports identity only select-back
                        if (selectString.Length > 0) selectString.Append(", ");
                        selectString.Append(_dialect.FormatAutoIncrementSelect(field.SequenceName));
                        selectString.Append(" AS ");
                        selectString.Append(sFieldDescr);
                    }

                    continue;
                }
                
                if (field.IsKeyField)
                {
                    DbParameter whereParam = CreateWhereParam(command1, field);
                    if (whereString.Length > 0) whereString.Append(" AND ");
                    whereString.Append(sFieldDescr);
                    whereString.Append(" = ");
                    whereString.Append(whereParam.ParameterName);

                    whereParam.Value = pi.GetValue(obj, null);
                    whereParmList.Add(whereParam);
                }

                if ((field.IsRowVersion) || (field.AccessType == AccessType.ReadOnly))
                    continue;

                object oValue = pi.GetValue(obj, null);
                if ((oValue == null) && (!field.IsNullable)) continue;
                if ((oValue is Guid) && (Guid.Empty == (Guid)oValue)) continue;

                DbParameter parm = CreateSetParam(command1, field);
                if (pi.PropertyType == typeof (string))
                    SetStringParamValue(field, parm, oValue, true);
                else
                    parm.Value = (oValue ?? DBNull.Value);

                if (intoString.Length > 0)
                {
                    intoString.Append(", ");
                    valuesString.Append(", ");
                }

                intoString.Append(sFieldDescr);
                valuesString.Append(parm.ParameterName);

                setParmList.Add(parm);
            }

            if (intoString.Length == 0)
                throw new InvalidOperationException(String.Format("Class '{0}' does not contain any properties with DataFieldAttribute(s) or all fields are ReadOnly.", _dataObjectType.FullName));

            command1.CommandText = string.Format("INSERT INTO {0} ({1}) VALUES ({2}); ", SaveToTable, intoString, valuesString);
            command1.Parameters.AddRange(setParmList.ToArray());

            if (_dialect.SupportsChangeContext && ChangeTrackingContext != null)
                _dialect.ApplyChangeTrackingContext(command1, ChangeTrackingContext);

            if (selectBack != SelectBack.None)
            {
                string sSql2;
                if ((selectBack == SelectBack.IdentityOrVersion) && (selectString.Length > 0))
                    sSql2 = string.Concat("SELECT ", selectString);
                else if (whereString.Length == 0)
                    throw new InvalidOperationException(String.Format("Class '{0}' does not contain any properties with DataFieldAttributes or none are marked with IsKeyField.", _dataObjectType.FullName));
                else
                    sSql2 = string.Format("SELECT {0} FROM {1} WHERE {2}; ", ColumnsString, TableName, whereString);

                if (_dialect.UseSqlBatches)
                {
                    command1.CommandText += sSql2;
                    command1.Parameters.AddRange(whereParmList.ToArray());
                }
                else
                {
                    DbCommand command2 = GetTextCommand(sSql2);
                    command2.Parameters.AddRange(whereParmList.ToArray());

                    return new[] {command1, command2};
                }
            }

            return new[] {command1};
        }

        /// <summary>
        /// Gets the bulk insert info.
        /// </summary>
        /// <param name="command">The command.</param>
        /// <param name="sortedProps">The sorted props.</param>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1021:AvoidOutParameters", MessageId = "1#")]
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1021:AvoidOutParameters", MessageId = "0#")]
        public void GetBulkInsertInfo(out DbCommand command, out PropertyInfo[] sortedProps)
        {
            command = GetTextCommand("");
            var intoString = new StringBuilder();
            var valuesString = new StringBuilder();

            var setParmList = new List<DbParameter>();
            var sortedPropList = new List<PropertyInfo>();

            foreach (IDataMapField field in _dataMap.DataFields)
            {
                if (field.AccessType == AccessType.ReadOnly) continue;
                if ((field.IsRowVersion) || (field.IsAutoIncrement)) continue;

                DbParameter parm = CreateSetParam(command, field);
                if (intoString.Length > 0)
                {
                    intoString.Append(", ");
                    valuesString.Append(", ");
                }

                intoString.Append(_dialect.FormatFieldName(field.FieldName, (field.UseQuotedIdentifier ?? UseQuotedIdentifier)));
                valuesString.Append(parm.ParameterName);

                setParmList.Add(parm);
                sortedPropList.Add(field.Property);
            }

            if (intoString.Length == 0)
                throw new InvalidOperationException(String.Format("Class '{0}' does not contain any properties with DataFieldAttribute(s) or all fields are ReadOnly.", _dataObjectType.FullName));

            command.CommandText = string.Format("INSERT INTO {0} ({1}) VALUES ({2}); ", SaveToTable, intoString, valuesString);
            command.Parameters.AddRange(setParmList.ToArray());

            sortedProps = sortedPropList.ToArray();
        }
    }
}