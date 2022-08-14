using System;
using System.Data;
using Zonkey.ObjectModel;

namespace Zonkey.UnitTests.AdventureWorks.DataObjects
{
	[DataItem("vSalesPerson", SchemaName = "Sales")]
	public class Sales_vSalesPerson : DataClass
	{
		#region Data Columns (Properties)

		[DataField("BusinessEntityID", DbType.Int32, false)]
		public Int32 BusinessEntityID
		{
			get { return _businessEntityID; }
			set { SetFieldValue(ref _businessEntityID, value); }
		}
		private Int32 _businessEntityID;

		[DataField("Title", DbType.String, true, Length = 8)]
		public string Title
		{
			get { return _title; }
			set { SetFieldValue(ref _title, value); }
		}
		private string _title;

		[DataField("FirstName", DbType.String, false, Length = 50)]
		public string FirstName
		{
			get { return _firstName; }
			set { SetFieldValue(ref _firstName, value); }
		}
		private string _firstName;

		[DataField("MiddleName", DbType.String, true, Length = 50)]
		public string MiddleName
		{
			get { return _middleName; }
			set { SetFieldValue(ref _middleName, value); }
		}
		private string _middleName;

		[DataField("LastName", DbType.String, false, Length = 50)]
		public string LastName
		{
			get { return _lastName; }
			set { SetFieldValue(ref _lastName, value); }
		}
		private string _lastName;

		[DataField("Suffix", DbType.String, true, Length = 10)]
		public string Suffix
		{
			get { return _suffix; }
			set { SetFieldValue(ref _suffix, value); }
		}
		private string _suffix;

		[DataField("JobTitle", DbType.String, false, Length = 50)]
		public string JobTitle
		{
			get { return _jobTitle; }
			set { SetFieldValue(ref _jobTitle, value); }
		}
		private string _jobTitle;

		[DataField("PhoneNumber", DbType.String, true, Length = 25)]
		public string PhoneNumber
		{
			get { return _phoneNumber; }
			set { SetFieldValue(ref _phoneNumber, value); }
		}
		private string _phoneNumber;

		[DataField("PhoneNumberType", DbType.String, true, Length = 50)]
		public string PhoneNumberType
		{
			get { return _phoneNumberType; }
			set { SetFieldValue(ref _phoneNumberType, value); }
		}
		private string _phoneNumberType;

		[DataField("EmailAddress", DbType.String, true, Length = 50)]
		public string EmailAddress
		{
			get { return _emailAddress; }
			set { SetFieldValue(ref _emailAddress, value); }
		}
		private string _emailAddress;

		[DataField("EmailPromotion", DbType.Int32, false)]
		public Int32 EmailPromotion
		{
			get { return _emailPromotion; }
			set { SetFieldValue(ref _emailPromotion, value); }
		}
		private Int32 _emailPromotion;

		[DataField("AddressLine1", DbType.String, false, Length = 60)]
		public string AddressLine1
		{
			get { return _addressLine1; }
			set { SetFieldValue(ref _addressLine1, value); }
		}
		private string _addressLine1;

		[DataField("AddressLine2", DbType.String, true, Length = 60)]
		public string AddressLine2
		{
			get { return _addressLine2; }
			set { SetFieldValue(ref _addressLine2, value); }
		}
		private string _addressLine2;

		[DataField("City", DbType.String, false, Length = 30)]
		public string City
		{
			get { return _city; }
			set { SetFieldValue(ref _city, value); }
		}
		private string _city;

		[DataField("StateProvinceName", DbType.String, false, Length = 50)]
		public string StateProvinceName
		{
			get { return _stateProvinceName; }
			set { SetFieldValue(ref _stateProvinceName, value); }
		}
		private string _stateProvinceName;

		[DataField("PostalCode", DbType.String, false, Length = 15)]
		public string PostalCode
		{
			get { return _postalCode; }
			set { SetFieldValue(ref _postalCode, value); }
		}
		private string _postalCode;

		[DataField("CountryRegionName", DbType.String, false, Length = 50)]
		public string CountryRegionName
		{
			get { return _countryRegionName; }
			set { SetFieldValue(ref _countryRegionName, value); }
		}
		private string _countryRegionName;

		[DataField("TerritoryName", DbType.String, true, Length = 50)]
		public string TerritoryName
		{
			get { return _territoryName; }
			set { SetFieldValue(ref _territoryName, value); }
		}
		private string _territoryName;

		[DataField("TerritoryGroup", DbType.String, true, Length = 50)]
		public string TerritoryGroup
		{
			get { return _territoryGroup; }
			set { SetFieldValue(ref _territoryGroup, value); }
		}
		private string _territoryGroup;

		[DataField("SalesQuota", DbType.Decimal, true)]
		public decimal SalesQuota
		{
			get { return _salesQuota; }
			set { SetFieldValue(ref _salesQuota, value); }
		}
		private decimal _salesQuota;

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

		#endregion

		#region Constructors

		public Sales_vSalesPerson(bool addingNew) : base(addingNew)
		{
			if (addingNew)
			{
			}
		}

		[Obsolete("This default constructor is required by the DataClassAdapter, but should never be used directly in code.", true)]
		public Sales_vSalesPerson() : this(false)
		{ }

		#endregion

	}

}
