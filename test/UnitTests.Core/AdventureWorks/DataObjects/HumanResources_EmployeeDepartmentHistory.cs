using System;
using System.Data;
using Zonkey.ObjectModel;

namespace Zonkey.UnitTests.AdventureWorks.DataObjects
{
	[DataItem("EmployeeDepartmentHistory", SchemaName = "HumanResources")]
	public class HumanResources_EmployeeDepartmentHistory : DataClass
	{
		#region Data Columns (Properties)

		[DataField("BusinessEntityID", DbType.Int32, false, IsKeyField = true)]
		public Int32 BusinessEntityID
		{
			get { return _businessEntityID; }
			set { SetFieldValue(ref _businessEntityID, value); }
		}
		private Int32 _businessEntityID;

		[DataField("DepartmentID", DbType.Int16, false, IsKeyField = true)]
		public Int16 DepartmentID
		{
			get { return _departmentID; }
			set { SetFieldValue(ref _departmentID, value); }
		}
		private Int16 _departmentID;

		[DataField("ShiftID", DbType.Byte, false, IsKeyField = true)]
		public Byte ShiftID
		{
			get { return _shiftID; }
			set { SetFieldValue(ref _shiftID, value); }
		}
		private Byte _shiftID;

		[DataField("StartDate", DbType.DateTime, false, IsKeyField = true)]
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

		[DataField("ModifiedDate", DbType.DateTime, false)]
		public DateTime ModifiedDate
		{
			get { return _modifiedDate; }
			set { SetFieldValue(ref _modifiedDate, value); }
		}
		private DateTime _modifiedDate;

		#endregion

		#region Constructors

		public HumanResources_EmployeeDepartmentHistory(bool addingNew) : base(addingNew)
		{
			if (addingNew)
			{
			}
		}

		[Obsolete("This default constructor is required by the DataClassAdapter, but should never be used directly in code.", true)]
		public HumanResources_EmployeeDepartmentHistory() : this(false)
		{ }

		#endregion

	}

}
