using System;
using System.Data;
using Zonkey.ObjectModel;

namespace Zonkey.UnitTests.AdventureWorks.DataObjects
{
	[DataItem("Person", SchemaName = "Person")]
	public class Person_Person : DataClass
	{
		#region Data Columns (Properties)

		[DataField("BusinessEntityID", DbType.Int32, false, IsKeyField = true)]
		public Int32 BusinessEntityID
		{
			get { return _businessEntityID; }
			set { SetFieldValue(ref _businessEntityID, value); }
		}
		private Int32 _businessEntityID;

		[DataField("PersonType", DbType.StringFixedLength, false, Length = 2)]
		public string PersonType
		{
			get { return _personType; }
			set { SetFieldValue(ref _personType, value); }
		}
		private string _personType;

		[DataField("NameStyle", DbType.Boolean, false)]
		public bool NameStyle
		{
			get { return _nameStyle; }
			set { SetFieldValue(ref _nameStyle, value); }
		}
		private bool _nameStyle;

		[DataField("Title", DbType.String, true, Length = 8)]
		public string Title
		{
			get => _title;
		    set => SetFieldValue(ref _title, value);
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

		[DataField("EmailPromotion", DbType.Int32, false)]
		public Int32 EmailPromotion
		{
			get { return _emailPromotion; }
			set { SetFieldValue(ref _emailPromotion, value); }
		}
		private Int32 _emailPromotion;

		[DataField("AdditionalContactInfo", DbType.String, true, Length = 2147483647)]
		public string AdditionalContactInfo
		{
			get { return _additionalContactInfo; }
			set { SetFieldValue(ref _additionalContactInfo, value); }
		}
		private string _additionalContactInfo;

		[DataField("Demographics", DbType.String, true, Length = 2147483647)]
		public string Demographics
		{
			get { return _demographics; }
			set { SetFieldValue(ref _demographics, value); }
		}
		private string _demographics;

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

		public Person_Person(bool addingNew) : base(addingNew)
		{
			if (addingNew)
			{
			}
		}

		[Obsolete("This default constructor is required by the DataClassAdapter, but should never be used directly in code.", true)]
		public Person_Person() : this(false)
		{ }

		#endregion

	}

}
