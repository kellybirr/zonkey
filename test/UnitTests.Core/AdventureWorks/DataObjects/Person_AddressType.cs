using System;
using System.Data;
using Zonkey.ObjectModel;

namespace Zonkey.UnitTests.AdventureWorks.DataObjects
{
	[DataItem("AddressType", SchemaName = "Person")]
	public class Person_AddressType : DataClass
	{
		#region Data Columns (Properties)

		[DataField("AddressTypeID", DbType.Int32, false, IsKeyField = true, IsAutoIncrement = true)]
		public Int32 AddressTypeID
		{
			get { return _addressTypeID; }
			set { SetFieldValue(ref _addressTypeID, value); }
		}
		private Int32 _addressTypeID;

		[DataField("Name", DbType.String, false, Length = 50)]
		public string Name
		{
			get { return _name; }
			set { SetFieldValue(ref _name, value); }
		}
		private string _name;

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

		public Person_AddressType(bool addingNew) : base(addingNew)
		{
			if (addingNew)
			{
			}
		}

		[Obsolete("This default constructor is required by the DataClassAdapter, but should never be used directly in code.", true)]
		public Person_AddressType() : this(false)
		{ }

		#endregion

	}

}
