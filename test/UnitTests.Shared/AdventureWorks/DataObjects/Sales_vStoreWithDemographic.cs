using System;
using System.Data;
using Zonkey.ObjectModel;

namespace Zonkey.UnitTests.AdventureWorks.DataObjects
{
	[DataItem("vStoreWithDemographics", SchemaName = "Sales")]
	public class Sales_vStoreWithDemographic : DataClass
	{
		#region Data Columns (Properties)

		[DataField("BusinessEntityID", DbType.Int32, false)]
		public Int32 BusinessEntityID
		{
			get { return _businessEntityID; }
			set { SetFieldValue(ref _businessEntityID, value); }
		}
		private Int32 _businessEntityID;

		[DataField("Name", DbType.String, false, Length = 50)]
		public string Name
		{
			get { return _name; }
			set { SetFieldValue(ref _name, value); }
		}
		private string _name;

		[DataField("AnnualSales", DbType.Decimal, true)]
		public decimal AnnualSales
		{
			get { return _annualSales; }
			set { SetFieldValue(ref _annualSales, value); }
		}
		private decimal _annualSales;

		[DataField("AnnualRevenue", DbType.Decimal, true)]
		public decimal AnnualRevenue
		{
			get { return _annualRevenue; }
			set { SetFieldValue(ref _annualRevenue, value); }
		}
		private decimal _annualRevenue;

		[DataField("BankName", DbType.String, true, Length = 50)]
		public string BankName
		{
			get { return _bankName; }
			set { SetFieldValue(ref _bankName, value); }
		}
		private string _bankName;

		[DataField("BusinessType", DbType.String, true, Length = 5)]
		public string BusinessType
		{
			get { return _businessType; }
			set { SetFieldValue(ref _businessType, value); }
		}
		private string _businessType;

		[DataField("YearOpened", DbType.Int32, true)]
		public Int32? YearOpened
		{
			get { return _yearOpened; }
			set { SetFieldValue(ref _yearOpened, value); }
		}
		private Int32? _yearOpened;

		[DataField("Specialty", DbType.String, true, Length = 50)]
		public string Specialty
		{
			get { return _specialty; }
			set { SetFieldValue(ref _specialty, value); }
		}
		private string _specialty;

		[DataField("SquareFeet", DbType.Int32, true)]
		public Int32? SquareFeet
		{
			get { return _squareFeet; }
			set { SetFieldValue(ref _squareFeet, value); }
		}
		private Int32? _squareFeet;

		[DataField("Brands", DbType.String, true, Length = 30)]
		public string Brands
		{
			get { return _brands; }
			set { SetFieldValue(ref _brands, value); }
		}
		private string _brands;

		[DataField("Internet", DbType.String, true, Length = 30)]
		public string Internet
		{
			get { return _internet; }
			set { SetFieldValue(ref _internet, value); }
		}
		private string _internet;

		[DataField("NumberEmployees", DbType.Int32, true)]
		public Int32? NumberEmployees
		{
			get { return _numberEmployees; }
			set { SetFieldValue(ref _numberEmployees, value); }
		}
		private Int32? _numberEmployees;

		#endregion

		#region Constructors

		public Sales_vStoreWithDemographic(bool addingNew) : base(addingNew)
		{
			if (addingNew)
			{
			}
		}

		[Obsolete("This default constructor is required by the DataClassAdapter, but should never be used directly in code.", true)]
		public Sales_vStoreWithDemographic() : this(false)
		{ }

		#endregion

	}

}
