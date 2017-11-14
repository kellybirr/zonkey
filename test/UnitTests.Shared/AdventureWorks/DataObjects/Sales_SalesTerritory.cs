using System;
using System.Data;
using Zonkey.ObjectModel;

namespace Zonkey.UnitTests.AdventureWorks.DataObjects
{
	[DataItem("SalesTerritory", SchemaName = "Sales")]
	public class Sales_SalesTerritory : DataClass
	{
		#region Data Columns (Properties)

		[DataField("TerritoryID", DbType.Int32, false, IsKeyField = true, IsAutoIncrement = true)]
		public Int32 TerritoryID
		{
			get { return _territoryID; }
			set { SetFieldValue(ref _territoryID, value); }
		}
		private Int32 _territoryID;

		[DataField("Name", DbType.String, false, Length = 50)]
		public string Name
		{
			get { return _name; }
			set { SetFieldValue(ref _name, value); }
		}
		private string _name;

		[DataField("CountryRegionCode", DbType.String, false, Length = 3)]
		public string CountryRegionCode
		{
			get { return _countryRegionCode; }
			set { SetFieldValue(ref _countryRegionCode, value); }
		}
		private string _countryRegionCode;

		[DataField("Group", DbType.String, false, Length = 50)]
		public string Group
		{
			get { return _group; }
			set { SetFieldValue(ref _group, value); }
		}
		private string _group;

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

		[DataField("CostYTD", DbType.Decimal, false)]
		public decimal CostYTD
		{
			get { return _costYTD; }
			set { SetFieldValue(ref _costYTD, value); }
		}
		private decimal _costYTD;

		[DataField("CostLastYear", DbType.Decimal, false)]
		public decimal CostLastYear
		{
			get { return _costLastYear; }
			set { SetFieldValue(ref _costLastYear, value); }
		}
		private decimal _costLastYear;

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

		public Sales_SalesTerritory(bool addingNew) : base(addingNew)
		{
			if (addingNew)
			{
			}
		}

		[Obsolete("This default constructor is required by the DataClassAdapter, but should never be used directly in code.", true)]
		public Sales_SalesTerritory() : this(false)
		{ }

		#endregion

	}

}
