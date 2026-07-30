using System;
using System.Collections.Generic;

namespace Zonkey.ObjectModel
{
    /// <summary>
    /// Provides generic methods to sort a list.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class ListSorter<T>
    {
        /// <summary>
        /// Sorts the specified list.
        /// </summary>
        /// <typeparam name="P"></typeparam>
        /// <param name="list">The list.</param>
        /// <param name="lambda">The lambda.</param>
        public void Sort<P>(List<T> list, SortFunc<T, P> lambda)
            where P : IComparable<P>
        {
            Sort(list, lambda, false);
        }

        /// <summary>
        /// Sorts the specified list.
        /// </summary>
        /// <typeparam name="P"></typeparam>
        /// <param name="list">The list.</param>
        /// <param name="lambda">The lambda.</param>
        /// <param name="descending">if set to <c>true</c>, then sorts the list in descending order.</param>
        public void Sort<P>(List<T> list, SortFunc<T, P> lambda, bool descending)
            where P : IComparable<P>
        {
            // Apply and set the sort, if items to sort
            list.Sort(delegate(T x, T y)
            {
                // Get property values
                P xValue = lambda(x);
                P yValue = lambda(y);

                // Do comparison
                int result;

                if (xValue == null)
                    result = (yValue == null) ? 0 : 1;
                else
                    result = xValue.CompareTo(yValue);

                // return based on direction
                return (descending) ? -(result) : result;
            });
        }

        public void Sort(List<T> list, string propertyName, bool descending)
            => Sort(list, propertyName, descending, StringComparison.CurrentCulture);

        public void Sort(List<T> list, string propertyName, bool descending, StringComparison stringComparison)
        {
            // Apply and set the sort, if items to sort
            var pc = new PropertyComparer<T>(propertyName, descending, stringComparison);
            if (pc.IsValid) list.Sort(pc);
        }
    }
}
