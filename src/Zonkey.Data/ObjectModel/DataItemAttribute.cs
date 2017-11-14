using System;
using System.Reflection;

namespace Zonkey.ObjectModel
{
    /// <summary>
    /// 
    /// </summary>
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1019:DefineAccessorsForAttributeArguments")]
    [AttributeUsage(AttributeTargets.Class, Inherited = true, AllowMultiple = false)]
    public sealed class DataItemAttribute : Attribute, IDataMapItem
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="DataItemAttribute"/> class.
        /// </summary>
        /// <param name="tableName">Name of the table.</param>
        public DataItemAttribute(string tableName)
        {
            TableName = tableName;
            UpdateCriteria = UpdateCriteria.Default;
            SelectBack = SelectBack.Default;
            AccessType = AccessType.ReadWrite;
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
        public UpdateCriteria UpdateCriteria { get; set; }

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

        /// <summary>
        /// Gets from type or member.
        /// </summary>
        /// <param name="type">The type.</param>
        /// <returns></returns>
        public static DataItemAttribute GetFromType(Type type)
        {
            return (DataItemAttribute)type.GetTypeInfo().GetCustomAttribute(typeof(DataItemAttribute));
        }

        /// <summary>
        /// Gets from type or member.
        /// </summary>
        /// <param name="mi">The mi.</param>
        /// <returns></returns>
        public static DataItemAttribute GetFromMember(MemberInfo mi)
        {
            return (DataItemAttribute)mi.GetCustomAttribute(typeof(DataItemAttribute));
        }
    }
}