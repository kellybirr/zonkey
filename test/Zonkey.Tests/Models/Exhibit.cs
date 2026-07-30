using System.Data;
using Zonkey.ObjectModel;

namespace Zonkey.Tests.Models
{
    [DataItem("Exhibit")]
    public class Exhibit : DataClass
    {
        private int _exhibitId;
        private string _name;
        private string _location;
        private int _capacity;
        private bool _isOpen;
        private byte[] _rowVersion;

        public Exhibit() : base(true) { }
        public Exhibit(bool addingNew) : base(addingNew) { }

        [DataField("ExhibitId", DbType.Int32, IsKeyField = true, IsAutoIncrement = true)]
        public int ExhibitId
        {
            get => _exhibitId;
            set => SetFieldValue(ref _exhibitId, value);
        }

        [DataField("Name", DbType.String)]
        public string Name
        {
            get => _name;
            set => SetFieldValue(ref _name, value);
        }

        [DataField("Location", DbType.String, true)]
        public string Location
        {
            get => _location;
            set => SetFieldValue(ref _location, value);
        }

        [DataField("Capacity", DbType.Int32)]
        public int Capacity
        {
            get => _capacity;
            set => SetFieldValue(ref _capacity, value);
        }

        [DataField("IsOpen", DbType.Boolean)]
        public bool IsOpen
        {
            get => _isOpen;
            set => SetFieldValue(ref _isOpen, value);
        }

        [DataField("RowVersion", DbType.Binary, IsRowVersion = true)]
        public byte[] RowVersion
        {
            get => _rowVersion;
            set => SetFieldValue(ref _rowVersion, value);
        }
    }
}
