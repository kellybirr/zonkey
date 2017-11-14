using System;
using System.Collections.Generic;

namespace Zonkey.Extensions
{
    /// <summary>
    /// 
    /// </summary>
    public static class SqlInHelper
    {
        /// <summary>
        /// Splits the list.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="inList">The in list.</param>
        /// <param name="size">The size.</param>
        /// <returns></returns>
        public static IList<IList<T>> SplitList<T>(this IEnumerable<T> inList, int size=2000)
        {
            if (size < 1)
                throw new ArgumentException("size must be > 0", nameof(size));

            var outerList = new List<IList<T>>();
            var innerList = new List<T>();
            foreach (T item in inList)
            {
                innerList.Add(item);

                if (innerList.Count >= size)
                {
                    outerList.Add(innerList);
                    innerList = new List<T>();
                }
            }

            if ( (innerList.Count > 0) && (!outerList.Contains(innerList)) )
                outerList.Add(innerList);

            return outerList;
        }
    }
}
