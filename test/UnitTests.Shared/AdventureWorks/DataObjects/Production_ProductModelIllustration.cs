using System;
using System.Data;
using Zonkey.ObjectModel;

namespace Zonkey.UnitTests.AdventureWorks.DataObjects
{
	[DataItem("ProductModelIllustration", SchemaName = "Production")]
	public class Production_ProductModelIllustration : DataClass
	{
		#region Data Columns (Properties)

		[DataField("ProductModelID", DbType.Int32, false, IsKeyField = true)]
		public Int32 ProductModelID
		{
			get { return _productModelID; }
			set { SetFieldValue(ref _productModelID, value); }
		}
		private Int32 _productModelID;

		[DataField("IllustrationID", DbType.Int32, false, IsKeyField = true)]
		public Int32 IllustrationID
		{
			get { return _illustrationID; }
			set { SetFieldValue(ref _illustrationID, value); }
		}
		private Int32 _illustrationID;

		[DataField("ModifiedDate", DbType.DateTime, false)]
		public DateTime ModifiedDate
		{
			get { return _modifiedDate; }
			set { SetFieldValue(ref _modifiedDate, value); }
		}
		private DateTime _modifiedDate;

		#endregion

		#region Constructors

		public Production_ProductModelIllustration(bool addingNew) : base(addingNew)
		{
			if (addingNew)
			{
			}
		}

		[Obsolete("This default constructor is required by the DataClassAdapter, but should never be used directly in code.", true)]
		public Production_ProductModelIllustration() : this(false)
		{ }

		#endregion

	}

}
