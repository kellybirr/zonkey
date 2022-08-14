using System;
using System.Data;
using Zonkey.ObjectModel;

namespace Zonkey.UnitTests.AdventureWorks.DataObjects
{
	[DataItem("UnitMeasure", SchemaName = "Production")]
	public class Production_UnitMeasure : DataClass
	{
		#region Data Columns (Properties)

		[DataField("UnitMeasureCode", DbType.StringFixedLength, false, Length = 3, IsKeyField = true)]
		public string UnitMeasureCode
		{
			get { return _unitMeasureCode; }
			set { SetFieldValue(ref _unitMeasureCode, value); }
		}
		private string _unitMeasureCode;

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

		public Production_UnitMeasure(bool addingNew) : base(addingNew)
		{
			if (addingNew)
			{
			}
		}

		[Obsolete("This default constructor is required by the DataClassAdapter, but should never be used directly in code.", true)]
		public Production_UnitMeasure() : this(false)
		{ }

		#endregion

	}

}
