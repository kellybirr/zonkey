namespace Zonkey.ObjectModel
{
    /// <inheritdoc />
    /// <summary>
    /// A manually created data map item
    /// </summary>
    public sealed class DataMapItem : IDataMapItem
    {
        /// <inheritdoc />
        /// <summary>
        /// Initializes a new instance of the <see cref="DataItemAttribute"/> class.
        /// </summary>
        /// <param name="tableName">Name of the table.</param>
        public DataMapItem(string tableName) : this(tableName, false) { }

        /// <summary>
        /// Initializes a new instance of the <see cref="DataMapItem"/> class.
        /// </summary>
        /// <param name="tableName">Name of the table.</param>
        /// <param name="implicitFields">if set to <c>true</c> [implicit fields].</param>
        public DataMapItem(string tableName, bool implicitFields)
        {
            TableName = tableName;
            AccessType = AccessType.ReadWrite;
            SelectBack = SelectBack.Default;
            ImplicitFieldDefinition = implicitFields;
        }

        /// <inheritdoc />
        public string SchemaName { get; set; }

        /// <inheritdoc />
        public string TableName { get; set; }

        /// <inheritdoc />
        public string SaveToTable
        {
            get => (_saveToTable ?? TableName);
            set => _saveToTable = value;
        }

        private string _saveToTable;


        /// <inheritdoc />
        public AccessType AccessType { get; set; }

        /// <inheritdoc />
        public UpdateCriteria UpdateCriteria { get; set; } = UpdateCriteria.Default;

        /// <inheritdoc />
        public SelectBack SelectBack { get; set; }

        /// <inheritdoc />
        public bool? UseQuotedIdentifier { get; set; }

        /// <inheritdoc />
        public bool ImplicitFieldDefinition { get; set; }

        /// <inheritdoc />
        public bool IncludePrivateProperties { get; set; }
    }
}