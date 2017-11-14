using System;
using System.Data;
using Zonkey.ObjectModel;

namespace Zonkey.UnitTests.AdventureWorks.DataObjects
{
	[DataItem("CountryRegionCurrency", SchemaName = "Sales")]
	public class Sales_CountryRegionCurrency : DataClass
	{
		#region Data Columns (Properties)

		[DataField("CountryRegionCode", DbType.String, false, Length = 3, IsKeyField = true)]
		public string CountryRegionCode
		{
			get { return _countryRegionCode; }
			set { SetFieldValue(ref _countryRegionCode, value); }
		}
		private string _countryRegionCode;

		[DataField("CurrencyCode", DbType.StringFixedLength, false, Length = 3, IsKeyField = true)]
		public string CurrencyCode
		{
			get { return _currencyCode; }
			set { SetFieldValue(ref _currencyCode, value); }
		}
		private string _currencyCode;

		[DataField("ModifiedDate", DbType.DateTime, false)]
		public DateTime ModifiedDate
		{
			get { return _modifiedDate; }
			set { SetFieldValue(ref _modifiedDate, value); }
		}
		private DateTime _modifiedDate;

		#endregion

		#region Constructors

		public Sales_CountryRegionCurrency(bool addingNew) : base(addingNew)
		{
			if (addingNew)
			{
			}
		}

		[Obsolete("This default constructor is required by the DataClassAdapter, but should never be used directly in code.", true)]
		public Sales_CountryRegionCurrency() : this(false)
		{ }

		#endregion

	}

}
