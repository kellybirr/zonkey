using System;
using System.Data;
using Zonkey.ObjectModel;

namespace Zonkey.UnitTests.AdventureWorks.DataObjects
{
	[DataItem("Vendor", SchemaName = "Purchasing")]
	public class Purchasing_Vendor : DataClass<int>
	{
		#region Data Columns (Properties)

		[DataField("BusinessEntityID", DbType.Int32, false, IsKeyField = true)]
		public Int32 BusinessEntityID
		{
			get { return _businessEntityID; }
			set { SetFieldValue(ref _businessEntityID, value); }
		}
		private Int32 _businessEntityID;

		[DataField("AccountNumber", DbType.String, false, Length = 15)]
		public string AccountNumber
		{
			get { return _accountNumber; }
			set { SetFieldValue(ref _accountNumber, value); }
		}
		private string _accountNumber;

		[DataField("Name", DbType.String, false, Length = 50)]
		public string Name
		{
			get { return _name; }
			set { SetFieldValue(ref _name, value); }
		}
		private string _name;

		[DataField("CreditRating", DbType.Byte, false)]
		public Byte CreditRating
		{
			get { return _creditRating; }
			set { SetFieldValue(ref _creditRating, value); }
		}
		private Byte _creditRating;

		[DataField("PreferredVendorStatus", DbType.Boolean, false)]
		public bool PreferredVendorStatus
		{
			get { return _preferredVendorStatus; }
			set { SetFieldValue(ref _preferredVendorStatus, value); }
		}
		private bool _preferredVendorStatus;

		[DataField("ActiveFlag", DbType.Boolean, false)]
		public bool ActiveFlag
		{
			get { return _activeFlag; }
			set { SetFieldValue(ref _activeFlag, value); }
		}
		private bool _activeFlag;

		[DataField("PurchasingWebServiceURL", DbType.String, true, Length = 1024)]
		public string PurchasingWebServiceURL
		{
			get { return _purchasingWebServiceURL; }
			set { SetFieldValue(ref _purchasingWebServiceURL, value); }
		}
		private string _purchasingWebServiceURL;

		[DataField("ModifiedDate", DbType.DateTime, false)]
		public DateTime ModifiedDate
		{
			get { return _modifiedDate; }
			set { SetFieldValue(ref _modifiedDate, value); }
		}
		private DateTime _modifiedDate;

		#endregion

		#region Constructors

		public Purchasing_Vendor(bool addingNew) : base(addingNew)
		{
			if (addingNew)
			{
			}
		}

		[Obsolete("This default constructor is required by the DataClassAdapter, but should never be used directly in code.", true)]
		public Purchasing_Vendor() : this(false)
		{ }

		#endregion

	}

}
