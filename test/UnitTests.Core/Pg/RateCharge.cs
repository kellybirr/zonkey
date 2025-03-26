#if (NET6_0_OR_GREATER)
#nullable enable

using System;
using System.Data;
//using NpgsqlTypes;
using Zonkey.ObjectModel;

namespace Zonkey.UnitTests.Pg
{
    [DataItem("rate_charges", SchemaName = "public")]
    public class RateCharge : DataClass<int>
    {
        [DataField("id", DbType.Int64, false, IsKeyField = true)]
        public long Id
        {
            get => _id;
            set => SetFieldValue(ref _id, value);
        }
        private long _id;

        [DataField("amount", DbType.Int64, false)]
        public Int32 Amount
        {
            get => _amount;
            set => SetFieldValue(ref _amount, value);
        }
        private Int32 _amount;

        [DataField("time_end", DbType.Time, true)]
        public TimeOnly? TimeEnd
        {
            get => _timeEnd;
            set => SetFieldValue(ref _timeEnd, value);
        }
        private TimeOnly? _timeEnd;

        [DataField("action", DbType.String, true)]
        public string? Action
        {
            get => _action;
            set => SetFieldValue(ref _action, value);
        }
        private string? _action;

        [DataField("cooldown", DbType.Object, true)]
        public TimeSpan? Cooldown
        {
            get => _cooldown;
            set => SetFieldValue(ref _cooldown, value);
        }
        private TimeSpan? _cooldown;

        #region Constructors

        public RateCharge(bool addingNew) : base(addingNew)
        {
            if (addingNew)
            {
            }
        }

        [Obsolete("This default constructor is required by the DataClassAdapter, but should never be used directly in code.", true)]
        public RateCharge() : this(false)
        { }

        #endregion
    }
}
#endif