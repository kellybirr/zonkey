using System;
using System.Data;
using Zonkey.ObjectModel;

namespace Zonkey.UnitTests.AdventureWorks.DataObjects
{
	[DataItem("Customer", SchemaName = "Sales")]
	public class Sales_Customer : DataClass
	{
		#region Data Columns (Properties)

		[DataField("CustomerID", DbType.Int32, false, IsKeyField = true, IsAutoIncrement = true)]
		public Int32 CustomerID
		{
			get { return _customerID; }
			set { SetFieldValue(ref _customerID, value); }
		}
		private Int32 _customerID;

		[DataField("PersonID", DbType.Int32, true)]
		public Int32? PersonID
		{
			get { return _personID; }
			set { SetFieldValue(ref _personID, value); }
		}
		private Int32? _personID;

		[DataField("StoreID", DbType.Int32, true)]
		public Int32? StoreID
		{
			get { return _storeID; }
			set { SetFieldValue(ref _storeID, value); }
		}
		private Int32? _storeID;

		[DataField("TerritoryID", DbType.Int32, true)]
		public Int32? TerritoryID
		{
			get { return _territoryID; }
			set { SetFieldValue(ref _territoryID, value); }
		}
		private Int32? _territoryID;

		[DataField("AccountNumber", DbType.String, false, Length = 10)]
		public string AccountNumber
		{
			get { return _accountNumber; }
			set { SetFieldValue(ref _accountNumber, value); }
		}
		private string _accountNumber;

		[DataField("rowguid", DbType.Guid, false)]
		public Guid rowguid
		{
			get { return _rowguid; }
			set { SetFieldValue(ref _rowguid, value); }
		}
		private Guid _rowguid;

		[DataField("ModifiedDate", DbType.DateTime, false)]
		public DateTime ModifiedDate
		{
			get { return _modifiedDate; }
			set { SetFieldValue(ref _modifiedDate, value); }
		}
		private DateTime _modifiedDate;

		#endregion

		#region Constructors

		public Sales_Customer(bool addingNew) : base(addingNew)
		{
			if (addingNew)
			{
			}
		}

		[Obsolete("This default constructor is required by the DataClassAdapter, but should never be used directly in code.", true)]
		public Sales_Customer() : this(false)
		{ }

		#endregion

	}

}
