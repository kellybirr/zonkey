using System;
using System.Data;
using Zonkey.ObjectModel;

namespace Zonkey.UnitTests.AdventureWorks.DataObjects
{
	[DataItem("SalesOrderHeaderSalesReason", SchemaName = "Sales")]
	public class Sales_SalesOrderHeaderSalesReason : DataClass
	{
		#region Data Columns (Properties)

		[DataField("SalesOrderID", DbType.Int32, false, IsKeyField = true)]
		public Int32 SalesOrderID
		{
			get { return _salesOrderID; }
			set { SetFieldValue(ref _salesOrderID, value); }
		}
		private Int32 _salesOrderID;

		[DataField("SalesReasonID", DbType.Int32, false, IsKeyField = true)]
		public Int32 SalesReasonID
		{
			get { return _salesReasonID; }
			set { SetFieldValue(ref _salesReasonID, value); }
		}
		private Int32 _salesReasonID;

		[DataField("ModifiedDate", DbType.DateTime, false)]
		public DateTime ModifiedDate
		{
			get { return _modifiedDate; }
			set { SetFieldValue(ref _modifiedDate, value); }
		}
		private DateTime _modifiedDate;

		#endregion

		#region Constructors

		public Sales_SalesOrderHeaderSalesReason(bool addingNew) : base(addingNew)
		{
			if (addingNew)
			{
			}
		}

		[Obsolete("This default constructor is required by the DataClassAdapter, but should never be used directly in code.", true)]
		public Sales_SalesOrderHeaderSalesReason() : this(false)
		{ }

		#endregion

	}

}
