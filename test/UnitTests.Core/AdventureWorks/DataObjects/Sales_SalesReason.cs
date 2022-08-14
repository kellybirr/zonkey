using System;
using System.Data;
using Zonkey.ObjectModel;

namespace Zonkey.UnitTests.AdventureWorks.DataObjects
{
	[DataItem("SalesReason", SchemaName = "Sales")]
	public class Sales_SalesReason : DataClass
	{
		#region Data Columns (Properties)

		[DataField("SalesReasonID", DbType.Int32, false, IsKeyField = true, IsAutoIncrement = true)]
		public Int32 SalesReasonID
		{
			get { return _salesReasonID; }
			set { SetFieldValue(ref _salesReasonID, value); }
		}
		private Int32 _salesReasonID;

		[DataField("Name", DbType.String, false, Length = 50)]
		public string Name
		{
			get { return _name; }
			set { SetFieldValue(ref _name, value); }
		}
		private string _name;

		[DataField("ReasonType", DbType.String, false, Length = 50)]
		public string ReasonType
		{
			get { return _reasonType; }
			set { SetFieldValue(ref _reasonType, value); }
		}
		private string _reasonType;

		[DataField("ModifiedDate", DbType.DateTime, false)]
		public DateTime ModifiedDate
		{
			get { return _modifiedDate; }
			set { SetFieldValue(ref _modifiedDate, value); }
		}
		private DateTime _modifiedDate;

		#endregion

		#region Constructors

		public Sales_SalesReason(bool addingNew) : base(addingNew)
		{
			if (addingNew)
			{
			}
		}

		[Obsolete("This default constructor is required by the DataClassAdapter, but should never be used directly in code.", true)]
		public Sales_SalesReason() : this(false)
		{ }

		#endregion

	}

}
