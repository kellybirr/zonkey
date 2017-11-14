using System;
using System.Data;
using Zonkey.ObjectModel;

namespace Zonkey.UnitTests.AdventureWorks.DataObjects
{
	[DataItem("Product", SchemaName = "Production")]
	public class Production_Product : DataClass
	{
		#region Data Columns (Properties)

		[DataField("ProductID", DbType.Int32, false, IsKeyField = true, IsAutoIncrement = true)]
		public Int32 ProductID
		{
			get { return _productID; }
			set { SetFieldValue(ref _productID, value); }
		}
		private Int32 _productID;

		[DataField("Name", DbType.String, false, Length = 50)]
		public string Name
		{
			get { return _name; }
			set { SetFieldValue(ref _name, value); }
		}
		private string _name;

		[DataField("ProductNumber", DbType.String, false, Length = 25)]
		public string ProductNumber
		{
			get { return _productNumber; }
			set { SetFieldValue(ref _productNumber, value); }
		}
		private string _productNumber;

		[DataField("MakeFlag", DbType.Boolean, false)]
		public bool MakeFlag
		{
			get { return _makeFlag; }
			set { SetFieldValue(ref _makeFlag, value); }
		}
		private bool _makeFlag;

		[DataField("FinishedGoodsFlag", DbType.Boolean, false)]
		public bool FinishedGoodsFlag
		{
			get { return _finishedGoodsFlag; }
			set { SetFieldValue(ref _finishedGoodsFlag, value); }
		}
		private bool _finishedGoodsFlag;

		[DataField("Color", DbType.String, true, Length = 15)]
		public string Color
		{
			get { return _color; }
			set { SetFieldValue(ref _color, value); }
		}
		private string _color;

		[DataField("SafetyStockLevel", DbType.Int16, false)]
		public Int16 SafetyStockLevel
		{
			get { return _safetyStockLevel; }
			set { SetFieldValue(ref _safetyStockLevel, value); }
		}
		private Int16 _safetyStockLevel;

		[DataField("ReorderPoint", DbType.Int16, false)]
		public Int16 ReorderPoint
		{
			get { return _reorderPoint; }
			set { SetFieldValue(ref _reorderPoint, value); }
		}
		private Int16 _reorderPoint;

		[DataField("StandardCost", DbType.Decimal, false)]
		public decimal StandardCost
		{
			get { return _standardCost; }
			set { SetFieldValue(ref _standardCost, value); }
		}
		private decimal _standardCost;

		[DataField("ListPrice", DbType.Decimal, false)]
		public decimal ListPrice
		{
			get { return _listPrice; }
			set { SetFieldValue(ref _listPrice, value); }
		}
		private decimal _listPrice;

		[DataField("Size", DbType.String, true, Length = 5)]
		public string Size
		{
			get { return _size; }
			set { SetFieldValue(ref _size, value); }
		}
		private string _size;

		[DataField("SizeUnitMeasureCode", DbType.StringFixedLength, true, Length = 3)]
		public string SizeUnitMeasureCode
		{
			get { return _sizeUnitMeasureCode; }
			set { SetFieldValue(ref _sizeUnitMeasureCode, value); }
		}
		private string _sizeUnitMeasureCode;

		[DataField("WeightUnitMeasureCode", DbType.StringFixedLength, true, Length = 3)]
		public string WeightUnitMeasureCode
		{
			get { return _weightUnitMeasureCode; }
			set { SetFieldValue(ref _weightUnitMeasureCode, value); }
		}
		private string _weightUnitMeasureCode;

		[DataField("Weight", DbType.Decimal, true)]
		public decimal Weight
		{
			get { return _weight; }
			set { SetFieldValue(ref _weight, value); }
		}
		private decimal _weight;

		[DataField("DaysToManufacture", DbType.Int32, false)]
		public Int32 DaysToManufacture
		{
			get { return _daysToManufacture; }
			set { SetFieldValue(ref _daysToManufacture, value); }
		}
		private Int32 _daysToManufacture;

		[DataField("ProductLine", DbType.StringFixedLength, true, Length = 2)]
		public string ProductLine
		{
			get { return _productLine; }
			set { SetFieldValue(ref _productLine, value); }
		}
		private string _productLine;

		[DataField("Class", DbType.StringFixedLength, true, Length = 2)]
		public string Class
		{
			get { return _class; }
			set { SetFieldValue(ref _class, value); }
		}
		private string _class;

		[DataField("Style", DbType.StringFixedLength, true, Length = 2)]
		public string Style
		{
			get { return _style; }
			set { SetFieldValue(ref _style, value); }
		}
		private string _style;

		[DataField("ProductSubcategoryID", DbType.Int32, true)]
		public Int32? ProductSubcategoryID
		{
			get { return _productSubcategoryID; }
			set { SetFieldValue(ref _productSubcategoryID, value); }
		}
		private Int32? _productSubcategoryID;

		[DataField("ProductModelID", DbType.Int32, true)]
		public Int32? ProductModelID
		{
			get { return _productModelID; }
			set { SetFieldValue(ref _productModelID, value); }
		}
		private Int32? _productModelID;

		[DataField("SellStartDate", DbType.DateTime, false)]
		public DateTime SellStartDate
		{
			get { return _sellStartDate; }
			set { SetFieldValue(ref _sellStartDate, value); }
		}
		private DateTime _sellStartDate;

		[DataField("SellEndDate", DbType.DateTime, true)]
		public DateTime? SellEndDate
		{
			get { return _sellEndDate; }
			set { SetFieldValue(ref _sellEndDate, value); }
		}
		private DateTime? _sellEndDate;

		[DataField("DiscontinuedDate", DbType.DateTime, true)]
		public DateTime? DiscontinuedDate
		{
			get { return _discontinuedDate; }
			set { SetFieldValue(ref _discontinuedDate, value); }
		}
		private DateTime? _discontinuedDate;

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

		public Production_Product(bool addingNew) : base(addingNew)
		{
			if (addingNew)
			{
			}
		}

		[Obsolete("This default constructor is required by the DataClassAdapter, but should never be used directly in code.", true)]
		public Production_Product() : this(false)
		{ }

		#endregion

	}

}
