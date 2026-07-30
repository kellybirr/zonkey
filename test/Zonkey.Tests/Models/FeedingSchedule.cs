using System;
using System.Data;
using Zonkey.ObjectModel;

namespace Zonkey.Tests.Models
{
    [DataItem("FeedingSchedule")]
    public class FeedingSchedule : DataClass
    {
        private int _animalId;
        private int _dayOfWeek;
        private string _timeSlot;
        private string _foodType;
        private decimal _quantity;
        private Guid? _assignedKeeperId;

        public FeedingSchedule() : base(true) { }
        public FeedingSchedule(bool addingNew) : base(addingNew) { }

        [DataField("AnimalId", DbType.Int32, IsKeyField = true)]
        public int AnimalId
        {
            get => _animalId;
            set => SetFieldValue(ref _animalId, value);
        }

        [DataField("DayOfWeek", DbType.Int32, IsKeyField = true)]
        public int DayOfWeek
        {
            get => _dayOfWeek;
            set => SetFieldValue(ref _dayOfWeek, value);
        }

        [DataField("TimeSlot", DbType.String, IsKeyField = true)]
        public string TimeSlot
        {
            get => _timeSlot;
            set => SetFieldValue(ref _timeSlot, value);
        }

        [DataField("FoodType", DbType.String)]
        public string FoodType
        {
            get => _foodType;
            set => SetFieldValue(ref _foodType, value);
        }

        [DataField("Quantity", DbType.Decimal)]
        public decimal Quantity
        {
            get => _quantity;
            set => SetFieldValue(ref _quantity, value);
        }

        [DataField("AssignedKeeperId", DbType.Guid, true)]
        public Guid? AssignedKeeperId
        {
            get => _assignedKeeperId;
            set => SetFieldValue(ref _assignedKeeperId, value);
        }
    }
}
