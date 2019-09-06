using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using Zonkey.ObjectModel;

namespace Zonkey.Extensions
{
    public static class DCAdapterExtensions
    {
        public static List<T> GetList<T>(this DCAdapterBase<T> adapter, Expression<Func<T, bool>> filterExpression)
            where T : class, new()
        {
            using (var reader = adapter.OpenReader(filterExpression))
                return reader.ToList();
        }

        public static T[] GetArray<T>(this DCAdapterBase<T> adapter, Expression<Func<T, bool>> filterExpression)
            where T : class, new()
        {
            using (var reader = adapter.OpenReader(filterExpression))
                return reader.ToArray();
        }
    }
}
