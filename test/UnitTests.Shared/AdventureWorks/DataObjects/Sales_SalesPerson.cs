using System;
using System.Data;
using Zonkey.ObjectModel;

namespace Zonkey.UnitTests.AdventureWorks.DataObjects
{
	[DataItem("SalesPerson", SchemaName = "Sales")]
	public class Sales_SalesPerson : DataClass
	{
		#region Data Columns (Properties)

		[DataField("BusinessEntityID", DbType.Int32, false, IsKeyField = true)]
		public Int32 BusinessEntityID
		{
			get { return _businessEntityID; }
			set { SetFieldValue(ref _businessEntityID, value); }
		}
		private Int32 _businessEntityID;

		[DataField("TerritoryID", DbType.Int32, true)]
		public Int32? TerritoryID
		{
			get { return _territoryID; }
			set { SetFieldValue(ref _territoryID, value); }
		}
		private Int32? _territoryID;

		[DataField("SalesQuota", DbType.Decimal, true)]
		public decimal SalesQuota
		{
			get { return _salesQuota; }
			set { SetFieldValue(ref _salesQuota, value); }
		}
		private decimal _salesQuota;

		[DataField("Bonus", DbType.Decimal, false)]
		public decimal Bonus
		{
			get { return _bonus; }
			set { SetFieldValue(ref _bonus, value); }
		}
		private decimal _bonus;

		[DataField("CommissionPct", DbType.Decimal, false)]
		public decimal CommissionPct
		{
			get { return _commissionPct; }
			set { SetFieldValue(ref _commissionPct, value); }
		}
		private decimal _commissionPct;

		[DataField("SalesYTD", DbType.Decimal, false)]
		public decimal SalesYTD
		{
			get { return _salesYTD; }
			set { SetFieldValue(ref _salesYTD, value); }
		}
		private decimal _salesYTD;

		[DataField("SalesLastYear", DbType.Decimal, false)]
		public decimal SalesLastYear
		{
			get { return _salesLastYear; }
			set { SetFieldValue(ref _salesLastYear, value); }
		}
		private decimal _salesLastYear;

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

		public Sales_SalesPerson(bool addingNew) : base(addingNew)
		{
			if (addingNew)
			{
			}
		}

		[Obsolete("This default constructor is required by the DataClassAdapter, but should never be used directly in code.", true)]
		public Sales_SalesPerson() : this(false)
		{ }

		#endregion

	}

}
