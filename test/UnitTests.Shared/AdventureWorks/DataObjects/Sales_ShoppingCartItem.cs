using System;
using System.Data;
using Zonkey.ObjectModel;

namespace Zonkey.UnitTests.AdventureWorks.DataObjects
{
	[DataItem("ShoppingCartItem", SchemaName = "Sales")]
	public class Sales_ShoppingCartItem : DataClass
	{
		#region Data Columns (Properties)

		[DataField("ShoppingCartItemID", DbType.Int32, false, IsKeyField = true, IsAutoIncrement = true)]
		public Int32 ShoppingCartItemID
		{
			get { return _shoppingCartItemID; }
			set { SetFieldValue(ref _shoppingCartItemID, value); }
		}
		private Int32 _shoppingCartItemID;

		[DataField("ShoppingCartID", DbType.String, false, Length = 50)]
		public string ShoppingCartID
		{
			get { return _shoppingCartID; }
			set { SetFieldValue(ref _shoppingCartID, value); }
		}
		private string _shoppingCartID;

		[DataField("Quantity", DbType.Int32, false)]
		public Int32 Quantity
		{
			get { return _quantity; }
			set { SetFieldValue(ref _quantity, value); }
		}
		private Int32 _quantity;

		[DataField("ProductID", DbType.Int32, false)]
		public Int32 ProductID
		{
			get { return _productID; }
			set { SetFieldValue(ref _productID, value); }
		}
		private Int32 _productID;

		[DataField("DateCreated", DbType.DateTime, false)]
		public DateTime DateCreated
		{
			get { return _dateCreated; }
			set { SetFieldValue(ref _dateCreated, value); }
		}
		private DateTime _dateCreated;

		[DataField("ModifiedDate", DbType.DateTime, false)]
		public DateTime ModifiedDate
		{
			get { return _modifiedDate; }
			set { SetFieldValue(ref _modifiedDate, value); }
		}
		private DateTime _modifiedDate;

		#endregion

		#region Constructors

		public Sales_ShoppingCartItem(bool addingNew) : base(addingNew)
		{
			if (addingNew)
			{
			}
		}

		[Obsolete("This default constructor is required by the DataClassAdapter, but should never be used directly in code.", true)]
		public Sales_ShoppingCartItem() : this(false)
		{ }

		#endregion

	}

}
