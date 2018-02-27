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
        /// <inheritdoc />
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
        public UpdateCriteria UpdateCriteria { get; set; }

        /// <inheritdoc />
        public SelectBack SelectBack { get; set; }

        /// <inheritdoc />
        public bool? UseQuotedIdentifier { get; set; }

        /// <inheritdoc />
        public bool ImplicitFieldDefinition { get; set; }

        /// <inheritdoc />
        public bool IncludePrivateProperties { get; set; }

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