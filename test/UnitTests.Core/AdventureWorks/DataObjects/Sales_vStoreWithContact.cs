using System;
using System.Data;
using Zonkey.ObjectModel;

namespace Zonkey.UnitTests.AdventureWorks.DataObjects
{
	[DataItem("vStoreWithContacts", SchemaName = "Sales")]
	public class Sales_vStoreWithContact : DataClass
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

		[DataField("ContactType", DbType.String, false, Length = 50)]
		public string ContactType
		{
			get { return _contactType; }
			set { SetFieldValue(ref _contactType, value); }
		}
		private string _contactType;

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

		#endregion

		#region Constructors

		public Sales_vStoreWithContact(bool addingNew) : base(addingNew)
		{
			if (addingNew)
			{
			}
		}

		[Obsolete("This default constructor is required by the DataClassAdapter, but should never be used directly in code.", true)]
		public Sales_vStoreWithContact() : this(false)
		{ }

		#endregion

	}

}
