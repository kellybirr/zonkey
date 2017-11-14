using System;

namespace Zonkey
{
    /// <summary>
    /// Enumeration describing database access type
    /// </summary>
    public enum AccessType
    {
        /// <summary>
        /// Read Only
        /// </summary>
        ReadOnly = 0,
        /// <summary>
        /// Write Only
        /// </summary>
        WriteOnly = 1,
        /// <summary>
        /// Read and Write
        /// </summary>
        ReadWrite = 2
    }

    /// <summary>
    ///
    /// </summary>
    public enum UpdateCriteria
    {
        /// <summary>
        ///
        /// </summary>
        Default = -1,
        /// <summary>
        ///
        /// </summary>
        KeyOnly = 0,
        /// <summary>
        ///
        /// </summary>
        KeyAndVersion = 1,
        /// <summary>
        ///
        /// </summary>
        ChangedFields = 2,
        /// <summary>
        ///
        /// </summary>
        AllFields = 3
    }

    /// <summary>
    /// Enumeration describing which fields to modify during an UPDATE
    /// </summary>
    public enum UpdateAffect
    {
        /// <summary>
        /// Only fields with changed values
        /// </summary>
        ChangedFields = 0,
        /// <summary>
        /// All fields
        /// </summary>
        AllFields = 1
    }

    /// <summary>
    /// Enumeration describing a return value for an UPDATE/INSERT into the database.
    /// </summary>
    public enum SaveResultStatus
    {
        /// <summary>
        /// Item was skipped
        /// </summary>
        Skipped = 0,
        /// <summary>
        /// Item was successfully updated/inserted
        /// </summary>
        Success = 1,
        /// <summary>
        /// Item is in a conflicted state
        /// </summary>
        Conflict = 2,
        /// <summary>
        /// Item failed update/insert operation
        /// </summary>
        Fail = 3
    }

    /// <summary>
    /// Enumeration describing rows to select-back after performing an update/insert operation
    /// </summary>
    public enum SelectBack
    {
        /// <summary>
        /// Default is to select-back unchanged fields.
        /// </summary>
        Default = -1,
        /// <summary>
        /// Do not select-back any fields
        /// </summary>
        None = 0,
        /// <summary>
        /// Select-back identity or version fields
        /// </summary>
        IdentityOrVersion = 1,
        /// <summary>
        /// Select-back unchanged fields
        /// </summary>
        UnchangedFields = 2,
        /// <summary>
        /// Select-back all fields
        /// </summary>
        AllFields = 3
    }

    /// <summary>
    /// The Threading Mode for async operations
    /// </summary>
    public enum ThreadMode
    {
        /// <summary>
        /// Use the thread pool
        /// </summary>
        Pooled,
        /// <summary>
        /// Use a stand-alone thread
        /// </summary>
        Standalone,
        /// <summary>
        /// USe a stand-alone thread with a low thread prioroty
        /// </summary>
        LowPriority
    }

    internal enum FillMethod
    {
        Unfiltered,
        FilterText,
        FilterArray,
        StoredProcedure,
        LinqExpression
    }

    /// <summary>
    /// Enumeration describing a non-public property for advanced scenarios.
    /// </summary>
    public enum AdapterProperty
    {
        /// <summary>
        /// Name of the database table
        /// </summary>
        TableName,
        /// <summary>
        /// Name of the database table to perform save operations on.
        /// </summary>
        SaveToTable,
        /// <summary>
        /// Should identifiers be quoted by default
        /// </summary>
        UseQuotedIdentifiers
    }

    /// <summary>
    /// Enumeration describing the default behavoir of quoted identifiers settings
    /// </summary>
    public enum QuotedIdentifiers
    {
        /// <summary>
        /// Not Supported
        /// </summary>
        NotSupported,
        /// <summary>
        /// Supported, only if directly indicated
        /// </summary>
        Optional,
        /// <summary>
        /// Used by default unless specified otherwise
        /// </summary>
        Preferred,
    }

    /// <summary>
    /// Enumeration describing the type of save to be preformed
    /// </summary>
    public enum SaveType
    {
        /// <summary>
        /// Record is not being saved
        /// </summary>
        None,
        /// <summary>
        /// Record is to be Inserted
        /// </summary>
        Insert,
        /// <summary>
        /// Record is to be Updated
        /// </summary>
        Update
    }
}