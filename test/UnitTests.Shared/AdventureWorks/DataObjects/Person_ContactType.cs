using System;
using System.Data;
using Zonkey.ObjectModel;

namespace Zonkey.UnitTests.AdventureWorks.DataObjects
{
	[DataItem("ContactType", SchemaName = "Person")]
	public class Person_ContactType : DataClass
	{
		#region Data Columns (Properties)

		[DataField("ContactTypeID", DbType.Int32, false, IsKeyField = true, IsAutoIncrement = true)]
		public Int32 ContactTypeID
		{
			get { return _contactTypeID; }
			set { SetFieldValue(ref _contactTypeID, value); }
		}
		private Int32 _contactTypeID;

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

		public Person_ContactType(bool addingNew) : base(addingNew)
		{
			if (addingNew)
			{
			}
		}

		[Obsolete("This default constructor is required by the DataClassAdapter, but should never be used directly in code.", true)]
		public Person_ContactType() : this(false)
		{ }

		#endregion

	}

}
