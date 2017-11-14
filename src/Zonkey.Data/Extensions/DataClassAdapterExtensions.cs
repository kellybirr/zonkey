using System;
using System.Collections.Generic;
using System.Data;
using System.Linq.Expressions;
using System.Threading.Tasks;

namespace Zonkey.Extensions
{
    public static class DataClassAdapterExtensions
    {
        public static async Task<List<T>> GetList<T>(this DataClassAdapter<T> adapter, Expression<Func<T, bool>> filterExpression) 
            where T : class 
        {
            using (var reader = await adapter.OpenReader(filterExpression))
                return await reader.ToListAsync();
        }

        public static async Task<List<T>> GetList<T>(this DataClassAdapter<T> adapter, string filter, params object[] parameters) 
            where T : class
        {
            using (var reader = await adapter.OpenReader(filter, parameters))
                return await reader.ToListAsync();
        }

        public static async Task<T[]> GetArray<T>(this DataClassAdapter<T> adapter, Expression<Func<T, bool>> filterExpression) 
            where T : class
        {
            using (var reader = await adapter.OpenReader(filterExpression))
                return await reader.ToArrayAsync();
        }

        public static async Task<T[]> GetArray<T>(this DataClassAdapter<T> adapter, string filter, params object[] parameters)
            where T : class
        {
            using (var reader = await adapter.OpenReader(filter, parameters))
                return await reader.ToArrayAsync();
        }
    }

    public static class DataReaderExtension
    {
        public static IEnumerable<object[]> AsEnumeratedValues(this IDataReader sourceReader)
        {
            if (sourceReader == null)
                throw new ArgumentNullException(nameof(sourceReader));

            while (sourceReader.Read())
            {
                var row = new Object[sourceReader.FieldCount];
                sourceReader.GetValues(row);
                yield return row;
            }
        }

        public static IEnumerable<IDataRecord> AsEnumerable(this IDataReader sourceReader)
        {
            if (sourceReader == null)
                throw new ArgumentNullException(nameof(sourceReader));

            while (sourceReader.Read())
                yield return sourceReader;
        }
    }
}
