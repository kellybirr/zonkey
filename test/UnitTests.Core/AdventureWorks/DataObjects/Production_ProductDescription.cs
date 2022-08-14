using System;
using System.Data;
using Zonkey.ObjectModel;

namespace Zonkey.UnitTests.AdventureWorks.DataObjects
{
	[DataItem("ProductDescription", SchemaName = "Production")]
	public class Production_ProductDescription : DataClass
	{
		#region Data Columns (Properties)

		[DataField("ProductDescriptionID", DbType.Int32, false, IsKeyField = true, IsAutoIncrement = true)]
		public Int32 ProductDescriptionID
		{
			get { return _productDescriptionID; }
			set { SetFieldValue(ref _productDescriptionID, value); }
		}
		private Int32 _productDescriptionID;

		[DataField("Description", DbType.String, false, Length = 400)]
		public string Description
		{
			get { return _description; }
			set { SetFieldValue(ref _description, value); }
		}
		private string _description;

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

		public Production_ProductDescription(bool addingNew) : base(addingNew)
		{
			if (addingNew)
			{
			}
		}

		[Obsolete("This default constructor is required by the DataClassAdapter, but should never be used directly in code.", true)]
		public Production_ProductDescription() : this(false)
		{ }

		#endregion

	}

}
