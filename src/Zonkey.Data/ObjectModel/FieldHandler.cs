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
                    if (value != null && dstType.IsInstanceOfType(value))
                    {
                        // statically unassignable but the runtime value fits: e.g. PostgreSQL
                        // array columns report System.Array while values are concrete arrays
                        dstInfo.SetValue(obj, ApplyDateTimeKind(value, mapField), null);
                    }
                    else if ((dstType == typeof(Guid) || dstType == typeof(Guid?)) && value is string valStr)
                    { 
                        dstInfo.SetValue(obj, new Guid(valStr), null);
                    }
                    else if (srcType.Name.EndsWith("SqlHierarchyId"))
                    {
                        // if the column is a HierarchyID type, then just treat it as a string (SQL server can implicitly convert between the two)
                        dstInfo.SetValue(obj, value.ToString(), null);
                    }
#if !NETFRAMEWORK
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
                    else if (value is string enumText && GetEnumType(dstInfo.PropertyType) is Type textEnumType)
                    {
                        // string-sourced enums accept names or numeric strings, case-insensitively
                        dstInfo.SetValue(obj, Enum.Parse(textEnumType, enumText, true), null);
                    }
                    else if (value != null && Nullable.GetUnderlyingType(dstType) is Type nullableType)
                    {
                        if (nullableType.IsEnum)
                        {
                            // ChangeType first so out-of-range values throw instead of silently wrapping
                            dstInfo.SetValue(obj, Enum.ToObject(nullableType, Convert.ChangeType(value, Enum.GetUnderlyingType(nullableType))), null);
                        }
                        else
                        {
                            dstInfo.SetValue(obj, ApplyDateTimeKind(Convert.ChangeType(value, nullableType), mapField), null);
                        }
                    }
                    else
                    {
                        dstInfo.SetValue(obj, ApplyDateTimeKind(Convert.ChangeType(value, dstType), mapField), null);
                    }
                }
                else if ((value is DateTime dt) && (mapField.DateTimeKind != DateTimeKind.Unspecified))
                {   // special date/time handling for UTC and Local times
                    var dtValue = new DateTime(dt.Ticks, mapField.DateTimeKind);
                    dstInfo.SetValue(obj, dtValue, null);
                }
                else
                { 
                    dstInfo.SetValue(obj, value, null);
                }
            }
            catch (Exception ex)
            {
                throw new PropertyReadException(dstInfo, value, ex);
            }
        }

        private static object ApplyDateTimeKind(object converted, IDataMapField mapField)
        {
            return (converted is DateTime dt) && (mapField.DateTimeKind != DateTimeKind.Unspecified)
                ? DateTime.SpecifyKind(dt, mapField.DateTimeKind)
                : converted;
        }

        private static Type GetEnumType(Type propertyType)
        {
            Type t = Nullable.GetUnderlyingType(propertyType) ?? propertyType;
            return t.IsEnum ? t : null;
        }
    }
}
