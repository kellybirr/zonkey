using System;
using System.Data;
using Zonkey.ObjectModel;

namespace Zonkey.Tests.Models
{
    [DataItem("Animal")]
    public class Animal : DataClass
    {
        private int _animalId;
        private string _name;
        private int _speciesId;
        private int? _exhibitId;
        private Guid _zookeeperId;
        private DateTime? _dateOfBirth;
        private decimal? _weight;
        private string _notes;

        public Animal() : base(true) { }
        public Animal(bool addingNew) : base(addingNew) { }

        [DataField("AnimalId", DbType.Int32, IsKeyField = true, IsAutoIncrement = true)]
        public int AnimalId
        {
            get => _animalId;
            set => SetFieldValue(ref _animalId, value);
        }

        [DataField("Name", DbType.String)]
        public string Name
        {
            get => _name;
            set => SetFieldValue(ref _name, value);
        }

        [DataField("SpeciesId", DbType.Int32)]
        public int SpeciesId
        {
            get => _speciesId;
            set => SetFieldValue(ref _speciesId, value);
        }

        [DataField("ExhibitId", DbType.Int32, true)]
        public int? ExhibitId
        {
            get => _exhibitId;
            set => SetFieldValue(ref _exhibitId, value);
        }

        [DataField("ZookeeperId", DbType.Guid)]
        public Guid ZookeeperId
        {
            get => _zookeeperId;
            set => SetFieldValue(ref _zookeeperId, value);
        }

        [DataField("DateOfBirth", DbType.DateTime, true)]
        public DateTime? DateOfBirth
        {
            get => _dateOfBirth;
            set => SetFieldValue(ref _dateOfBirth, value);
        }

        [DataField("Weight", DbType.Decimal, true)]
        public decimal? Weight
        {
            get => _weight;
            set => SetFieldValue(ref _weight, value);
        }

        [DataField("Notes", DbType.String, true, IsComparable = false)]
        public string Notes
        {
            get => _notes;
            set => SetFieldValue(ref _notes, value);
        }
    }
}
