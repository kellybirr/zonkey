using System.Data;
using Zonkey.ObjectModel;

namespace Zonkey.Tests.Models
{
    [DataItem("RenamedFieldTarget")]
    public class RenamedFieldTarget : DataClass
    {
        private int _id;
        private bool _active;

        public RenamedFieldTarget() : base(true) { }
        public RenamedFieldTarget(bool addingNew) : base(addingNew) { }

        [DataField("record_id", DbType.Int32, IsKeyField = true)]
        public int SpeciesId
        {
            get => _id;
            set => SetFieldValue(ref _id, value);
        }

        [DataField("is_active", DbType.Boolean)]
        public bool Active
        {
            get => _active;
            set => SetFieldValue(ref _active, value);
        }
    }
}
