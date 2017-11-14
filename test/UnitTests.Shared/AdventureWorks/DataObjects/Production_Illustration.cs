using System;
using System.Data;
using Zonkey.ObjectModel;

namespace Zonkey.UnitTests.AdventureWorks.DataObjects
{
	[DataItem("Illustration", SchemaName = "Production")]
	public class Production_Illustration : DataClass
	{
		#region Data Columns (Properties)

		[DataField("IllustrationID", DbType.Int32, false, IsKeyField = true, IsAutoIncrement = true)]
		public Int32 IllustrationID
		{
			get { return _illustrationID; }
			set { SetFieldValue(ref _illustrationID, value); }
		}
		private Int32 _illustrationID;

		[DataField("Diagram", DbType.String, true, Length = 2147483647)]
		public string Diagram
		{
			get { return _diagram; }
			set { SetFieldValue(ref _diagram, value); }
		}
		private string _diagram;

		[DataField("ModifiedDate", DbType.DateTime, false)]
		public DateTime ModifiedDate
		{
			get { return _modifiedDate; }
			set { SetFieldValue(ref _modifiedDate, value); }
		}
		private DateTime _modifiedDate;

		#endregion

		#region Constructors

		public Production_Illustration(bool addingNew) : base(addingNew)
		{
			if (addingNew)
			{
			}
		}

		[Obsolete("This default constructor is required by the DataClassAdapter, but should never be used directly in code.", true)]
		public Production_Illustration() : this(false)
		{ }

		#endregion

	}

}
