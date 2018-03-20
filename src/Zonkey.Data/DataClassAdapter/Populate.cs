using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Reflection;
using System.Threading.Tasks;
using Zonkey.ObjectModel;

namespace Zonkey
{
    public partial class DataClassAdapter<T>
    {
        private async Task<int> PopulateCollection(ICollection<T> collection, DbDataReader reader)
        {
            // Get DataClassReader
            using (var classReader = new DataClassReader<T>(reader, DataMap) {ObjectFactory = ObjectFactory, KeepOpen = true})
                return await classReader.FillAsyncInternal(collection);
        }

        private void PopulateSingleObject(T obj, IDataRecord record, bool skipDbNull)
        {
            lock (obj)
            {
                for (int i = 0; i < record.FieldCount; i++)
                {
                    IDataMapField field = DataMap.GetReadableField(record.GetName(i));
                    if (field == null) continue;

                    PropertyInfo pi = field.Property;
                    if (pi == null) continue;

                    Type propType = pi.PropertyType;
                    TypeInfo typeInfo = propType.GetTypeInfo();
                    if (typeInfo.IsEnum) propType = Enum.GetUnderlyingType(propType);

                    if (record.IsDBNull(i))
                    {
                        if (skipDbNull) continue;
                        if ( (!typeInfo.IsValueType) || (Nullable.GetUnderlyingType(propType) != null) )
                            pi.SetValue(obj, null, null);
                    }
                    else
                    {
                        object oValue = record.GetValue(i);
                        Type dbFieldType = record.GetFieldType(i);
                        try
                        {
                            if (! typeInfo.IsAssignableFrom(dbFieldType))
                            {
                                if ( (propType == typeof(Guid)) && (oValue is string) )
                                    pi.SetValue(obj, new Guid(oValue.ToString()), null);
                                else if (dbFieldType.Name.EndsWith("SqlHierarchyId")) // if the column is a HierarchyID type, then just treat it as a string (SQL server can implicitly convert between the two)
                                    pi.SetValue(obj, oValue.ToString(), null);
                                else
                                    pi.SetValue(obj, Convert.ChangeType(oValue, propType), null);
                            }
                            else if ((oValue is DateTime) && (field.DateTimeKind != DateTimeKind.Unspecified))
                            {	// special date/time handling for UTC and Local times
                                var dtValue = new DateTime(((DateTime)oValue).Ticks, field.DateTimeKind);
                                pi.SetValue(obj, dtValue, null);
                            }
                            else
                                pi.SetValue(obj, oValue, null);
                        }
                        catch (Exception ex)
                        {
                            throw new PropertyReadException(pi, oValue, ex);
                        }
                    }
                }
            }
        }
    }
}