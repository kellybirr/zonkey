using System;
using System.Data;
using Zonkey.ObjectModel;

namespace Zonkey.UnitTests.AdventureWorks.DataObjects
{
	[DataItem("BillOfMaterials", SchemaName = "Production")]
	public class Production_BillOfMaterial : DataClass
	{
		#region Data Columns (Properties)

		[DataField("BillOfMaterialsID", DbType.Int32, false, IsKeyField = true, IsAutoIncrement = true)]
		public Int32 BillOfMaterialsID
		{
			get { return _billOfMaterialsID; }
			set { SetFieldValue(ref _billOfMaterialsID, value); }
		}
		private Int32 _billOfMaterialsID;

		[DataField("ProductAssemblyID", DbType.Int32, true)]
		public Int32? ProductAssemblyID
		{
			get { return _productAssemblyID; }
			set { SetFieldValue(ref _productAssemblyID, value); }
		}
		private Int32? _productAssemblyID;

		[DataField("ComponentID", DbType.Int32, false)]
		public Int32 ComponentID
		{
			get { return _componentID; }
			set { SetFieldValue(ref _componentID, value); }
		}
		private Int32 _componentID;

		[DataField("StartDate", DbType.DateTime, false)]
		public DateTime StartDate
		{
			get { return _startDate; }
			set { SetFieldValue(ref _startDate, value); }
		}
		private DateTime _startDate;

		[DataField("EndDate", DbType.DateTime, true)]
		public DateTime? EndDate
		{
			get { return _endDate; }
			set { SetFieldValue(ref _endDate, value); }
		}
		private DateTime? _endDate;

		[DataField("UnitMeasureCode", DbType.StringFixedLength, false, Length = 3)]
		public string UnitMeasureCode
		{
			get { return _unitMeasureCode; }
			set { SetFieldValue(ref _unitMeasureCode, value); }
		}
		private string _unitMeasureCode;

		[DataField("BOMLevel", DbType.Int16, false)]
		public Int16 BOMLevel
		{
			get { return _bOMLevel; }
			set { SetFieldValue(ref _bOMLevel, value); }
		}
		private Int16 _bOMLevel;

		[DataField("PerAssemblyQty", DbType.Decimal, false)]
		public decimal PerAssemblyQty
		{
			get { return _perAssemblyQty; }
			set { SetFieldValue(ref _perAssemblyQty, value); }
		}
		private decimal _perAssemblyQty;

		[DataField("ModifiedDate", DbType.DateTime, false)]
		public DateTime ModifiedDate
		{
			get { return _modifiedDate; }
			set { SetFieldValue(ref _modifiedDate, value); }
		}
		private DateTime _modifiedDate;

		#endregion

		#region Constructors

		public Production_BillOfMaterial(bool addingNew) : base(addingNew)
		{
			if (addingNew)
			{
			}
		}

		[Obsolete("This default constructor is required by the DataClassAdapter, but should never be used directly in code.", true)]
		public Production_BillOfMaterial() : this(false)
		{ }

		#endregion

	}

}
