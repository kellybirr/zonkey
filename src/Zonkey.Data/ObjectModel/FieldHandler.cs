using System;
using System.Globalization;
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
                    else if (value is DateOnly srcDateOnly && (dstType == typeof(DateTime) || dstType == typeof(DateTime?)))
                    {
                        // providers may surface date columns as DateOnly (e.g. Npgsql 10);
                        // Convert.ChangeType cannot help because DateOnly is not IConvertible
                        dstInfo.SetValue(obj, ApplyDateTimeKind(DateOnlyToDateTime(srcDateOnly), mapField), null);
                    }
                    else if (value is TimeOnly srcTimeOnly && (dstType == typeof(TimeSpan) || dstType == typeof(TimeSpan?)))
                    {
                        dstInfo.SetValue(obj, TimeOnlyToTimeSpan(srcTimeOnly), null);
                    }
                    else if (value is TimeOnly srcTimeOnly2 && (dstType == typeof(DateTime) || dstType == typeof(DateTime?)))
                    {
                        dstInfo.SetValue(obj, ApplyDateTimeKind(TimeOnlyToDateTime(srcTimeOnly2), mapField), null);
                    }
#endif
                    else if (value is string timeSpanText && (dstType == typeof(TimeSpan) || dstType == typeof(TimeSpan?)))
                    {
                        // text-backed time columns (e.g. SQLite); standard "d.hh:mm:ss" form
                        dstInfo.SetValue(obj, StringToTimeSpan(timeSpanText), null);
                    }
                    else if (value is DateTime srcDateTime && (dstType == typeof(TimeSpan) || dstType == typeof(TimeSpan?)))
                    {
                        dstInfo.SetValue(obj, DateTimeToTimeSpan(srcDateTime), null);
                    }
                    else if (dstType == typeof(string) && ToIsoString(value) is string isoText)
                    {
                        dstInfo.SetValue(obj, isoText, null);
                    }
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

        // Conversions shared by this reflection path and the fast builder's emitted IL.
        // String renderings are round-trip ISO formats, never the current culture's
        // short patterns (which drop precision and vary by machine).

        internal static string DateTimeToIso(DateTime value) => value.ToString("O", CultureInfo.InvariantCulture);
        internal static string TimeSpanToIso(TimeSpan value) => value.ToString("c", CultureInfo.InvariantCulture);
        internal static TimeSpan StringToTimeSpan(string value) => TimeSpan.Parse(value, CultureInfo.InvariantCulture);
        internal static TimeSpan DateTimeToTimeSpan(DateTime value) => value.TimeOfDay;

#if !NETFRAMEWORK
        internal static string DateOnlyToIso(DateOnly value) => value.ToString("O", CultureInfo.InvariantCulture);
        internal static string TimeOnlyToIso(TimeOnly value) => value.ToString("O", CultureInfo.InvariantCulture);
        internal static DateTime DateOnlyToDateTime(DateOnly value) => value.ToDateTime(TimeOnly.MinValue);
        internal static TimeSpan TimeOnlyToTimeSpan(TimeOnly value) => value.ToTimeSpan();

        // baseline date 0001-01-01 mirrors TimeOnly.FromDateTime (which strips the date
        // part), so the round-trip through either representation is lossless
        internal static DateTime TimeOnlyToDateTime(TimeOnly value) => default(DateTime).Add(value.ToTimeSpan());
#endif

        private static string ToIsoString(object value)
        {
            switch (value)
            {
                case DateTime dt: return DateTimeToIso(dt);
                case TimeSpan ts: return TimeSpanToIso(ts);
#if !NETFRAMEWORK
                case DateOnly d: return DateOnlyToIso(d);
                case TimeOnly t: return TimeOnlyToIso(t);
#endif
                default: return null;
            }
        }

        private static Type GetEnumType(Type propertyType)
        {
            Type t = Nullable.GetUnderlyingType(propertyType) ?? propertyType;
            return t.IsEnum ? t : null;
        }
    }
}
