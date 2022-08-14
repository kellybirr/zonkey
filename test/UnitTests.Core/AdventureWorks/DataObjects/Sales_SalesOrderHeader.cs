using System;
using System.Data;
using Zonkey.ObjectModel;

namespace Zonkey.UnitTests.AdventureWorks.DataObjects
{
	[DataItem("SalesOrderHeader", SchemaName = "Sales")]
	public class Sales_SalesOrderHeader : DataClass
	{
		#region Data Columns (Properties)

		[DataField("SalesOrderID", DbType.Int32, false, IsKeyField = true, IsAutoIncrement = true)]
		public Int32 SalesOrderID
		{
			get { return _salesOrderID; }
			set { SetFieldValue(ref _salesOrderID, value); }
		}
		private Int32 _salesOrderID;

		[DataField("RevisionNumber", DbType.Byte, false)]
		public Byte RevisionNumber
		{
			get { return _revisionNumber; }
			set { SetFieldValue(ref _revisionNumber, value); }
		}
		private Byte _revisionNumber;

		[DataField("OrderDate", DbType.DateTime, false)]
		public DateTime OrderDate
		{
			get { return _orderDate; }
			set { SetFieldValue(ref _orderDate, value); }
		}
		private DateTime _orderDate;

		[DataField("DueDate", DbType.DateTime, false)]
		public DateTime DueDate
		{
			get { return _dueDate; }
			set { SetFieldValue(ref _dueDate, value); }
		}
		private DateTime _dueDate;

		[DataField("ShipDate", DbType.DateTime, true)]
		public DateTime? ShipDate
		{
			get { return _shipDate; }
			set { SetFieldValue(ref _shipDate, value); }
		}
		private DateTime? _shipDate;

		[DataField("Status", DbType.Byte, false)]
		public Byte Status
		{
			get { return _status; }
			set { SetFieldValue(ref _status, value); }
		}
		private Byte _status;

		[DataField("OnlineOrderFlag", DbType.Boolean, false)]
		public bool OnlineOrderFlag
		{
			get { return _onlineOrderFlag; }
			set { SetFieldValue(ref _onlineOrderFlag, value); }
		}
		private bool _onlineOrderFlag;

		[DataField("SalesOrderNumber", DbType.String, false, Length = 25)]
		public string SalesOrderNumber
		{
			get { return _salesOrderNumber; }
			set { SetFieldValue(ref _salesOrderNumber, value); }
		}
		private string _salesOrderNumber;

		[DataField("PurchaseOrderNumber", DbType.String, true, Length = 25)]
		public string PurchaseOrderNumber
		{
			get { return _purchaseOrderNumber; }
			set { SetFieldValue(ref _purchaseOrderNumber, value); }
		}
		private string _purchaseOrderNumber;

		[DataField("AccountNumber", DbType.String, true, Length = 15)]
		public string AccountNumber
		{
			get { return _accountNumber; }
			set { SetFieldValue(ref _accountNumber, value); }
		}
		private string _accountNumber;

		[DataField("CustomerID", DbType.Int32, false)]
		public Int32 CustomerID
		{
			get { return _customerID; }
			set { SetFieldValue(ref _customerID, value); }
		}
		private Int32 _customerID;

		[DataField("SalesPersonID", DbType.Int32, true)]
		public Int32? SalesPersonID
		{
			get { return _salesPersonID; }
			set { SetFieldValue(ref _salesPersonID, value); }
		}
		private Int32? _salesPersonID;

		[DataField("TerritoryID", DbType.Int32, true)]
		public Int32? TerritoryID
		{
			get { return _territoryID; }
			set { SetFieldValue(ref _territoryID, value); }
		}
		private Int32? _territoryID;

		[DataField("BillToAddressID", DbType.Int32, false)]
		public Int32 BillToAddressID
		{
			get { return _billToAddressID; }
			set { SetFieldValue(ref _billToAddressID, value); }
		}
		private Int32 _billToAddressID;

		[DataField("ShipToAddressID", DbType.Int32, false)]
		public Int32 ShipToAddressID
		{
			get { return _shipToAddressID; }
			set { SetFieldValue(ref _shipToAddressID, value); }
		}
		private Int32 _shipToAddressID;

		[DataField("ShipMethodID", DbType.Int32, false)]
		public Int32 ShipMethodID
		{
			get { return _shipMethodID; }
			set { SetFieldValue(ref _shipMethodID, value); }
		}
		private Int32 _shipMethodID;

		[DataField("CreditCardID", DbType.Int32, true)]
		public Int32? CreditCardID
		{
			get { return _creditCardID; }
			set { SetFieldValue(ref _creditCardID, value); }
		}
		private Int32? _creditCardID;

		[DataField("CreditCardApprovalCode", DbType.String, true, Length = 15)]
		public string CreditCardApprovalCode
		{
			get { return _creditCardApprovalCode; }
			set { SetFieldValue(ref _creditCardApprovalCode, value); }
		}
		private string _creditCardApprovalCode;

		[DataField("CurrencyRateID", DbType.Int32, true)]
		public Int32? CurrencyRateID
		{
			get { return _currencyRateID; }
			set { SetFieldValue(ref _currencyRateID, value); }
		}
		private Int32? _currencyRateID;

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

		[DataField("Comment", DbType.String, true, Length = 128)]
		public string Comment
		{
			get { return _comment; }
			set { SetFieldValue(ref _comment, value); }
		}
		private string _comment;

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

		public Sales_SalesOrderHeader(bool addingNew) : base(addingNew)
		{
			if (addingNew)
			{
			}
		}

		[Obsolete("This default constructor is required by the DataClassAdapter, but should never be used directly in code.", true)]
		public Sales_SalesOrderHeader() : this(false)
		{ }

		#endregion

	}

}
