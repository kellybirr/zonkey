using System;
using System.Data;
using Zonkey.ObjectModel;

namespace Zonkey.UnitTests.AdventureWorks.DataObjects
{
	[DataItem("BusinessEntity", SchemaName = "Person")]
	public class Person_BusinessEntity : DataClass
	{
		#region Data Columns (Properties)

		[DataField("BusinessEntityID", DbType.Int32, false, IsKeyField = true, IsAutoIncrement = true)]
		public Int32 BusinessEntityID
		{
			get { return _businessEntityID; }
			set { SetFieldValue(ref _businessEntityID, value); }
		}
		private Int32 _businessEntityID;

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

		public Person_BusinessEntity(bool addingNew) : base(addingNew)
		{
			if (addingNew)
			{
			}
		}

		[Obsolete("This default constructor is required by the DataClassAdapter, but should never be used directly in code.", true)]
		public Person_BusinessEntity() : this(false)
		{ }

		#endregion

	}

}
