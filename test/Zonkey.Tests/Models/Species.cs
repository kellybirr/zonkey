using System.Data;
using Zonkey.ObjectModel;

namespace Zonkey.Tests.Models
{
    [DataItem("Species")]
    public class Species : DataClass
    {
        private int _speciesId;
        private string _name;
        private string _classification;
        private bool _isEndangered;

        public Species() : base(true) { }
        public Species(bool addingNew) : base(addingNew) { }

        [DataField("SpeciesId", DbType.Int32, IsKeyField = true, IsAutoIncrement = true)]
        public int SpeciesId
        {
            get => _speciesId;
            set => SetFieldValue(ref _speciesId, value);
        }

        [DataField("Name", DbType.String)]
        public string Name
        {
            get => _name;
            set => SetFieldValue(ref _name, value);
        }

        [DataField("Classification", DbType.String, true)]
        public string Classification
        {
            get => _classification;
            set => SetFieldValue(ref _classification, value);
        }

        [DataField("IsEndangered", DbType.Boolean)]
        public bool IsEndangered
        {
            get => _isEndangered;
            set => SetFieldValue(ref _isEndangered, value);
        }
    }
}
