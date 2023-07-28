using System;
using System.Reflection;

namespace Zonkey.ObjectModel
{
    internal static class FieldHandler
    {
        public static void SetValue<T>(T obj, object value, IDataMapField mapField, Type srcType, PropertyInfo dstInfo, Type dstType=null) 
            where T : class
        {
            dstType ??= dstInfo.PropertyType;
            SetValue(obj, value, mapField, srcType, dstInfo, dstType, dstType.IsAssignableFrom(srcType));
        }

        public static void SetValue<T>(T obj, object value, IDataMapField mapField, Type srcType, PropertyInfo dstInfo, Type dstType, bool isAssignable)
            where T : class
        {
            try
            {
                if (!isAssignable)
                {
                    if (dstType == typeof(Guid) && value is string valStr)
                        dstInfo.SetValue(obj, new Guid(valStr), null);
                    else if (srcType.Name.EndsWith("SqlHierarchyId")) // if the column is a HierarchyID type, then just treat it as a string (SQL server can implicitly convert between the two)
                        dstInfo.SetValue(obj, value.ToString(), null);
#if (NET6_0_OR_GREATER)
                    else if (dstType == typeof(DateOnly) || dstType == typeof(DateOnly?))
                    {
                        if (value is DateTime dtDO)
                            dstInfo.SetValue(obj, DateOnly.FromDateTime(dtDO), null);
                        else
                            dstInfo.SetValue(obj, DateOnly.Parse(value.ToString()), null);
                    }
                    else if (dstType == typeof(TimeOnly) || dstType == typeof(TimeOnly?))
                    {
                        if (value is TimeSpan ts)
                            dstInfo.SetValue(obj, TimeOnly.FromTimeSpan(ts), null);
                        else if (value is DateTime dtTO)
                            dstInfo.SetValue(obj, TimeOnly.FromDateTime(dtTO), null);
                        else
                            dstInfo.SetValue(obj, TimeOnly.Parse(value.ToString()), null);
                    }
#endif
                    else if (value != null && dstType.Name == "Nullable`1")
                        dstInfo.SetValue(obj, Convert.ChangeType(value, dstType.GenericTypeArguments[0]), null);
                    else
                        dstInfo.SetValue(obj, Convert.ChangeType(value, dstType), null);
                }
                else if ((value is DateTime dt) && (mapField.DateTimeKind != DateTimeKind.Unspecified))
                {   // special date/time handling for UTC and Local times
                    var dtValue = new DateTime(dt.Ticks, mapField.DateTimeKind);
                    dstInfo.SetValue(obj, dtValue, null);
                }
                else
                    dstInfo.SetValue(obj, value, null);
            }
            catch (Exception ex)
            {
                throw new PropertyReadException(dstInfo, value, ex);
            }
        }
    }
}
