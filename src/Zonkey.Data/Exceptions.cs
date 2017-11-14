using System;
using System.Data.Common;
using System.Reflection;

namespace Zonkey
{
    /// <summary>
    /// Thrown when an update in insert method fails
    /// </summary>
    public class SaveFailedException : DbException
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="SaveFailedException"/> class.
        /// </summary>
        public SaveFailedException() { }

        /// <summary>
        /// Initializes a new instance with specified message
        /// </summary>
        /// <param name="result">The result of the failed save</param>
        public SaveFailedException(SaveResult result)
            : base(string.Format("The save appears to have failed for an unknown reason, or did not affect the expected number of records. [Records Affected: {0}]", result.RecordsAffected))
        {
            Result = result;
        }

        /// <summary>
        /// Initializes a new instance with specified message
        /// </summary>
        /// <param name="message">The exception message</param>
        public SaveFailedException(string message) : base(message) { }

        /// <summary>
        /// Initializes a new instance with specified message and inner exception
        /// </summary>
        /// <param name="message">The exception message</param>
        /// <param name="innerException">The inner exception</param>
        public SaveFailedException(string message, Exception innerException) : base(message, innerException) { }


        /// <summary>
        /// Gets the result of the failed save.
        /// </summary>
        /// <value>The result.</value>
        public SaveResult Result { get; }		
    }

    /// <summary>
    /// Thrown when an update method fails due to a data conflict
    /// </summary>	
    public class UpdateConflictException : DbException
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="UpdateConflictException"/> class.
        /// </summary>
        public UpdateConflictException() { }

        /// <summary>
        /// Initializes a new instance of the <see cref="UpdateConflictException"/> class.
        /// </summary>
        public UpdateConflictException(SaveResult result)
            : base(string.Format("A synchronization conflict was detected during the update. [Records Affected: {0}]", result.RecordsAffected))
        {
            Result = result;
        }

        /// <summary>
        /// Initializes a new instance with specified message
        /// </summary>
        /// <param name="message">The exception message</param>
        public UpdateConflictException(string message) : base(message) { }

        /// <summary>
        /// Initializes a new instance with specified message and inner exception
        /// </summary>
        /// <param name="message">The exception message</param>
        /// <param name="innerException">The inner exception</param>
        public UpdateConflictException(string message, Exception innerException) : base(message, innerException) { }

        /// <summary>
        /// Gets the result of the failed save.
        /// </summary>
        /// <value>The result.</value>
        public SaveResult Result { get; private set; }		
    }

    /// <summary>
    /// Thrown when an update method fails due to a data conflict
    /// </summary>
    public class CollectionSaveException<T> : DbException
    {
        private const string DEFAULT_ERROR_MESSAGE = "Errors occurred during the collection save";
        
        /// <summary>
        /// Gets or sets the results.
        /// </summary>
        /// <value>The results.</value>
        public CollectionSaveResult<T> Results { get; private set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="CollectionSaveException&lt;T&gt;"/> class.
        /// </summary>
        /// <param name="results">The results.</param>
        public CollectionSaveException(CollectionSaveResult<T> results) 
            : base(DEFAULT_ERROR_MESSAGE)
        {
            Results = results;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="CollectionSaveException&lt;T&gt;"/> class.
        /// </summary>
        /// <param name="results">The results.</param>
        /// <param name="innerException">The inner exception.</param>
        public CollectionSaveException(CollectionSaveResult<T> results, Exception innerException)
            : base(DEFAULT_ERROR_MESSAGE, innerException)
        {
            Results = results;
        }

    }

    /// <summary>
    /// Thrown when a record read fails on a property
    /// </summary>
    public class PropertyReadException : DbException
    {
        private const string DEFAULT_ERROR_MESSAGE = "Error reading property '{0}' of class '{1}'";

        /// <summary>
        /// Gets or sets the name of the property.
        /// </summary>
        /// <value>The name of the property.</value>
        public PropertyInfo Property { get; set; }

        /// <summary>
        /// Gets or sets the field value.
        /// </summary>
        /// <value>The field value.</value>
        public object FieldValue { get; set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="PropertyReadException"/> class.
        /// </summary>
        /// <param name="prop">The property being read.</param>
        /// <param name="value">The value being set.</param>
        /// <param name="innerException">The inner exception.</param>
        internal PropertyReadException(PropertyInfo prop, object value, Exception innerException)
            : base(string.Format(DEFAULT_ERROR_MESSAGE, prop.Name, prop.DeclaringType.FullName), innerException)
        {
            Property = prop;
            FieldValue = value;
        }

    }

    public class BulkInsertException : DbException
    {
        private const string DEFAULT_ERROR_MESSAGE = "Bulk Insert failed due to exception on record {0}, previous objects were inserted properly.";

        /// <summary>
        /// Initializes a new instance of the <see cref="BulkInsertException"/> class.
        /// </summary>
        /// <param name="recordNumber">The number of the failed record</param>
        public BulkInsertException(int recordNumber) 
            : base(string.Format(DEFAULT_ERROR_MESSAGE, recordNumber))
        { }

        /// <summary>
        /// Initializes a new instance of the <see cref="BulkInsertException"/> class.
        /// </summary>
        /// <param name="recordNumber">The number of the failed record</param>
        /// <param name="innerException">The inner exception.</param>
        public BulkInsertException(int recordNumber, Exception innerException)
            : base(string.Format(DEFAULT_ERROR_MESSAGE, recordNumber), innerException)
        { }
    }

    public class BulkUpdateException : DbException
    {
        private const string DEFAULT_ERROR_MESSAGE = "Bulk Update failed due to exception on record {0}, previous objects were updated properly.";

        /// <summary>
        /// Initializes a new instance of the <see cref="BulkInsertException"/> class.
        /// </summary>
        /// <param name="recordNumber">The number of the failed record</param>
        public BulkUpdateException(int recordNumber)
            : base(string.Format(DEFAULT_ERROR_MESSAGE, recordNumber))
        { }

        /// <summary>
        /// Initializes a new instance of the <see cref="BulkInsertException"/> class.
        /// </summary>
        /// <param name="recordNumber">The number of the failed record</param>
        /// <param name="innerException">The inner exception.</param>
        public BulkUpdateException(int recordNumber, Exception innerException)
            : base(string.Format(DEFAULT_ERROR_MESSAGE, recordNumber), innerException)
        { }
    }

    public class DeleteFailedException : DbException
    {
        public DeleteFailedException(Exception innerException) :
            base(innerException.Message, innerException)
        { }
    }
}
