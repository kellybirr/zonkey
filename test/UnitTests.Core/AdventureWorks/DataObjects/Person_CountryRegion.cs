using System;
using System.Data;
using Zonkey.ObjectModel;

namespace Zonkey.UnitTests.AdventureWorks.DataObjects
{
	[DataItem("CountryRegion", SchemaName = "Person")]
	public class Person_CountryRegion : DataClass
	{
		#region Data Columns (Properties)

		[DataField("CountryRegionCode", DbType.String, false, Length = 3, IsKeyField = true)]
		public string CountryRegionCode
		{
			get { return _countryRegionCode; }
			set { SetFieldValue(ref _countryRegionCode, value); }
		}
		private string _countryRegionCode;

		[DataField("Name", DbType.String, false, Length = 50)]
		public string Name
		{
			get { return _name; }
			set { SetFieldValue(ref _name, value); }
		}
		private string _name;

		[DataField("ModifiedDate", DbType.DateTime, false)]
		public DateTime ModifiedDate
		{
			get { return _modifiedDate; }
			set { SetFieldValue(ref _modifiedDate, value); }
		}
		private DateTime _modifiedDate;

		#endregion

		#region Constructors

		public Person_CountryRegion(bool addingNew) : base(addingNew)
		{
			if (addingNew)
			{
			}
		}

		[Obsolete("This default constructor is required by the DataClassAdapter, but should never be used directly in code.", true)]
		public Person_CountryRegion() : this(false)
		{ }

		#endregion

	}

}
