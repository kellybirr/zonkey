using System;
using System.Data;
using Zonkey.ObjectModel;

namespace Zonkey.UnitTests.AdventureWorks.DataObjects
{
	[DataItem("SpecialOfferProduct", SchemaName = "Sales")]
	public class Sales_SpecialOfferProduct : DataClass
	{
		#region Data Columns (Properties)

		[DataField("SpecialOfferID", DbType.Int32, false, IsKeyField = true)]
		public Int32 SpecialOfferID
		{
			get { return _specialOfferID; }
			set { SetFieldValue(ref _specialOfferID, value); }
		}
		private Int32 _specialOfferID;

		[DataField("ProductID", DbType.Int32, false, IsKeyField = true)]
		public Int32 ProductID
		{
			get { return _productID; }
			set { SetFieldValue(ref _productID, value); }
		}
		private Int32 _productID;

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

		public Sales_SpecialOfferProduct(bool addingNew) : base(addingNew)
		{
			if (addingNew)
			{
			}
		}

		[Obsolete("This default constructor is required by the DataClassAdapter, but should never be used directly in code.", true)]
		public Sales_SpecialOfferProduct() : this(false)
		{ }

		#endregion

	}

}
