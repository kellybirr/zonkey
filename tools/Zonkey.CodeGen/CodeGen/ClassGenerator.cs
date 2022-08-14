using System;
using System.Collections.Generic;
using System.Data.Common;
using System.IO;

namespace ZonkeyCodeGen.CodeGen
{
    /// <summary>
    /// Base class for Zonkey Code Generators
    /// </summary>
    public abstract class ClassGenerator
    {
        /// <summary>
        /// Gets or sets a value indicating whether [private fields at top].
        /// </summary>
        /// <value><c>true</c> if [private fields at top]; otherwise, <c>false</c>.</value>
        public bool PrivateFieldsAtTop { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether to support C# nullable ref types.
        /// </summary>
        /// <value><c>true</c> if [cs nullable]; otherwise, <c>false</c>.</value>
        public bool CsNullable { get; set; }

        /// <summary>
        /// Gets or sets the tab level.
        /// </summary>
        /// <value>The tab level.</value>
        protected int TabLevel { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether [virtual properties].
        /// </summary>
        /// <value><c>true</c> if [virtual properties]; otherwise, <c>false</c>.</value>
        public bool VirtualProperties { get; set; }

        /// <summary>
        /// Gets or sets the name of the class.
        /// </summary>
        /// <value>The name of the class.</value>
        public string ClassName { get; set; }

        /// <summary>
        /// Gets or sets the namespace.
        /// </summary>
        /// <value>The namespace.</value>
        public string Namespace { get; set; }

        /// <summary>
        /// Gets or sets the connection.
        /// </summary>
        /// <value>The connection.</value>
        public DbConnection Connection { get; set; }

        /// <summary>
        /// Gets or sets the name of the table.
        /// </summary>
        /// <value>The name of the table.</value>
        public string TableName { get; set; }

        /// <summary>
        /// Gets or sets the name of the schema.
        /// </summary>
        /// <value>The name of the schema.</value>
        public string SchemaName { get; set; }

        /// <summary>
        /// The name of the Primary Key Field
        /// </summary>
        public List<string> KeyFieldName { get; set; }

        /// <summary>
        /// One or more lines of code to be added the the (addingnew) constructor
        /// </summary>
        public IList<string> AddConstructorCode
        {
            get 
            {
                if (_addConstructorCode == null)
                    _addConstructorCode = new List<string>();

                return _addConstructorCode; 
            }
        }
        private List<string> _addConstructorCode;

        /// <summary>
        /// Gets or sets the generate collections mode.
        /// </summary>
        /// <value>The generate collections mode.</value>
        public GenerateCollectionMode GenerateCollections { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether [generate typed adapters].
        /// </summary>
        /// <value>
        ///     <c>true</c> if [generate typed adapters]; otherwise, <c>false</c>.
        /// </value>
        public bool GenerateTypedAdapters { get; set; }

        /// <summary>
        /// Gets or sets the output.
        /// </summary>
        /// <value>The output.</value>
        public TextWriter Output
        {
            get { return _output; }
            set { _output = value; }
        }

        private TextWriter _output;

        protected ClassGenerator()
        {
            GenerateCollections = GenerateCollectionMode.DataClassCollection;
            VirtualProperties = true;
        }

        /// <summary>
        /// Writes the begin line.
        /// </summary>
        protected void WriteBeginLine()
        {
            _output.Write(new string('\t', TabLevel));
        }

        /// <summary>
        /// Writes the end line.
        /// </summary>
        protected void WriteEndLine()
        {
            _output.WriteLine();
        }

        /// <summary>
        /// Writes the line.
        /// </summary>
        /// <param name="format">The format.</param>
        /// <param name="args">The args.</param>
        protected void WriteLine(string format, params object[] args)
        {
            WriteBeginLine();
            _output.WriteLine(format, args);
        }

        /// <summary>
        /// Writes the line.
        /// </summary>
        /// <param name="s">The s.</param>
        protected void WriteLine(string s)
        {
            WriteBeginLine();
            _output.WriteLine(s);
        }

        /// <summary>
        /// Writes the specified format.
        /// </summary>
        /// <param name="format">The format.</param>
        /// <param name="args">The args.</param>
        protected void Write(string format, params object[] args)
        {
            _output.Write(format, args);
        }

        /// <summary>
        /// Writes the specified s.
        /// </summary>
        /// <param name="s">The s.</param>
        protected void Write(string s)
        {
            _output.Write(s);
        }

        public PropertyNameFormatter FormatPropertyName { get; set; } = (fn,cn) => fn;

        public NullableChecker NullableCheck { get; set; }

        public SequenceNameLookup SequenceNameFunc { get; set; }

        public DateTimeKindChecker DateTimeKindFunc { get; set; }

        /// <summary>
        /// Generates this instance.
        /// </summary>
        public abstract void Generate();
    }

    /// <summary>
    /// A delegate to handle formatting property names
    /// </summary>
    /// <param name="fieldName"></param>
    /// <param name="className"></param>
    /// <returns></returns>
    public delegate string PropertyNameFormatter(string fieldName, string className);

    /// <summary>
    /// A delegate to override checking for nullability of fields
    /// </summary>
    /// <param name="tableName">The name of the table</param>
    /// <param name="columnName">The name of the column</param>
    /// <returns></returns>
    public delegate bool NullableChecker(string tableName, string columnName);

    /// <summary>
    /// A delegate to lookup sequence names
    /// </summary>
    /// <param name="columnName"></param>
    /// <returns></returns>
    public delegate string SequenceNameLookup(string columnName);

    /// <summary>
    /// A delegate to check the Kind (Unspecified/Local/Utc) of a DateTime column
    /// </summary>
    /// <param name="tableName"></param>
    /// <param name="columnName"></param>
    /// <returns></returns>
    public delegate DateTimeKind DateTimeKindChecker(string tableName, string columnName);

    /// <summary>
    /// Enumeration that describes the inheritance of a <see cref="Zonkey.ObjectModel.DataClass"/> collection.
    /// </summary>
    public enum GenerateCollectionMode
    {
        /// <summary>
        /// No inheritance
        /// </summary>
        None = 0,
        /// <summary>
        /// Inherits from Collection
        /// </summary>
        GenericCollection = 1,
        /// <summary>
        /// Inherits from DataClassCollection
        /// </summary>
        DataClassCollection = 2,
        /// <summary>
        /// Inherits from BindableCollection
        /// </summary>
        BindableCollection = 3
    }
}
