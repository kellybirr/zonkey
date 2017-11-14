using System;
using System.Data;
using Zonkey.ObjectModel;

namespace Zonkey.UnitTests.AdventureWorks.DataObjects
{
	[DataItem("StateProvince", SchemaName = "Person")]
	public class Person_StateProvince : DataClass
	{
		#region Data Columns (Properties)

		[DataField("StateProvinceID", DbType.Int32, false, IsKeyField = true, IsAutoIncrement = true)]
		public Int32 StateProvinceID
		{
			get { return _stateProvinceID; }
			set { SetFieldValue(ref _stateProvinceID, value); }
		}
		private Int32 _stateProvinceID;

		[DataField("StateProvinceCode", DbType.StringFixedLength, false, Length = 3)]
		public string StateProvinceCode
		{
			get { return _stateProvinceCode; }
			set { SetFieldValue(ref _stateProvinceCode, value); }
		}
		private string _stateProvinceCode;

		[DataField("CountryRegionCode", DbType.String, false, Length = 3)]
		public string CountryRegionCode
		{
			get { return _countryRegionCode; }
			set { SetFieldValue(ref _countryRegionCode, value); }
		}
		private string _countryRegionCode;

		[DataField("IsOnlyStateProvinceFlag", DbType.Boolean, false)]
		public bool IsOnlyStateProvinceFlag
		{
			get { return _isOnlyStateProvinceFlag; }
			set { SetFieldValue(ref _isOnlyStateProvinceFlag, value); }
		}
		private bool _isOnlyStateProvinceFlag;

		[DataField("Name", DbType.String, false, Length = 50)]
		public string Name
		{
			get { return _name; }
			set { SetFieldValue(ref _name, value); }
		}
		private string _name;

		[DataField("TerritoryID", DbType.Int32, false)]
		public Int32 TerritoryID
		{
			get { return _territoryID; }
			set { SetFieldValue(ref _territoryID, value); }
		}
		private Int32 _territoryID;

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

		public Person_StateProvince(bool addingNew) : base(addingNew)
		{
			if (addingNew)
			{
			}
		}

		[Obsolete("This default constructor is required by the DataClassAdapter, but should never be used directly in code.", true)]
		public Person_StateProvince() : this(false)
		{ }

		#endregion

	}

}
