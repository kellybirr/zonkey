using System;
using System.Data;
using Zonkey.ObjectModel;

namespace Zonkey.UnitTests.AdventureWorks.DataObjects
{
	[DataItem("Currency", SchemaName = "Sales")]
	public class Sales_Currency : DataClass
	{
		#region Data Columns (Properties)

		[DataField("CurrencyCode", DbType.StringFixedLength, false, Length = 3, IsKeyField = true)]
		public string CurrencyCode
		{
			get { return _currencyCode; }
			set { SetFieldValue(ref _currencyCode, value); }
		}
		private string _currencyCode;

		[DataField("Name", DbType.String, false, Length = 50)]
		public string Name
		{
			get { return _name; }
			set { SetFieldValue(ref _name, value); }
		}
		private string _name;

		[DataField("ModifiedDate", DbType.DateTime, false)]
		public DateTime ModifiedDate
		{
			get { return _modifiedDate; }
			set { SetFieldValue(ref _modifiedDate, value); }
		}
		private DateTime _modifiedDate;

		#endregion

		#region Constructors

		public Sales_Currency(bool addingNew) : base(addingNew)
		{
			if (addingNew)
			{
			}
		}

		[Obsolete("This default constructor is required by the DataClassAdapter, but should never be used directly in code.", true)]
		public Sales_Currency() : this(false)
		{ }

		#endregion

	}

}
