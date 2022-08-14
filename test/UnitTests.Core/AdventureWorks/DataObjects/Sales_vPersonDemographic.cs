using System;
using System.Data;
using Zonkey.ObjectModel;

namespace Zonkey.UnitTests.AdventureWorks.DataObjects
{
	[DataItem("vPersonDemographics", SchemaName = "Sales")]
	public class Sales_vPersonDemographic : DataClass
	{
		#region Data Columns (Properties)

		[DataField("BusinessEntityID", DbType.Int32, false)]
		public Int32 BusinessEntityID
		{
			get { return _businessEntityID; }
			set { SetFieldValue(ref _businessEntityID, value); }
		}
		private Int32 _businessEntityID;

		[DataField("TotalPurchaseYTD", DbType.Decimal, true)]
		public decimal TotalPurchaseYTD
		{
			get { return _totalPurchaseYTD; }
			set { SetFieldValue(ref _totalPurchaseYTD, value); }
		}
		private decimal _totalPurchaseYTD;

		[DataField("DateFirstPurchase", DbType.DateTime, true)]
		public DateTime? DateFirstPurchase
		{
			get { return _dateFirstPurchase; }
			set { SetFieldValue(ref _dateFirstPurchase, value); }
		}
		private DateTime? _dateFirstPurchase;

		[DataField("BirthDate", DbType.DateTime, true)]
		public DateTime? BirthDate
		{
			get { return _birthDate; }
			set { SetFieldValue(ref _birthDate, value); }
		}
		private DateTime? _birthDate;

		[DataField("MaritalStatus", DbType.String, true, Length = 1)]
		public string MaritalStatus
		{
			get { return _maritalStatus; }
			set { SetFieldValue(ref _maritalStatus, value); }
		}
		private string _maritalStatus;

		[DataField("YearlyIncome", DbType.String, true, Length = 30)]
		public string YearlyIncome
		{
			get { return _yearlyIncome; }
			set { SetFieldValue(ref _yearlyIncome, value); }
		}
		private string _yearlyIncome;

		[DataField("Gender", DbType.String, true, Length = 1)]
		public string Gender
		{
			get { return _gender; }
			set { SetFieldValue(ref _gender, value); }
		}
		private string _gender;

		[DataField("TotalChildren", DbType.Int32, true)]
		public Int32? TotalChildren
		{
			get { return _totalChildren; }
			set { SetFieldValue(ref _totalChildren, value); }
		}
		private Int32? _totalChildren;

		[DataField("NumberChildrenAtHome", DbType.Int32, true)]
		public Int32? NumberChildrenAtHome
		{
			get { return _numberChildrenAtHome; }
			set { SetFieldValue(ref _numberChildrenAtHome, value); }
		}
		private Int32? _numberChildrenAtHome;

		[DataField("Education", DbType.String, true, Length = 30)]
		public string Education
		{
			get { return _education; }
			set { SetFieldValue(ref _education, value); }
		}
		private string _education;

		[DataField("Occupation", DbType.String, true, Length = 30)]
		public string Occupation
		{
			get { return _occupation; }
			set { SetFieldValue(ref _occupation, value); }
		}
		private string _occupation;

		[DataField("HomeOwnerFlag", DbType.Boolean, true)]
		public bool HomeOwnerFlag
		{
			get { return _homeOwnerFlag; }
			set { SetFieldValue(ref _homeOwnerFlag, value); }
		}
		private bool _homeOwnerFlag;

		[DataField("NumberCarsOwned", DbType.Int32, true)]
		public Int32? NumberCarsOwned
		{
			get { return _numberCarsOwned; }
			set { SetFieldValue(ref _numberCarsOwned, value); }
		}
		private Int32? _numberCarsOwned;

		#endregion

		#region Constructors

		public Sales_vPersonDemographic(bool addingNew) : base(addingNew)
		{
			if (addingNew)
			{
			}
		}

		[Obsolete("This default constructor is required by the DataClassAdapter, but should never be used directly in code.", true)]
		public Sales_vPersonDemographic() : this(false)
		{ }

		#endregion

	}

}
