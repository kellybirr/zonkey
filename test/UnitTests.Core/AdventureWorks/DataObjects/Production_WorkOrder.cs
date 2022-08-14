using System;
using System.Data;
using Zonkey.ObjectModel;

namespace Zonkey.UnitTests.AdventureWorks.DataObjects
{
	[DataItem("WorkOrder", SchemaName = "Production")]
	public class Production_WorkOrder : DataClass
	{
		#region Data Columns (Properties)

		[DataField("WorkOrderID", DbType.Int32, false, IsKeyField = true, IsAutoIncrement = true)]
		public Int32 WorkOrderID
		{
			get { return _workOrderID; }
			set { SetFieldValue(ref _workOrderID, value); }
		}
		private Int32 _workOrderID;

		[DataField("ProductID", DbType.Int32, false)]
		public Int32 ProductID
		{
			get { return _productID; }
			set { SetFieldValue(ref _productID, value); }
		}
		private Int32 _productID;

		[DataField("OrderQty", DbType.Int32, false)]
		public Int32 OrderQty
		{
			get { return _orderQty; }
			set { SetFieldValue(ref _orderQty, value); }
		}
		private Int32 _orderQty;

		[DataField("StockedQty", DbType.Int32, false)]
		public Int32 StockedQty
		{
			get { return _stockedQty; }
			set { SetFieldValue(ref _stockedQty, value); }
		}
		private Int32 _stockedQty;

		[DataField("ScrappedQty", DbType.Int16, false)]
		public Int16 ScrappedQty
		{
			get { return _scrappedQty; }
			set { SetFieldValue(ref _scrappedQty, value); }
		}
		private Int16 _scrappedQty;

		[DataField("StartDate", DbType.DateTime, false)]
		public DateTime StartDate
		{
			get { return _startDate; }
			set { SetFieldValue(ref _startDate, value); }
		}
		private DateTime _startDate;

		[DataField("EndDate", DbType.DateTime, true)]
		public DateTime? EndDate
		{
			get { return _endDate; }
			set { SetFieldValue(ref _endDate, value); }
		}
		private DateTime? _endDate;

		[DataField("DueDate", DbType.DateTime, false)]
		public DateTime DueDate
		{
			get { return _dueDate; }
			set { SetFieldValue(ref _dueDate, value); }
		}
		private DateTime _dueDate;

		[DataField("ScrapReasonID", DbType.Int16, true)]
		public Int16? ScrapReasonID
		{
			get { return _scrapReasonID; }
			set { SetFieldValue(ref _scrapReasonID, value); }
		}
		private Int16? _scrapReasonID;

		[DataField("ModifiedDate", DbType.DateTime, false)]
		public DateTime ModifiedDate
		{
			get { return _modifiedDate; }
			set { SetFieldValue(ref _modifiedDate, value); }
		}
		private DateTime _modifiedDate;

		#endregion

		#region Constructors

		public Production_WorkOrder(bool addingNew) : base(addingNew)
		{
			if (addingNew)
			{
			}
		}

		[Obsolete("This default constructor is required by the DataClassAdapter, but should never be used directly in code.", true)]
		public Production_WorkOrder() : this(false)
		{ }

		#endregion

	}

}
