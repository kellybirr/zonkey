using System;
using System.Data;
using Zonkey.ObjectModel;

namespace Zonkey.UnitTests.AdventureWorks.DataObjects
{
	[DataItem("SalesOrderDetail", SchemaName = "Sales")]
	public class Sales_SalesOrderDetail : DataClass
	{
		#region Data Columns (Properties)

		[DataField("SalesOrderID", DbType.Int32, false, IsKeyField = true)]
		public Int32 SalesOrderID
		{
			get { return _salesOrderID; }
			set { SetFieldValue(ref _salesOrderID, value); }
		}
		private Int32 _salesOrderID;

		[DataField("SalesOrderDetailID", DbType.Int32, false, IsKeyField = true, IsAutoIncrement = true)]
		public Int32 SalesOrderDetailID
		{
			get { return _salesOrderDetailID; }
			set { SetFieldValue(ref _salesOrderDetailID, value); }
		}
		private Int32 _salesOrderDetailID;

		[DataField("CarrierTrackingNumber", DbType.String, true, Length = 25)]
		public string CarrierTrackingNumber
		{
			get { return _carrierTrackingNumber; }
			set { SetFieldValue(ref _carrierTrackingNumber, value); }
		}
		private string _carrierTrackingNumber;

		[DataField("OrderQty", DbType.Int16, false)]
		public Int16 OrderQty
		{
			get { return _orderQty; }
			set { SetFieldValue(ref _orderQty, value); }
		}
		private Int16 _orderQty;

		[DataField("ProductID", DbType.Int32, false)]
		public Int32 ProductID
		{
			get { return _productID; }
			set { SetFieldValue(ref _productID, value); }
		}
		private Int32 _productID;

		[DataField("SpecialOfferID", DbType.Int32, false)]
		public Int32 SpecialOfferID
		{
			get { return _specialOfferID; }
			set { SetFieldValue(ref _specialOfferID, value); }
		}
		private Int32 _specialOfferID;

		[DataField("UnitPrice", DbType.Decimal, false)]
		public decimal UnitPrice
		{
			get { return _unitPrice; }
			set { SetFieldValue(ref _unitPrice, value); }
		}
		private decimal _unitPrice;

		[DataField("UnitPriceDiscount", DbType.Decimal, false)]
		public decimal UnitPriceDiscount
		{
			get { return _unitPriceDiscount; }
			set { SetFieldValue(ref _unitPriceDiscount, value); }
		}
		private decimal _unitPriceDiscount;

		[DataField("LineTotal", DbType.Decimal, false)]
		public decimal LineTotal
		{
			get { return _lineTotal; }
			set { SetFieldValue(ref _lineTotal, value); }
		}
		private decimal _lineTotal;

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

		public Sales_SalesOrderDetail(bool addingNew) : base(addingNew)
		{
			if (addingNew)
			{
			}
		}

		[Obsolete("This default constructor is required by the DataClassAdapter, but should never be used directly in code.", true)]
		public Sales_SalesOrderDetail() : this(false)
		{ }

		#endregion

	}

}
