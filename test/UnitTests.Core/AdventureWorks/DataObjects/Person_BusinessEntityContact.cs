using System;
using System.Data;
using Zonkey.ObjectModel;

namespace Zonkey.UnitTests.AdventureWorks.DataObjects
{
	[DataItem("BusinessEntityContact", SchemaName = "Person")]
	public class Person_BusinessEntityContact : DataClass
	{
		#region Data Columns (Properties)

		[DataField("BusinessEntityID", DbType.Int32, false, IsKeyField = true)]
		public Int32 BusinessEntityID
		{
			get { return _businessEntityID; }
			set { SetFieldValue(ref _businessEntityID, value); }
		}
		private Int32 _businessEntityID;

		[DataField("PersonID", DbType.Int32, false, IsKeyField = true)]
		public Int32 PersonID
		{
			get { return _personID; }
			set { SetFieldValue(ref _personID, value); }
		}
		private Int32 _personID;

		[DataField("ContactTypeID", DbType.Int32, false, IsKeyField = true)]
		public Int32 ContactTypeID
		{
			get { return _contactTypeID; }
			set { SetFieldValue(ref _contactTypeID, value); }
		}
		private Int32 _contactTypeID;

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

		public Person_BusinessEntityContact(bool addingNew) : base(addingNew)
		{
			if (addingNew)
			{
			}
		}

		[Obsolete("This default constructor is required by the DataClassAdapter, but should never be used directly in code.", true)]
		public Person_BusinessEntityContact() : this(false)
		{ }

		#endregion

	}

}
