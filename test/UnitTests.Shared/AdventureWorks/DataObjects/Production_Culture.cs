using System;
using System.Data;
using Zonkey.ObjectModel;

namespace Zonkey.UnitTests.AdventureWorks.DataObjects
{
	[DataItem("Culture", SchemaName = "Production")]
	public class Production_Culture : DataClass
	{
		#region Data Columns (Properties)

		[DataField("CultureID", DbType.StringFixedLength, false, Length = 6, IsKeyField = true)]
		public string CultureID
		{
			get { return _cultureID; }
			set { SetFieldValue(ref _cultureID, value); }
		}
		private string _cultureID;

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

		public Production_Culture(bool addingNew) : base(addingNew)
		{
			if (addingNew)
			{
			}
		}

		[Obsolete("This default constructor is required by the DataClassAdapter, but should never be used directly in code.", true)]
		public Production_Culture() : this(false)
		{ }

		#endregion

	}

}
