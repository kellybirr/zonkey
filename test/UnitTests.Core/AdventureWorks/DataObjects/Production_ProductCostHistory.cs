using System;
using System.Data;
using Zonkey.ObjectModel;

namespace Zonkey.UnitTests.AdventureWorks.DataObjects
{
	[DataItem("ProductCostHistory", SchemaName = "Production")]
	public class Production_ProductCostHistory : DataClass
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

		[DataField("StandardCost", DbType.Decimal, false)]
		public decimal StandardCost
		{
			get { return _standardCost; }
			set { SetFieldValue(ref _standardCost, value); }
		}
		private decimal _standardCost;

		[DataField("ModifiedDate", DbType.DateTime, false)]
		public DateTime ModifiedDate
		{
			get { return _modifiedDate; }
			set { SetFieldValue(ref _modifiedDate, value); }
		}
		private DateTime _modifiedDate;

		#endregion

		#region Constructors

		public Production_ProductCostHistory(bool addingNew) : base(addingNew)
		{
			if (addingNew)
			{
			}
		}

		[Obsolete("This default constructor is required by the DataClassAdapter, but should never be used directly in code.", true)]
		public Production_ProductCostHistory() : this(false)
		{ }

		#endregion

	}

}
