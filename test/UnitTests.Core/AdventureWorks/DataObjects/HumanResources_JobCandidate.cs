using System;
using System.Data;
using Zonkey.ObjectModel;

namespace Zonkey.UnitTests.AdventureWorks.DataObjects
{
	[DataItem("JobCandidate", SchemaName = "HumanResources")]
	public class HumanResources_JobCandidate : DataClass
	{
		#region Data Columns (Properties)

		[DataField("JobCandidateID", DbType.Int32, false, IsKeyField = true, IsAutoIncrement = true)]
		public Int32 JobCandidateID
		{
			get { return _jobCandidateID; }
			set { SetFieldValue(ref _jobCandidateID, value); }
		}
		private Int32 _jobCandidateID;

		[DataField("BusinessEntityID", DbType.Int32, true)]
		public Int32? BusinessEntityID
		{
			get { return _businessEntityID; }
			set { SetFieldValue(ref _businessEntityID, value); }
		}
		private Int32? _businessEntityID;

		[DataField("Resume", DbType.String, true, Length = 2147483647)]
		public string Resume
		{
			get { return _resume; }
			set { SetFieldValue(ref _resume, value); }
		}
		private string _resume;

		[DataField("ModifiedDate", DbType.DateTime, false)]
		public DateTime ModifiedDate
		{
			get { return _modifiedDate; }
			set { SetFieldValue(ref _modifiedDate, value); }
		}
		private DateTime _modifiedDate;

		#endregion

		#region Constructors

		public HumanResources_JobCandidate(bool addingNew) : base(addingNew)
		{
			if (addingNew)
			{
			}
		}

		[Obsolete("This default constructor is required by the DataClassAdapter, but should never be used directly in code.", true)]
		public HumanResources_JobCandidate() : this(false)
		{ }

		#endregion

	}

}
