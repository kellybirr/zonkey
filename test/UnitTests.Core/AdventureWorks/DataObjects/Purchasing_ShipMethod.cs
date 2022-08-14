using System;
using System.Data;
using Zonkey.ObjectModel;

namespace Zonkey.UnitTests.AdventureWorks.DataObjects
{
	[DataItem("ShipMethod", SchemaName = "Purchasing")]
	public class Purchasing_ShipMethod : DataClass
	{
		#region Data Columns (Properties)

		[DataField("ShipMethodID", DbType.Int32, false, IsKeyField = true, IsAutoIncrement = true)]
		public Int32 ShipMethodID
		{
			get { return _shipMethodID; }
			set { SetFieldValue(ref _shipMethodID, value); }
		}
		private Int32 _shipMethodID;

		[DataField("Name", DbType.String, false, Length = 50)]
		public string Name
		{
			get { return _name; }
			set { SetFieldValue(ref _name, value); }
		}
		private string _name;

		[DataField("ShipBase", DbType.Decimal, false)]
		public decimal ShipBase
		{
			get { return _shipBase; }
			set { SetFieldValue(ref _shipBase, value); }
		}
		private decimal _shipBase;

		[DataField("ShipRate", DbType.Decimal, false)]
		public decimal ShipRate
		{
			get { return _shipRate; }
			set { SetFieldValue(ref _shipRate, value); }
		}
		private decimal _shipRate;

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

		public Purchasing_ShipMethod(bool addingNew) : base(addingNew)
		{
			if (addingNew)
			{
			}
		}

		[Obsolete("This default constructor is required by the DataClassAdapter, but should never be used directly in code.", true)]
		public Purchasing_ShipMethod() : this(false)
		{ }

		#endregion

	}

}
