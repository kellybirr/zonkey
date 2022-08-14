using System;
using System.Data;
using Zonkey.ObjectModel;

namespace Zonkey.UnitTests.AdventureWorks.DataObjects
{
	[DataItem("CreditCard", SchemaName = "Sales")]
	public class Sales_CreditCard : DataClass
	{
		#region Data Columns (Properties)

		[DataField("CreditCardID", DbType.Int32, false, IsKeyField = true, IsAutoIncrement = true)]
		public Int32 CreditCardID
		{
			get { return _creditCardID; }
			set { SetFieldValue(ref _creditCardID, value); }
		}
		private Int32 _creditCardID;

		[DataField("CardType", DbType.String, false, Length = 50)]
		public string CardType
		{
			get { return _cardType; }
			set { SetFieldValue(ref _cardType, value); }
		}
		private string _cardType;

		[DataField("CardNumber", DbType.String, false, Length = 25)]
		public string CardNumber
		{
			get { return _cardNumber; }
			set { SetFieldValue(ref _cardNumber, value); }
		}
		private string _cardNumber;

		[DataField("ExpMonth", DbType.Byte, false)]
		public Byte ExpMonth
		{
			get { return _expMonth; }
			set { SetFieldValue(ref _expMonth, value); }
		}
		private Byte _expMonth;

		[DataField("ExpYear", DbType.Int16, false)]
		public Int16 ExpYear
		{
			get { return _expYear; }
			set { SetFieldValue(ref _expYear, value); }
		}
		private Int16 _expYear;

		[DataField("ModifiedDate", DbType.DateTime, false)]
		public DateTime ModifiedDate
		{
			get { return _modifiedDate; }
			set { SetFieldValue(ref _modifiedDate, value); }
		}
		private DateTime _modifiedDate;

		#endregion

		#region Constructors

		public Sales_CreditCard(bool addingNew) : base(addingNew)
		{
			if (addingNew)
			{
			}
		}

		[Obsolete("This default constructor is required by the DataClassAdapter, but should never be used directly in code.", true)]
		public Sales_CreditCard() : this(false)
		{ }

		#endregion

	}

}
