using System;
using System.Data;
using Zonkey.ObjectModel;

namespace Zonkey.UnitTests.AdventureWorks.DataObjects
{
	[DataItem("Department", SchemaName = "HumanResources")]
	public class HumanResources_Department : DataClass
	{
		#region Data Columns (Properties)

		[DataField("DepartmentID", DbType.Int16, false, IsKeyField = true, IsAutoIncrement = true)]
		public Int16 DepartmentID
		{
			get { return _departmentID; }
			set { SetFieldValue(ref _departmentID, value); }
		}
		private Int16 _departmentID;

		[DataField("Name", DbType.String, false, Length = 50)]
		public string Name
		{
			get { return _name; }
			set { SetFieldValue(ref _name, value); }
		}
		private string _name;

		[DataField("GroupName", DbType.String, false, Length = 50)]
		public string GroupName
		{
			get { return _groupName; }
			set { SetFieldValue(ref _groupName, value); }
		}
		private string _groupName;

		[DataField("ModifiedDate", DbType.DateTime, false)]
		public DateTime ModifiedDate
		{
			get { return _modifiedDate; }
			set { SetFieldValue(ref _modifiedDate, value); }
		}
		private DateTime _modifiedDate;

		#endregion

		#region Constructors

		public HumanResources_Department(bool addingNew) : base(addingNew)
		{
			if (addingNew)
			{
			}
		}

		[Obsolete("This default constructor is required by the DataClassAdapter, but should never be used directly in code.", true)]
		public HumanResources_Department() : this(false)
		{ }

		#endregion

	}

}
