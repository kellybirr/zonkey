using System;
using System.Xml.Serialization;

namespace Zonkey
{
    /// <summary>
    /// Provides methods and properties that describe a generic item in a DataList.
    /// </summary>
    /// <typeparam name="K"></typeparam>
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1715:IdentifiersShouldHaveCorrectPrefix", MessageId = "T")]
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", MessageId = "K")]
    public sealed class DataListItem<K> : IDataListItem
    {
        private K _Id;
        private string _Label;

        /// <summary>
        /// Gets or sets the id.
        /// </summary>
        /// <value>The id.</value>
        [XmlAttribute]
        public K Id
        {
            get { return _Id; }
            set { _Id = value; }
        }

        /// <summary>
        /// Gets or sets the id.
        /// </summary>
        /// <value>The id.</value>
        object IDataListItem.Id
        {
            get { return _Id; }
            set { _Id = (K)value; }
        }

        /// <summary>
        /// Gets or sets the label.
        /// </summary>
        /// <value>The label.</value>
        [XmlAttribute]
        public string Label
        {
            get { return _Label; }
            set { _Label = value; }
        }

        /// <summary>
        /// Returns a <see cref="T:System.String"></see> that represents the current <see cref="T:System.Object"></see>.
        /// </summary>
        /// <returns>
        /// A <see cref="T:System.String"></see> that represents the current <see cref="T:System.Object"></see>.
        /// </returns>
        public override string ToString()
        {
            return _Label;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="K:DataListItem"/> class.
        /// </summary>
        /// <param name="id">The id.</param>
        /// <param name="label">The label.</param>
        public DataListItem(K id, string label)
        {
            _Id = id;
            _Label = label;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="K:DataListItem"/> class.
        /// </summary>
        public DataListItem()
        {
        }
    }

    /// <summary>
    /// Provides methods and properties that describe an item in a DataList.
    /// </summary>
    /// <remarks>
    /// Provides methods for casting the Id to a <see cref="System.Int32"/> or <see cref="System.Guid"/>
    /// </remarks>
    public class DataListItem : IDataListItem
    {
        private object _Id;
        private string _Label;

        /// <summary>
        /// Gets or sets the id.
        /// </summary>
        /// <value>The id.</value>
        public object Id
        {
            get { return _Id; }
            set { _Id = value; }
        }

        /// <summary>
        /// Gets the int id.
        /// </summary>
        /// <value>The int id.</value>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1720:IdentifiersShouldNotContainTypeNames", MessageId = "int")]
        public int IntId
        {
            get { return (_Id is int) ? (int)_Id : -1; }
        }

        /// <summary>
        /// Gets the GUID id.
        /// </summary>
        /// <value>The GUID id.</value>
        public Guid GuidId
        {
            get { return (_Id is Guid) ? (Guid)_Id : Guid.Empty; }
        }

        /// <summary>
        /// Gets or sets the label.
        /// </summary>
        /// <value>The label.</value>
        public string Label
        {
            get { return _Label; }
            set { _Label = value; }
        }

        /// <summary>
        /// Returns a <see cref="T:System.String"></see> that represents the current <see cref="T:System.Object"></see>.
        /// </summary>
        /// <returns>
        /// A <see cref="T:System.String"></see> that represents the current <see cref="T:System.Object"></see>.
        /// </returns>
        public override string ToString()
        {
            return _Label;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="DataListItem"/> class.
        /// </summary>
        /// <param name="id">The id.</param>
        /// <param name="label">The label.</param>
        public DataListItem(object id, string label)
        {
            _Id = id;
            _Label = label;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="DataListItem"/> class.
        /// </summary>
        public DataListItem()
        {
        }
    }

    /// <summary>
    /// Provides an interface for describing a DataListItem.
    /// </summary>
    public interface IDataListItem
    {
        /// <summary>
        /// Gets or sets the id.
        /// </summary>
        /// <value>The id.</value>
        object Id { get; set; }

        /// <summary>
        /// Gets or sets the label.
        /// </summary>
        /// <value>The label.</value>
        string Label { get; set; }
    }
}