using System;
using System.Data;
using Zonkey.ObjectModel;

namespace Zonkey.UnitTests.AdventureWorks.DataObjects
{
	[DataItem("CurrencyRate", SchemaName = "Sales")]
	public class Sales_CurrencyRate : DataClass
	{
		#region Data Columns (Properties)

		[DataField("CurrencyRateID", DbType.Int32, false, IsKeyField = true, IsAutoIncrement = true)]
		public Int32 CurrencyRateID
		{
			get { return _currencyRateID; }
			set { SetFieldValue(ref _currencyRateID, value); }
		}
		private Int32 _currencyRateID;

		[DataField("CurrencyRateDate", DbType.DateTime, false)]
		public DateTime CurrencyRateDate
		{
			get { return _currencyRateDate; }
			set { SetFieldValue(ref _currencyRateDate, value); }
		}
		private DateTime _currencyRateDate;

		[DataField("FromCurrencyCode", DbType.StringFixedLength, false, Length = 3)]
		public string FromCurrencyCode
		{
			get { return _fromCurrencyCode; }
			set { SetFieldValue(ref _fromCurrencyCode, value); }
		}
		private string _fromCurrencyCode;

		[DataField("ToCurrencyCode", DbType.StringFixedLength, false, Length = 3)]
		public string ToCurrencyCode
		{
			get { return _toCurrencyCode; }
			set { SetFieldValue(ref _toCurrencyCode, value); }
		}
		private string _toCurrencyCode;

		[DataField("AverageRate", DbType.Decimal, false)]
		public decimal AverageRate
		{
			get { return _averageRate; }
			set { SetFieldValue(ref _averageRate, value); }
		}
		private decimal _averageRate;

		[DataField("EndOfDayRate", DbType.Decimal, false)]
		public decimal EndOfDayRate
		{
			get { return _endOfDayRate; }
			set { SetFieldValue(ref _endOfDayRate, value); }
		}
		private decimal _endOfDayRate;

		[DataField("ModifiedDate", DbType.DateTime, false)]
		public DateTime ModifiedDate
		{
			get { return _modifiedDate; }
			set { SetFieldValue(ref _modifiedDate, value); }
		}
		private DateTime _modifiedDate;

		#endregion

		#region Constructors

		public Sales_CurrencyRate(bool addingNew) : base(addingNew)
		{
			if (addingNew)
			{
			}
		}

		[Obsolete("This default constructor is required by the DataClassAdapter, but should never be used directly in code.", true)]
		public Sales_CurrencyRate() : this(false)
		{ }

		#endregion

	}

}
