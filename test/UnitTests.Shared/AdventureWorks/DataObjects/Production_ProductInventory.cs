using System;
using System.Data;
using Zonkey.ObjectModel;

namespace Zonkey.UnitTests.AdventureWorks.DataObjects
{
	[DataItem("ProductInventory", SchemaName = "Production")]
	public class Production_ProductInventory : DataClass
	{
		#region Data Columns (Properties)

		[DataField("ProductID", DbType.Int32, false, IsKeyField = true)]
		public Int32 ProductID
		{
			get { return _productID; }
			set { SetFieldValue(ref _productID, value); }
		}
		private Int32 _productID;

		[DataField("LocationID", DbType.Int16, false, IsKeyField = true)]
		public Int16 LocationID
		{
			get { return _locationID; }
			set { SetFieldValue(ref _locationID, value); }
		}
		private Int16 _locationID;

		[DataField("Shelf", DbType.String, false, Length = 10)]
		public string Shelf
		{
			get { return _shelf; }
			set { SetFieldValue(ref _shelf, value); }
		}
		private string _shelf;

		[DataField("Bin", DbType.Byte, false)]
		public Byte Bin
		{
			get { return _bin; }
			set { SetFieldValue(ref _bin, value); }
		}
		private Byte _bin;

		[DataField("Quantity", DbType.Int16, false)]
		public Int16 Quantity
		{
			get { return _quantity; }
			set { SetFieldValue(ref _quantity, value); }
		}
		private Int16 _quantity;

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

		public Production_ProductInventory(bool addingNew) : base(addingNew)
		{
			if (addingNew)
			{
			}
		}

		[Obsolete("This default constructor is required by the DataClassAdapter, but should never be used directly in code.", true)]
		public Production_ProductInventory() : this(false)
		{ }

		#endregion

	}

}
