namespace Zonkey.ObjectModel
{
    /// <summary>
    /// 
    /// A manually created data map item
    /// </summary>
    public sealed class DataMapItem : IDataMapItem
    {
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

        /// <summary>
        /// Gets or sets the name of the schema.
        /// </summary>
        /// <value>The name of the schema.</value>
        public string SchemaName { get; set; }

        /// <summary>
        /// Gets the name of the table.
        /// </summary>
        /// <value>The name of the table.</value>
        public string TableName { get; set; }

        /// <summary>
        /// Gets or sets the save to table.
        /// </summary>
        /// <value>The save to table.</value>
        public string SaveToTable
        {
            get { return (_saveToTable ?? TableName); }
            set { _saveToTable = value; }
        }

        private string _saveToTable;


        /// <summary>
        /// Gets or sets the type of the access.
        /// </summary>
        /// <value>The type of the access.</value>
        public AccessType AccessType { get; set; }

        /// <summary>
        /// Gets or sets the update criteria.
        /// </summary>
        /// <value>The update criteria.</value>
        public UpdateCriteria UpdateCriteria
        {
            get { return _updateCriteria; }
            set { _updateCriteria = value; }
        }

        private UpdateCriteria _updateCriteria = UpdateCriteria.Default;

        /// <summary>
        /// Gets or sets the select back.
        /// </summary>
        /// <value>The select back.</value>
        public SelectBack SelectBack { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether [use quoted identifier].
        /// </summary>
        /// <value><c>true</c> if [use quoted identifier]; otherwise, <c>false</c>.</value>
        public bool? UseQuotedIdentifier { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether fields are implicitly defined.
        /// </summary>
        /// <value><c>true</c> if [implicit field definition]; otherwise, <c>false</c>.</value>
        public bool ImplicitFieldDefinition { get; set; }
    }
}