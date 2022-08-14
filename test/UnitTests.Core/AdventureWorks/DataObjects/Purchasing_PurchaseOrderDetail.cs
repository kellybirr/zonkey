using System;
using System.Data;
using Zonkey.ObjectModel;

namespace Zonkey.UnitTests.AdventureWorks.DataObjects
{
	[DataItem("PurchaseOrderDetail", SchemaName = "Purchasing")]
	public class Purchasing_PurchaseOrderDetail : DataClass
	{
		#region Data Columns (Properties)

		[DataField("PurchaseOrderID", DbType.Int32, false, IsKeyField = true)]
		public Int32 PurchaseOrderID
		{
			get { return _purchaseOrderID; }
			set { SetFieldValue(ref _purchaseOrderID, value); }
		}
		private Int32 _purchaseOrderID;

		[DataField("PurchaseOrderDetailID", DbType.Int32, false, IsKeyField = true, IsAutoIncrement = true)]
		public Int32 PurchaseOrderDetailID
		{
			get { return _purchaseOrderDetailID; }
			set { SetFieldValue(ref _purchaseOrderDetailID, value); }
		}
		private Int32 _purchaseOrderDetailID;

		[DataField("DueDate", DbType.DateTime, false)]
		public DateTime DueDate
		{
			get { return _dueDate; }
			set { SetFieldValue(ref _dueDate, value); }
		}
		private DateTime _dueDate;

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

		[DataField("UnitPrice", DbType.Decimal, false)]
		public decimal UnitPrice
		{
			get { return _unitPrice; }
			set { SetFieldValue(ref _unitPrice, value); }
		}
		private decimal _unitPrice;

		[DataField("LineTotal", DbType.Decimal, false)]
		public decimal LineTotal
		{
			get { return _lineTotal; }
			set { SetFieldValue(ref _lineTotal, value); }
		}
		private decimal _lineTotal;

		[DataField("ReceivedQty", DbType.Decimal, false)]
		public decimal ReceivedQty
		{
			get { return _receivedQty; }
			set { SetFieldValue(ref _receivedQty, value); }
		}
		private decimal _receivedQty;

		[DataField("RejectedQty", DbType.Decimal, false)]
		public decimal RejectedQty
		{
			get { return _rejectedQty; }
			set { SetFieldValue(ref _rejectedQty, value); }
		}
		private decimal _rejectedQty;

		[DataField("StockedQty", DbType.Decimal, false)]
		public decimal StockedQty
		{
			get { return _stockedQty; }
			set { SetFieldValue(ref _stockedQty, value); }
		}
		private decimal _stockedQty;

		[DataField("ModifiedDate", DbType.DateTime, false)]
		public DateTime ModifiedDate
		{
			get { return _modifiedDate; }
			set { SetFieldValue(ref _modifiedDate, value); }
		}
		private DateTime _modifiedDate;

		#endregion

		#region Constructors

		public Purchasing_PurchaseOrderDetail(bool addingNew) : base(addingNew)
		{
			if (addingNew)
			{
			}
		}

		[Obsolete("This default constructor is required by the DataClassAdapter, but should never be used directly in code.", true)]
		public Purchasing_PurchaseOrderDetail() : this(false)
		{ }

		#endregion

	}

}
