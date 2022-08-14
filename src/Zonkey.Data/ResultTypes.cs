using System;
using System.Collections.Generic;

namespace Zonkey
{
    /// <summary>
    /// Represents the result of a Save, Insert or Update Operation
    /// </summary>
    public class SaveResult
    {
        internal SaveResult(SaveResultStatus status, SaveType saveType, int recordCount = 0)
        {
            Status = status;
            SaveType = saveType;
            RecordsAffected = recordCount;
        }

        /// <summary>
        /// Was the save successful
        /// </summary>
        public bool Success
        {
            get { return (Status == SaveResultStatus.Success); }
        }

        /// <summary>
        /// Gets the status.
        /// </summary>
        /// <value>The status.</value>
        public SaveResultStatus Status { get; private set; }

        /// <summary>
        /// Gets the records affected.
        /// </summary>
        /// <value>The records affected.</value>
        public int RecordsAffected { get; internal set; }

        /// <summary>
        /// Gets the type of the save.
        /// </summary>
        /// <value>The type of the save.</value>
        public SaveType SaveType { get; private set; }
    }

    /// <summary>
    /// Provides methods and properties for storing and retrieving <see cref="SaveResultStatus"/> status when performing database operations on a collection.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class CollectionSaveResult<T>
    {
        /// <summary>
        /// Gets the items in the collection that were skipped.
        /// </summary>
        /// <value>The list of skipped items.</value>
        public IList<T> Skipped
        {
            get { return _skipped; }
        }

        private readonly List<T> _skipped = new List<T>();

        /// <summary>
        /// Gets the items in the collection that were inserted.
        /// </summary>
        /// <value>The list of inserted items.</value>
        public IList<T> Inserted
        {
            get { return _inserted; }
        }

        private readonly List<T> _inserted = new List<T>();

        /// <summary>
        /// Gets the items in the collection that were updated.
        /// </summary>
        /// <value>The list of updated items.</value>
        public IList<T> Updated
        {
            get { return _updated; }
        }

        private readonly List<T> _updated = new List<T>();

        /// <summary>
        /// Gets the items in the collection that were deleted.
        /// </summary>
        /// <value>The list of deleted items.</value>
        public IList<T> Deleted
        {
            get { return _deleted; }
        }

        private readonly List<T> _deleted = new List<T>();

        /// <summary>
        /// Gets the items in the collection that failed.
        /// </summary>
        /// <value>The list of failed items.</value>
        public IList<T> Failed
        {
            get { return _failed; }
        }

        private readonly List<T> _failed = new List<T>();

        /// <summary>
        /// Gets the items in the collection that conflicted.
        /// </summary>
        /// <value>The list of conflicted items.</value>
        public IList<T> Conflicted
        {
            get { return _conflicted; }
        }

        private readonly List<T> _conflicted = new List<T>();

        /// <summary>
        /// Gets the items in the collection that conflicted.
        /// </summary>
        /// <value>The list of conflicted items.</value>
        public IList<CollectionSaveExceptionItem<T>> Exceptions
        {
            get { return _exceptions; }
        }

        private readonly IList<CollectionSaveExceptionItem<T>> _exceptions = new List<CollectionSaveExceptionItem<T>>();

        /// <summary>
        /// Gets the number of errors that occurred when performing the database operation on the collection.
        /// </summary>
        /// <value>The error count.</value>
        public int ErrorCount
        {
            get { return _failed.Count + _conflicted.Count + _exceptions.Count; }
        }
    }

    /// <summary>
    /// A failed item from a colleciton save
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class CollectionSaveExceptionItem<T>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="CollectionSaveExceptionItem&lt;T&gt;"/> class.
        /// </summary>
        /// <param name="item">The item.</param>
        /// <param name="ex">The ex.</param>
        public CollectionSaveExceptionItem(T item, Exception ex)
        {
            Item = item;
            Exception = ex;
        }

        /// <summary>
        /// Gets or sets the item.
        /// </summary>
        /// <value>The item.</value>
        public T Item { get; private set; }

        /// <summary>
        /// Gets or sets the exception.
        /// </summary>
        /// <value>The exception.</value>
        public Exception Exception { get; private set; }
    }

}
