using System;
using System.Data;
using Zonkey.ObjectModel;

namespace Zonkey.UnitTests.AdventureWorks.DataObjects
{
	[DataItem("ScrapReason", SchemaName = "Production")]
	public class Production_ScrapReason : DataClass
	{
		#region Data Columns (Properties)

		[DataField("ScrapReasonID", DbType.Int16, false, IsKeyField = true, IsAutoIncrement = true)]
		public Int16 ScrapReasonID
		{
			get { return _scrapReasonID; }
			set { SetFieldValue(ref _scrapReasonID, value); }
		}
		private Int16 _scrapReasonID;

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

		public Production_ScrapReason(bool addingNew) : base(addingNew)
		{
			if (addingNew)
			{
			}
		}

		[Obsolete("This default constructor is required by the DataClassAdapter, but should never be used directly in code.", true)]
		public Production_ScrapReason() : this(false)
		{ }

		#endregion

	}

}
