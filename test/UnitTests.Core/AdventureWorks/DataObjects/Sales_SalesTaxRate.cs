using System;
using System.Data;
using Zonkey.ObjectModel;

namespace Zonkey.UnitTests.AdventureWorks.DataObjects
{
	[DataItem("SalesTaxRate", SchemaName = "Sales")]
	public class Sales_SalesTaxRate : DataClass
	{
		#region Data Columns (Properties)

		[DataField("SalesTaxRateID", DbType.Int32, false, IsKeyField = true, IsAutoIncrement = true)]
		public Int32 SalesTaxRateID
		{
			get { return _salesTaxRateID; }
			set { SetFieldValue(ref _salesTaxRateID, value); }
		}
		private Int32 _salesTaxRateID;

		[DataField("StateProvinceID", DbType.Int32, false)]
		public Int32 StateProvinceID
		{
			get { return _stateProvinceID; }
			set { SetFieldValue(ref _stateProvinceID, value); }
		}
		private Int32 _stateProvinceID;

		[DataField("TaxType", DbType.Byte, false)]
		public Byte TaxType
		{
			get { return _taxType; }
			set { SetFieldValue(ref _taxType, value); }
		}
		private Byte _taxType;

		[DataField("TaxRate", DbType.Decimal, false)]
		public decimal TaxRate
		{
			get { return _taxRate; }
			set { SetFieldValue(ref _taxRate, value); }
		}
		private decimal _taxRate;

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

		public Sales_SalesTaxRate(bool addingNew) : base(addingNew)
		{
			if (addingNew)
			{
			}
		}

		[Obsolete("This default constructor is required by the DataClassAdapter, but should never be used directly in code.", true)]
		public Sales_SalesTaxRate() : this(false)
		{ }

		#endregion

	}

}
