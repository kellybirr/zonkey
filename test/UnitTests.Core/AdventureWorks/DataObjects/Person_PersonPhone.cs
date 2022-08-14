using System;
using System.Data;
using Zonkey.ObjectModel;

namespace Zonkey.UnitTests.AdventureWorks.DataObjects
{
	[DataItem("PersonPhone", SchemaName = "Person")]
	public class Person_PersonPhone : DataClass
	{
		#region Data Columns (Properties)

		[DataField("BusinessEntityID", DbType.Int32, false, IsKeyField = true)]
		public Int32 BusinessEntityID
		{
			get { return _businessEntityID; }
			set { SetFieldValue(ref _businessEntityID, value); }
		}
		private Int32 _businessEntityID;

		[DataField("PhoneNumber", DbType.String, false, Length = 25, IsKeyField = true)]
		public string PhoneNumber
		{
			get { return _phoneNumber; }
			set { SetFieldValue(ref _phoneNumber, value); }
		}
		private string _phoneNumber;

		[DataField("PhoneNumberTypeID", DbType.Int32, false, IsKeyField = true)]
		public Int32 PhoneNumberTypeID
		{
			get { return _phoneNumberTypeID; }
			set { SetFieldValue(ref _phoneNumberTypeID, value); }
		}
		private Int32 _phoneNumberTypeID;

		[DataField("ModifiedDate", DbType.DateTime, false)]
		public DateTime ModifiedDate
		{
			get { return _modifiedDate; }
			set { SetFieldValue(ref _modifiedDate, value); }
		}
		private DateTime _modifiedDate;

		#endregion

		#region Constructors

		public Person_PersonPhone(bool addingNew) : base(addingNew)
		{
			if (addingNew)
			{
			}
		}

		[Obsolete("This default constructor is required by the DataClassAdapter, but should never be used directly in code.", true)]
		public Person_PersonPhone() : this(false)
		{ }

		#endregion

	}

}
