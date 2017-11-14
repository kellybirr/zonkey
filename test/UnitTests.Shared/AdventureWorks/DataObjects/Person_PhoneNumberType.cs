using System;
using System.Data;
using Zonkey.ObjectModel;

namespace Zonkey.UnitTests.AdventureWorks.DataObjects
{
	[DataItem("PhoneNumberType", SchemaName = "Person")]
	public class Person_PhoneNumberType : DataClass
	{
		#region Data Columns (Properties)

		[DataField("PhoneNumberTypeID", DbType.Int32, false, IsKeyField = true, IsAutoIncrement = true)]
		public Int32 PhoneNumberTypeID
		{
			get { return _phoneNumberTypeID; }
			set { SetFieldValue(ref _phoneNumberTypeID, value); }
		}
		private Int32 _phoneNumberTypeID;

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

		public Person_PhoneNumberType(bool addingNew) : base(addingNew)
		{
			if (addingNew)
			{
			}
		}

		[Obsolete("This default constructor is required by the DataClassAdapter, but should never be used directly in code.", true)]
		public Person_PhoneNumberType() : this(false)
		{ }

		#endregion

	}

}
