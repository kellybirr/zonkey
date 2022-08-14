using System;
using System.Data;
using Zonkey.ObjectModel;

namespace Zonkey.UnitTests.AdventureWorks.DataObjects
{
	[DataItem("SpecialOffer", SchemaName = "Sales")]
	public class Sales_SpecialOffer : DataClass
	{
		#region Data Columns (Properties)

		[DataField("SpecialOfferID", DbType.Int32, false, IsKeyField = true, IsAutoIncrement = true)]
		public Int32 SpecialOfferID
		{
			get { return _specialOfferID; }
			set { SetFieldValue(ref _specialOfferID, value); }
		}
		private Int32 _specialOfferID;

		[DataField("Description", DbType.String, false, Length = 255)]
		public string Description
		{
			get { return _description; }
			set { SetFieldValue(ref _description, value); }
		}
		private string _description;

		[DataField("DiscountPct", DbType.Decimal, false)]
		public decimal DiscountPct
		{
			get { return _discountPct; }
			set { SetFieldValue(ref _discountPct, value); }
		}
		private decimal _discountPct;

		[DataField("Type", DbType.String, false, Length = 50)]
		public string Type
		{
			get { return _type; }
			set { SetFieldValue(ref _type, value); }
		}
		private string _type;

		[DataField("Category", DbType.String, false, Length = 50)]
		public string Category
		{
			get { return _category; }
			set { SetFieldValue(ref _category, value); }
		}
		private string _category;

		[DataField("StartDate", DbType.DateTime, false)]
		public DateTime StartDate
		{
			get { return _startDate; }
			set { SetFieldValue(ref _startDate, value); }
		}
		private DateTime _startDate;

		[DataField("EndDate", DbType.DateTime, false)]
		public DateTime EndDate
		{
			get { return _endDate; }
			set { SetFieldValue(ref _endDate, value); }
		}
		private DateTime _endDate;

		[DataField("MinQty", DbType.Int32, false)]
		public Int32 MinQty
		{
			get { return _minQty; }
			set { SetFieldValue(ref _minQty, value); }
		}
		private Int32 _minQty;

		[DataField("MaxQty", DbType.Int32, true)]
		public Int32? MaxQty
		{
			get { return _maxQty; }
			set { SetFieldValue(ref _maxQty, value); }
		}
		private Int32? _maxQty;

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

		public Sales_SpecialOffer(bool addingNew) : base(addingNew)
		{
			if (addingNew)
			{
			}
		}

		[Obsolete("This default constructor is required by the DataClassAdapter, but should never be used directly in code.", true)]
		public Sales_SpecialOffer() : this(false)
		{ }

		#endregion

	}

}
