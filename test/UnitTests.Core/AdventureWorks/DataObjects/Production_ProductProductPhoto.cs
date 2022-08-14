using System;
using System.Data;
using Zonkey.ObjectModel;

namespace Zonkey.UnitTests.AdventureWorks.DataObjects
{
	[DataItem("ProductProductPhoto", SchemaName = "Production")]
	public class Production_ProductProductPhoto : DataClass
	{
		#region Data Columns (Properties)

		[DataField("ProductID", DbType.Int32, false, IsKeyField = true)]
		public Int32 ProductID
		{
			get { return _productID; }
			set { SetFieldValue(ref _productID, value); }
		}
		private Int32 _productID;

		[DataField("ProductPhotoID", DbType.Int32, false, IsKeyField = true)]
		public Int32 ProductPhotoID
		{
			get { return _productPhotoID; }
			set { SetFieldValue(ref _productPhotoID, value); }
		}
		private Int32 _productPhotoID;

		[DataField("Primary", DbType.Boolean, false)]
		public bool Primary
		{
			get { return _primary; }
			set { SetFieldValue(ref _primary, value); }
		}
		private bool _primary;

		[DataField("ModifiedDate", DbType.DateTime, false)]
		public DateTime ModifiedDate
		{
			get { return _modifiedDate; }
			set { SetFieldValue(ref _modifiedDate, value); }
		}
		private DateTime _modifiedDate;

		#endregion

		#region Constructors

		public Production_ProductProductPhoto(bool addingNew) : base(addingNew)
		{
			if (addingNew)
			{
			}
		}

		[Obsolete("This default constructor is required by the DataClassAdapter, but should never be used directly in code.", true)]
		public Production_ProductProductPhoto() : this(false)
		{ }

		#endregion

	}

}
