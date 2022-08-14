using System;
using System.Data;
using Zonkey.ObjectModel;

namespace Zonkey.UnitTests.AdventureWorks.DataObjects
{
	[DataItem("Location", SchemaName = "Production")]
	public class Production_Location : DataClass
	{
		#region Data Columns (Properties)

		[DataField("LocationID", DbType.Int16, false, IsKeyField = true, IsAutoIncrement = true)]
		public Int16 LocationID
		{
			get { return _locationID; }
			set { SetFieldValue(ref _locationID, value); }
		}
		private Int16 _locationID;

		[DataField("Name", DbType.String, false, Length = 50)]
		public string Name
		{
			get { return _name; }
			set { SetFieldValue(ref _name, value); }
		}
		private string _name;

		[DataField("CostRate", DbType.Decimal, false)]
		public decimal CostRate
		{
			get { return _costRate; }
			set { SetFieldValue(ref _costRate, value); }
		}
		private decimal _costRate;

		[DataField("Availability", DbType.Decimal, false)]
		public decimal Availability
		{
			get { return _availability; }
			set { SetFieldValue(ref _availability, value); }
		}
		private decimal _availability;

		[DataField("ModifiedDate", DbType.DateTime, false)]
		public DateTime ModifiedDate
		{
			get { return _modifiedDate; }
			set { SetFieldValue(ref _modifiedDate, value); }
		}
		private DateTime _modifiedDate;

		#endregion

		#region Constructors

		public Production_Location(bool addingNew) : base(addingNew)
		{
			if (addingNew)
			{
			}
		}

		[Obsolete("This default constructor is required by the DataClassAdapter, but should never be used directly in code.", true)]
		public Production_Location() : this(false)
		{ }

		#endregion

	}

}
