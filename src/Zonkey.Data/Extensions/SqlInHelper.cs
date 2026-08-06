using System;
using System.Collections.Generic;

namespace Zonkey.Extensions
{
    /// <summary>
    /// Batching helper for lists too large to pass to a single <c>IN</c> clause.
    /// </summary>
    public static class SqlInHelper
    {
        /// <summary>
        /// Splits a sequence into batches of at most <paramref name="size"/> items.
        /// </summary>
        /// <remarks>
        /// Predates <c>System.Linq</c>'s <c>Chunk</c>, which does the same job and is the
        /// recommended form on every target that has it (.NET 6 and later). This remains the
        /// supported answer on .NET Framework 4.8, where <c>Chunk</c> does not exist, and so is
        /// only marked obsolete where a replacement is actually available.
        /// <para>
        /// Note the shape difference when migrating: this materializes eagerly and returns
        /// <c>IList&lt;IList&lt;T&gt;&gt;</c>, whereas <c>Chunk</c> is lazy and yields
        /// <c>T[]</c> — which is what a <c>Contains</c> filter wants anyway.
        /// </para>
        /// </remarks>
        /// <typeparam name="T">The element type.</typeparam>
        /// <param name="inList">The sequence to split.</param>
        /// <param name="size">Maximum items per batch. Must be greater than zero.</param>
        /// <returns>The batches, in order.</returns>
#if NET6_0_OR_GREATER
        [Obsolete("Use System.Linq's Chunk(size) instead; SplitList predates it and does the same thing.")]
#endif
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
