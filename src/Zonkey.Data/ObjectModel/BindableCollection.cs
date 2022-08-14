#if (NET48)
using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;

namespace Zonkey.ObjectModel
{
    /// <summary>
    /// Provides methods and properties for creating a bindable collection.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class BindableCollection<T> : BindingList<T>, ITrackDeletedItems<T>, IComponent, IListSource
        where T : DataClass, new()
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="T:BindableCollection"/> class.
        /// </summary>
        public BindableCollection()
        {
            _deletedItems = new List<T>();
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="T:BindableCollection"/> class.
        /// </summary>
        /// <param name="container">The container.</param>
        public BindableCollection(IContainer container)
        {
            container.Add(this);
            _deletedItems = new List<T>();
        }

        /// <summary>
        /// Gets the deleted items.
        /// </summary>
        /// <value>The deleted items.</value>
        public ICollection<T> DeletedItems
        {
            get { return _deletedItems; }
        }

        private readonly List<T> _deletedItems;

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
        /// Raises the <see cref="E:System.ComponentModel.BindingList`1.AddingNew"></see> event.
        /// </summary>
        /// <param name="e">An <see cref="T:System.ComponentModel.AddingNewEventArgs"></see> that contains the event data.</param>
        protected override void OnAddingNew(AddingNewEventArgs e)
        {
            if (e.NewObject == null)
            {
                object[] parms = {true}; // invoke adding-new constructor
                e.NewObject = Activator.CreateInstance(typeof (T), parms);
            }

            base.OnAddingNew(e);
        }

        /// <summary>
        /// Sorts the items if overridden in a derived class; otherwise, throws a <see cref="T:System.NotSupportedException"/>.
        /// </summary>
        /// <param name="prop">A <see cref="T:System.ComponentModel.PropertyDescriptor"/> that specifies the property to sort on.</param>
        /// <param name="direction">One of the <see cref="T:System.ComponentModel.ListSortDirection"/>  values.</param>
        /// <exception cref="T:System.NotSupportedException">Method is not overridden in a derived class. </exception>
        protected override void ApplySortCore(PropertyDescriptor prop, ListSortDirection direction)
        {
            _sortProperty = prop;
            _sortDirection = direction;

            // Get list to sort
            List<T> items = Items as List<T>;

            // Apply and set the sort, if items to sort
            if (items != null)
            {
                PropertyComparer<T> pc = new PropertyComparer<T>(prop, direction);
                items.Sort(pc);
                _isSorted = true;
            }
            else
            {
                _isSorted = false;
            }

            // Let bound controls know they should refresh their views
            OnListChanged(new ListChangedEventArgs(ListChangedType.Reset, -1));
        }

        /// <summary>
        /// Removes any sort applied with <see cref="M:System.ComponentModel.BindingList`1.ApplySortCore(System.ComponentModel.PropertyDescriptor,System.ComponentModel.ListSortDirection)"/> if sorting is implemented in a derived class; otherwise, raises <see cref="T:System.NotSupportedException"/>.
        /// </summary>
        /// <exception cref="T:System.NotSupportedException">Method is not overridden in a derived class. </exception>
        protected override void RemoveSortCore()
        {
            _isSorted = false;
            _sortProperty = null;
        }

        /// <summary>
        /// Gets a value indicating whether the list is sorted.
        /// </summary>
        /// <value></value>
        /// <returns>true if the list is sorted; otherwise, false. The default is false.</returns>
        protected override bool IsSortedCore
        {
            get { return _isSorted; }
        }

        private bool _isSorted;

        /// <summary>
        /// Gets the direction the list is sorted.
        /// </summary>
        /// <value></value>
        /// <returns>One of the <see cref="T:System.ComponentModel.ListSortDirection"/> values. The default is <see cref="F:System.ComponentModel.ListSortDirection.Ascending"/>. </returns>
        protected override ListSortDirection SortDirectionCore
        {
            get { return _sortDirection; }
        }

        private ListSortDirection _sortDirection;

        /// <summary>
        /// Gets the property descriptor that is used for sorting the list if sorting is implemented in a derived class; otherwise, returns null.
        /// </summary>
        /// <value></value>
        /// <returns>The <see cref="T:System.ComponentModel.PropertyDescriptor"/> used for sorting the list.</returns>
        protected override PropertyDescriptor SortPropertyCore
        {
            get { return _sortProperty; }
        }

        private PropertyDescriptor _sortProperty;

        /// <summary>
        /// Gets a value indicating whether the list supports sorting.
        /// </summary>
        /// <value></value>
        /// <returns>true if the list supports sorting; otherwise, false. The default is false.</returns>
        protected override bool SupportsSortingCore
        {
            get { return true; }
        }

        /// <summary>
        /// Deletes all.
        /// </summary>
        public void DeleteAll()
        {
            for (int i = Count - 1; i >= 0; i--)
                RemoveAt(i);
        }

        /// <summary>
        /// Causes items removed from collection to be treated as deletions
        /// </summary>
        public bool DeleteRemovedItems
        {
            get { return _deleteRemovedItems; }
            set { _deleteRemovedItems = value; }
        }
        private bool _deleteRemovedItems = true;

        /// <summary>
        /// Removes the item at the specified index.
        /// </summary>
        /// <param name="index">The zero-based index of the item to remove.</param>
        /// <exception cref="T:System.NotSupportedException">You are removing a newly added item and <see cref="P:System.ComponentModel.IBindingList.AllowRemove"></see> is set to false. </exception>
        protected override void RemoveItem(int index)
        {
            if (_deleteRemovedItems)
            {
                T item = this[index];
                switch (item.DataRowState)
                {
                    case DataRowState.Unchanged:
                    case DataRowState.Modified:
                        item.DataRowState = DataRowState.Deleted;
                        goto case DataRowState.Deleted;
                    case DataRowState.Deleted:
                        _deletedItems.Add(item);
                        break;
                }
            }

            base.RemoveItem(index);
        }

#region IComponent Members

        /// <summary>
        /// Represents the method that handles the <see cref="E:System.ComponentModel.IComponent.Disposed"></see> event of a component.
        /// </summary>
        [Browsable(false)]
        [EditorBrowsable(EditorBrowsableState.Advanced)]
        public event EventHandler Disposed;

        /// <summary>
        /// Gets or sets the <see cref="T:System.ComponentModel.ISite"></see> associated with the <see cref="T:System.ComponentModel.IComponent"></see>.
        /// </summary>
        /// <value></value>
        /// <returns>The <see cref="T:System.ComponentModel.ISite"></see> object associated with the component; or null, if the component does not have a site.</returns>
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        [Browsable(false)]
        public ISite Site
        {
            get { return _site; }
            set { _site = value; }
        }

        private ISite _site;

#endregion

#region IDisposable Members

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

#region IListSource Members

        /// <summary>
        /// Gets a value indicating whether the collection is a collection of <see cref="T:System.Collections.IList"></see> objects.
        /// </summary>
        /// <value></value>
        /// <returns>true if the collection is a collection of <see cref="T:System.Collections.IList"></see> objects; otherwise, false.</returns>
        public virtual bool ContainsListCollection
        {
            get { return false; }
        }

        /// <summary>
        /// Returns an <see cref="T:System.Collections.IList"></see> that can be bound to a data source from an object that does not implement an <see cref="T:System.Collections.IList"></see> itself.
        /// </summary>
        /// <returns>
        /// An <see cref="T:System.Collections.IList"></see> that can be bound to a data source from the object.
        /// </returns>
        public virtual IList GetList()
        {
            return this;
        }

#endregion
    }
}
#endif
