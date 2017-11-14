using System;
using System.Data;
using Zonkey.ObjectModel;

namespace Zonkey.UnitTests.AdventureWorks.DataObjects
{
	[DataItem("PurchaseOrderHeader", SchemaName = "Purchasing")]
	public class Purchasing_PurchaseOrderHeader : DataClass
	{
		#region Data Columns (Properties)

		[DataField("PurchaseOrderID", DbType.Int32, false, IsKeyField = true, IsAutoIncrement = true)]
		public Int32 PurchaseOrderID
		{
			get { return _purchaseOrderID; }
			set { SetFieldValue(ref _purchaseOrderID, value); }
		}
		private Int32 _purchaseOrderID;

		[DataField("RevisionNumber", DbType.Byte, false)]
		public Byte RevisionNumber
		{
			get { return _revisionNumber; }
			set { SetFieldValue(ref _revisionNumber, value); }
		}
		private Byte _revisionNumber;

		[DataField("Status", DbType.Byte, false)]
		public Byte Status
		{
			get { return _status; }
			set { SetFieldValue(ref _status, value); }
		}
		private Byte _status;

		[DataField("EmployeeID", DbType.Int32, false)]
		public Int32 EmployeeID
		{
			get { return _employeeID; }
			set { SetFieldValue(ref _employeeID, value); }
		}
		private Int32 _employeeID;

		[DataField("VendorID", DbType.Int32, false)]
		public Int32 VendorID
		{
			get { return _vendorID; }
			set { SetFieldValue(ref _vendorID, value); }
		}
		private Int32 _vendorID;

		[DataField("ShipMethodID", DbType.Int32, false)]
		public Int32 ShipMethodID
		{
			get { return _shipMethodID; }
			set { SetFieldValue(ref _shipMethodID, value); }
		}
		private Int32 _shipMethodID;

		[DataField("OrderDate", DbType.DateTime, false)]
		public DateTime OrderDate
		{
			get { return _orderDate; }
			set { SetFieldValue(ref _orderDate, value); }
		}
		private DateTime _orderDate;

		[DataField("ShipDate", DbType.DateTime, true)]
		public DateTime? ShipDate
		{
			get { return _shipDate; }
			set { SetFieldValue(ref _shipDate, value); }
		}
		private DateTime? _shipDate;

		[DataField("SubTotal", DbType.Decimal, false)]
		public decimal SubTotal
		{
			get { return _subTotal; }
			set { SetFieldValue(ref _subTotal, value); }
		}
		private decimal _subTotal;

		[DataField("TaxAmt", DbType.Decimal, false)]
		public decimal TaxAmt
		{
			get { return _taxAmt; }
			set { SetFieldValue(ref _taxAmt, value); }
		}
		private decimal _taxAmt;

		[DataField("Freight", DbType.Decimal, false)]
		public decimal Freight
		{
			get { return _freight; }
			set { SetFieldValue(ref _freight, value); }
		}
		private decimal _freight;

		[DataField("TotalDue", DbType.Decimal, false)]
		public decimal TotalDue
		{
			get { return _totalDue; }
			set { SetFieldValue(ref _totalDue, value); }
		}
		private decimal _totalDue;

		[DataField("ModifiedDate", DbType.DateTime, false)]
		public DateTime ModifiedDate
		{
			get { return _modifiedDate; }
			set { SetFieldValue(ref _modifiedDate, value); }
		}
		private DateTime _modifiedDate;

		#endregion

		#region Constructors

		public Purchasing_PurchaseOrderHeader(bool addingNew) : base(addingNew)
		{
			if (addingNew)
			{
			}
		}

		[Obsolete("This default constructor is required by the DataClassAdapter, but should never be used directly in code.", true)]
		public Purchasing_PurchaseOrderHeader() : this(false)
		{ }

		#endregion

	}

}
