using System;
using System.Data;
using Zonkey.ObjectModel;

namespace Zonkey.UnitTests.AdventureWorks.DataObjects
{
	[DataItem("ProductListPriceHistory", SchemaName = "Production")]
	public class Production_ProductListPriceHistory : DataClass
	{
		#region Data Columns (Properties)

		[DataField("ProductID", DbType.Int32, false, IsKeyField = true)]
		public Int32 ProductID
		{
			get { return _productID; }
			set { SetFieldValue(ref _productID, value); }
		}
		private Int32 _productID;

		[DataField("StartDate", DbType.DateTime, false, IsKeyField = true)]
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

		[DataField("ListPrice", DbType.Decimal, false)]
		public decimal ListPrice
		{
			get { return _listPrice; }
			set { SetFieldValue(ref _listPrice, value); }
		}
		private decimal _listPrice;

		[DataField("ModifiedDate", DbType.DateTime, false)]
		public DateTime ModifiedDate
		{
			get { return _modifiedDate; }
			set { SetFieldValue(ref _modifiedDate, value); }
		}
		private DateTime _modifiedDate;

		#endregion

		#region Constructors

		public Production_ProductListPriceHistory(bool addingNew) : base(addingNew)
		{
			if (addingNew)
			{
			}
		}

		[Obsolete("This default constructor is required by the DataClassAdapter, but should never be used directly in code.", true)]
		public Production_ProductListPriceHistory() : this(false)
		{ }

		#endregion

	}

}
