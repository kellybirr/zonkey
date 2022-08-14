using System;
using System.Data;
using Zonkey.ObjectModel;

namespace Zonkey.UnitTests.AdventureWorks.DataObjects
{
	[DataItem("EmployeePayHistory", SchemaName = "HumanResources")]
	public class HumanResources_EmployeePayHistory : DataClass
	{
		#region Data Columns (Properties)

		[DataField("BusinessEntityID", DbType.Int32, false, IsKeyField = true)]
		public Int32 BusinessEntityID
		{
			get { return _businessEntityID; }
			set { SetFieldValue(ref _businessEntityID, value); }
		}
		private Int32 _businessEntityID;

		[DataField("RateChangeDate", DbType.DateTime, false, IsKeyField = true)]
		public DateTime RateChangeDate
		{
			get { return _rateChangeDate; }
			set { SetFieldValue(ref _rateChangeDate, value); }
		}
		private DateTime _rateChangeDate;

		[DataField("Rate", DbType.Decimal, false)]
		public decimal Rate
		{
			get { return _rate; }
			set { SetFieldValue(ref _rate, value); }
		}
		private decimal _rate;

		[DataField("PayFrequency", DbType.Byte, false)]
		public Byte PayFrequency
		{
			get { return _payFrequency; }
			set { SetFieldValue(ref _payFrequency, value); }
		}
		private Byte _payFrequency;

		[DataField("ModifiedDate", DbType.DateTime, false)]
		public DateTime ModifiedDate
		{
			get { return _modifiedDate; }
			set { SetFieldValue(ref _modifiedDate, value); }
		}
		private DateTime _modifiedDate;

		#endregion

		#region Constructors

		public HumanResources_EmployeePayHistory(bool addingNew) : base(addingNew)
		{
			if (addingNew)
			{
			}
		}

		[Obsolete("This default constructor is required by the DataClassAdapter, but should never be used directly in code.", true)]
		public HumanResources_EmployeePayHistory() : this(false)
		{ }

		#endregion

	}

}
