using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace Zonkey.ObjectModel
{
    /// <summary>
    /// Provides methods for interacting with a collection of <see cref="Zonkey.ObjectModel.DataClass"/> items.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class DataClassCollection<T> : Collection<T>, IDisposable
        where T : DataClass, new()
    {
        /// <summary>
        /// Adds the collection.
        /// </summary>
        /// <param name="collection">The collection.</param>
        public void AddCollection(DataClassCollection<T> collection)
        {
            lock (this)
            {
                foreach (T item in collection)
                    Add(item);
            }
        }

        /// <summary>
        /// Selects items from the collection which satisfy the specified predicate.
        /// </summary>
        /// <param name="predicate">The predicate.</param>
        /// <returns>An array containing the selected items.</returns>
        public T[] Select(Predicate<T> predicate)
        {
            List<T> results = new List<T>();

            foreach (T item in this)
            {
                if (predicate(item))
                    results.Add(item);
            }

            return results.ToArray();
        }

        /// <summary>
        /// Sorts the collection based on a specified property name.
        /// </summary>
        /// <param name="propertyName">Name of the property.</param>
        /// <param name="descending">if set to <c>true</c>, then sorts the collection in descending order.</param>
        public void Sort(string propertyName, bool descending)
        {
            // Get list to sort
            var items = Items as List<T>;

            // Apply and set the sort, if items to sort
            if (items != null)
            {
                var pc = new PropertyComparer<T>(propertyName, descending);
                items.Sort(pc);
            }
        }

        /// <summary>
        /// Sorts the collection based on a specified lambda expression.
        /// </summary>
        /// <typeparam name="P"></typeparam>
        /// <param name="lambda">The lambda expression.</param>
        public void Sort<P>(SortFunc<T, P> lambda) 
            where P : IComparable<P>
        {
            // get sorter and sort
            var sorter = new ListSorter<T>();
            sorter.Sort(Items as List<T>, lambda, false);
        }

        /// <summary>
        /// Sorts the collection based on a specified lambda expression.
        /// </summary>
        /// <typeparam name="P"></typeparam>
        /// <param name="lambda">The lambda expression.</param>
        /// <param name="descending">if set to <c>true</c>, then sorts then collection in descending order.</param>
        public void Sort<P>(SortFunc<T, P> lambda, bool descending)
            where P : IComparable<P>
        {
            // get sorter and sort
            var sorter = new ListSorter<T>();
            sorter.Sort(Items as List<T>, lambda, descending);
        }

        #region IDisposable Members

        /// <summary>
        /// Occurs when Dispose() is called.
        /// </summary>
        public event EventHandler Disposed;

        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Releases unmanaged and - optionally - managed resources
        /// </summary>
        /// <param name="disposing"><c>true</c> to release both managed and unmanaged resources; <c>false</c> to release only unmanaged resources.</param>
        protected virtual void Dispose(bool disposing)
        {
            base.ClearItems();

            if ((disposing) && (Disposed != null))
                Disposed(this, EventArgs.Empty);
        }

        #endregion
    }


}