using System;
using System.Data;
using Zonkey.ObjectModel;

namespace Zonkey.UnitTests.AdventureWorks.DataObjects
{
	[DataItem("ProductModel", SchemaName = "Production")]
	public class Production_ProductModel : DataClass
	{
		#region Data Columns (Properties)

		[DataField("ProductModelID", DbType.Int32, false, IsKeyField = true, IsAutoIncrement = true)]
		public Int32 ProductModelID
		{
			get { return _productModelID; }
			set { SetFieldValue(ref _productModelID, value); }
		}
		private Int32 _productModelID;

		[DataField("Name", DbType.String, false, Length = 50)]
		public string Name
		{
			get { return _name; }
			set { SetFieldValue(ref _name, value); }
		}
		private string _name;

		[DataField("CatalogDescription", DbType.String, true, Length = 2147483647)]
		public string CatalogDescription
		{
			get { return _catalogDescription; }
			set { SetFieldValue(ref _catalogDescription, value); }
		}
		private string _catalogDescription;

		[DataField("Instructions", DbType.String, true, Length = 2147483647)]
		public string Instructions
		{
			get { return _instructions; }
			set { SetFieldValue(ref _instructions, value); }
		}
		private string _instructions;

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

		public Production_ProductModel(bool addingNew) : base(addingNew)
		{
			if (addingNew)
			{
			}
		}

		[Obsolete("This default constructor is required by the DataClassAdapter, but should never be used directly in code.", true)]
		public Production_ProductModel() : this(false)
		{ }

		#endregion

	}

}
