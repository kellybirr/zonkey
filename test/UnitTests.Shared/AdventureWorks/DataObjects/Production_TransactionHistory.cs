using System;
using System.Data;
using Zonkey.ObjectModel;

namespace Zonkey.UnitTests.AdventureWorks.DataObjects
{
	[DataItem("TransactionHistory", SchemaName = "Production")]
	public class Production_TransactionHistory : DataClass
	{
		#region Data Columns (Properties)

		[DataField("TransactionID", DbType.Int32, false, IsKeyField = true, IsAutoIncrement = true)]
		public Int32 TransactionID
		{
			get { return _transactionID; }
			set { SetFieldValue(ref _transactionID, value); }
		}
		private Int32 _transactionID;

		[DataField("ProductID", DbType.Int32, false)]
		public Int32 ProductID
		{
			get { return _productID; }
			set { SetFieldValue(ref _productID, value); }
		}
		private Int32 _productID;

		[DataField("ReferenceOrderID", DbType.Int32, false)]
		public Int32 ReferenceOrderID
		{
			get { return _referenceOrderID; }
			set { SetFieldValue(ref _referenceOrderID, value); }
		}
		private Int32 _referenceOrderID;

		[DataField("ReferenceOrderLineID", DbType.Int32, false)]
		public Int32 ReferenceOrderLineID
		{
			get { return _referenceOrderLineID; }
			set { SetFieldValue(ref _referenceOrderLineID, value); }
		}
		private Int32 _referenceOrderLineID;

		[DataField("TransactionDate", DbType.DateTime, false)]
		public DateTime TransactionDate
		{
			get { return _transactionDate; }
			set { SetFieldValue(ref _transactionDate, value); }
		}
		private DateTime _transactionDate;

		[DataField("TransactionType", DbType.StringFixedLength, false, Length = 1)]
		public string TransactionType
		{
			get { return _transactionType; }
			set { SetFieldValue(ref _transactionType, value); }
		}
		private string _transactionType;

		[DataField("Quantity", DbType.Int32, false)]
		public Int32 Quantity
		{
			get { return _quantity; }
			set { SetFieldValue(ref _quantity, value); }
		}
		private Int32 _quantity;

		[DataField("ActualCost", DbType.Decimal, false)]
		public decimal ActualCost
		{
			get { return _actualCost; }
			set { SetFieldValue(ref _actualCost, value); }
		}
		private decimal _actualCost;

		[DataField("ModifiedDate", DbType.DateTime, false)]
		public DateTime ModifiedDate
		{
			get { return _modifiedDate; }
			set { SetFieldValue(ref _modifiedDate, value); }
		}
		private DateTime _modifiedDate;

		#endregion

		#region Constructors

		public Production_TransactionHistory(bool addingNew) : base(addingNew)
		{
			if (addingNew)
			{
			}
		}

		[Obsolete("This default constructor is required by the DataClassAdapter, but should never be used directly in code.", true)]
		public Production_TransactionHistory() : this(false)
		{ }

		#endregion

	}

}
