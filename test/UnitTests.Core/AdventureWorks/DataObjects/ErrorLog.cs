using System;
using System.Data;
using Zonkey.ObjectModel;

namespace Zonkey.UnitTests.AdventureWorks.DataObjects
{
	[DataItem("ErrorLog")]
	public class ErrorLog : DataClass
	{
		#region Data Columns (Properties)

		[DataField("ErrorLogID", DbType.Int32, false, IsKeyField = true, IsAutoIncrement = true)]
		public Int32 ErrorLogID
		{
			get { return _errorLogID; }
			set { SetFieldValue(ref _errorLogID, value); }
		}
		private Int32 _errorLogID;

		[DataField("ErrorTime", DbType.DateTime, false)]
		public DateTime ErrorTime
		{
			get { return _errorTime; }
			set { SetFieldValue(ref _errorTime, value); }
		}
		private DateTime _errorTime;

		[DataField("UserName", DbType.String, false, Length = 128)]
		public string UserName
		{
			get { return _userName; }
			set { SetFieldValue(ref _userName, value); }
		}
		private string _userName;

		[DataField("ErrorNumber", DbType.Int32, false)]
		public Int32 ErrorNumber
		{
			get { return _errorNumber; }
			set { SetFieldValue(ref _errorNumber, value); }
		}
		private Int32 _errorNumber;

		[DataField("ErrorSeverity", DbType.Int32, true)]
		public Int32? ErrorSeverity
		{
			get { return _errorSeverity; }
			set { SetFieldValue(ref _errorSeverity, value); }
		}
		private Int32? _errorSeverity;

		[DataField("ErrorState", DbType.Int32, true)]
		public Int32? ErrorState
		{
			get { return _errorState; }
			set { SetFieldValue(ref _errorState, value); }
		}
		private Int32? _errorState;

		[DataField("ErrorProcedure", DbType.String, true, Length = 126)]
		public string ErrorProcedure
		{
			get { return _errorProcedure; }
			set { SetFieldValue(ref _errorProcedure, value); }
		}
		private string _errorProcedure;

		[DataField("ErrorLine", DbType.Int32, true)]
		public Int32? ErrorLine
		{
			get { return _errorLine; }
			set { SetFieldValue(ref _errorLine, value); }
		}
		private Int32? _errorLine;

		[DataField("ErrorMessage", DbType.String, false, Length = 4000)]
		public string ErrorMessage
		{
			get { return _errorMessage; }
			set { SetFieldValue(ref _errorMessage, value); }
		}
		private string _errorMessage;

		#endregion

		#region Constructors

		public ErrorLog(bool addingNew) : base(addingNew)
		{
			if (addingNew)
			{
			}
		}

		[Obsolete("This default constructor is required by the DataClassAdapter, but should never be used directly in code.", true)]
		public ErrorLog() : this(false)
		{ }

		#endregion

	}

}
