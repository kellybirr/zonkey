using System;
using System.Data;
using Zonkey.ObjectModel;

namespace Zonkey.UnitTests.AdventureWorks.DataObjects
{
	[DataItem("ProductModelProductDescriptionCulture", SchemaName = "Production")]
	public class Production_ProductModelProductDescriptionCulture : DataClass
	{
		#region Data Columns (Properties)

		[DataField("ProductModelID", DbType.Int32, false, IsKeyField = true)]
		public Int32 ProductModelID
		{
			get { return _productModelID; }
			set { SetFieldValue(ref _productModelID, value); }
		}
		private Int32 _productModelID;

		[DataField("ProductDescriptionID", DbType.Int32, false, IsKeyField = true)]
		public Int32 ProductDescriptionID
		{
			get { return _productDescriptionID; }
			set { SetFieldValue(ref _productDescriptionID, value); }
		}
		private Int32 _productDescriptionID;

		[DataField("CultureID", DbType.StringFixedLength, false, Length = 6, IsKeyField = true)]
		public string CultureID
		{
			get { return _cultureID; }
			set { SetFieldValue(ref _cultureID, value); }
		}
		private string _cultureID;

		[DataField("ModifiedDate", DbType.DateTime, false)]
		public DateTime ModifiedDate
		{
			get { return _modifiedDate; }
			set { SetFieldValue(ref _modifiedDate, value); }
		}
		private DateTime _modifiedDate;

		#endregion

		#region Constructors

		public Production_ProductModelProductDescriptionCulture(bool addingNew) : base(addingNew)
		{
			if (addingNew)
			{
			}
		}

		[Obsolete("This default constructor is required by the DataClassAdapter, but should never be used directly in code.", true)]
		public Production_ProductModelProductDescriptionCulture() : this(false)
		{ }

		#endregion

	}

}
