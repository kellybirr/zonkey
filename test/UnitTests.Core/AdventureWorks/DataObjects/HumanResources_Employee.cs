using System;
using System.Data;
using Zonkey.ObjectModel;

namespace Zonkey.UnitTests.AdventureWorks.DataObjects
{
	[DataItem("Employee", SchemaName = "HumanResources")]
	public class HumanResources_Employee : DataClass
	{
		#region Data Columns (Properties)

		[DataField("BusinessEntityID", DbType.Int32, false, IsKeyField = true)]
		public Int32 BusinessEntityID
		{
			get { return _businessEntityID; }
			set { SetFieldValue(ref _businessEntityID, value); }
		}
		private Int32 _businessEntityID;

		[DataField("NationalIDNumber", DbType.String, false, Length = 15)]
		public string NationalIDNumber
		{
			get { return _nationalIDNumber; }
			set { SetFieldValue(ref _nationalIDNumber, value); }
		}
		private string _nationalIDNumber;

		[DataField("LoginID", DbType.String, false, Length = 256)]
		public string LoginID
		{
			get { return _loginID; }
			set { SetFieldValue(ref _loginID, value); }
		}
		private string _loginID;

		[DataField("OrganizationNode", DbType.String, true)]
		public string OrganizationNode
		{
			get { return _organizationNode; }
			set { SetFieldValue(ref _organizationNode, value); }
		}
		private string _organizationNode;

		[DataField("OrganizationLevel", DbType.Int16, true)]
		public Int16? OrganizationLevel
		{
			get { return _organizationLevel; }
			set { SetFieldValue(ref _organizationLevel, value); }
		}
		private Int16? _organizationLevel;

		[DataField("JobTitle", DbType.String, false, Length = 50)]
		public string JobTitle
		{
			get { return _jobTitle; }
			set { SetFieldValue(ref _jobTitle, value); }
		}
		private string _jobTitle;

		[DataField("BirthDate", DbType.DateTime, false)]
		public DateTime BirthDate
		{
			get { return _birthDate; }
			set { SetFieldValue(ref _birthDate, value); }
		}
		private DateTime _birthDate;

		[DataField("MaritalStatus", DbType.StringFixedLength, false, Length = 1)]
		public string MaritalStatus
		{
			get { return _maritalStatus; }
			set { SetFieldValue(ref _maritalStatus, value); }
		}
		private string _maritalStatus;

		[DataField("Gender", DbType.StringFixedLength, false, Length = 1)]
		public string Gender
		{
			get { return _gender; }
			set { SetFieldValue(ref _gender, value); }
		}
		private string _gender;

		[DataField("HireDate", DbType.DateTime, false)]
		public DateTime HireDate
		{
			get { return _hireDate; }
			set { SetFieldValue(ref _hireDate, value); }
		}
		private DateTime _hireDate;

		[DataField("SalariedFlag", DbType.Boolean, false)]
		public bool SalariedFlag
		{
			get { return _salariedFlag; }
			set { SetFieldValue(ref _salariedFlag, value); }
		}
		private bool _salariedFlag;

		[DataField("VacationHours", DbType.Int16, false)]
		public Int16 VacationHours
		{
			get { return _vacationHours; }
			set { SetFieldValue(ref _vacationHours, value); }
		}
		private Int16 _vacationHours;

		[DataField("SickLeaveHours", DbType.Int16, false)]
		public Int16 SickLeaveHours
		{
			get { return _sickLeaveHours; }
			set { SetFieldValue(ref _sickLeaveHours, value); }
		}
		private Int16 _sickLeaveHours;

		[DataField("CurrentFlag", DbType.Boolean, false)]
		public bool CurrentFlag
		{
			get { return _currentFlag; }
			set { SetFieldValue(ref _currentFlag, value); }
		}
		private bool _currentFlag;

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

		public HumanResources_Employee(bool addingNew) : base(addingNew)
		{
			if (addingNew)
			{
			}
		}

		[Obsolete("This default constructor is required by the DataClassAdapter, but should never be used directly in code.", true)]
		public HumanResources_Employee() : this(false)
		{ }

		#endregion

	}

}
