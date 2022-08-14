using System;
using System.Data;
using Zonkey.ObjectModel;

namespace Zonkey.UnitTests.AdventureWorks.DataObjects
{
	[DataItem("ProductSubcategory", SchemaName = "Production")]
	public class Production_ProductSubcategory : DataClass
	{
		#region Data Columns (Properties)

		[DataField("ProductSubcategoryID", DbType.Int32, false, IsKeyField = true, IsAutoIncrement = true)]
		public Int32 ProductSubcategoryID
		{
			get { return _productSubcategoryID; }
			set { SetFieldValue(ref _productSubcategoryID, value); }
		}
		private Int32 _productSubcategoryID;

		[DataField("ProductCategoryID", DbType.Int32, false)]
		public Int32 ProductCategoryID
		{
			get { return _productCategoryID; }
			set { SetFieldValue(ref _productCategoryID, value); }
		}
		private Int32 _productCategoryID;

		[DataField("Name", DbType.String, false, Length = 50)]
		public string Name
		{
			get { return _name; }
			set { SetFieldValue(ref _name, value); }
		}
		private string _name;

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

		public Production_ProductSubcategory(bool addingNew) : base(addingNew)
		{
			if (addingNew)
			{
			}
		}

		[Obsolete("This default constructor is required by the DataClassAdapter, but should never be used directly in code.", true)]
		public Production_ProductSubcategory() : this(false)
		{ }

		#endregion

	}

}
