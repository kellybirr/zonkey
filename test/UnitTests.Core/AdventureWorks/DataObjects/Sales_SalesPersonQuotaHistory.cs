using System;
using System.Data;
using Zonkey.ObjectModel;

namespace Zonkey.UnitTests.AdventureWorks.DataObjects
{
	[DataItem("SalesPersonQuotaHistory", SchemaName = "Sales")]
	public class Sales_SalesPersonQuotaHistory : DataClass
	{
		#region Data Columns (Properties)

		[DataField("BusinessEntityID", DbType.Int32, false, IsKeyField = true)]
		public Int32 BusinessEntityID
		{
			get { return _businessEntityID; }
			set { SetFieldValue(ref _businessEntityID, value); }
		}
		private Int32 _businessEntityID;

		[DataField("QuotaDate", DbType.DateTime, false, IsKeyField = true)]
		public DateTime QuotaDate
		{
			get { return _quotaDate; }
			set { SetFieldValue(ref _quotaDate, value); }
		}
		private DateTime _quotaDate;

		[DataField("SalesQuota", DbType.Decimal, false)]
		public decimal SalesQuota
		{
			get { return _salesQuota; }
			set { SetFieldValue(ref _salesQuota, value); }
		}
		private decimal _salesQuota;

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

		public Sales_SalesPersonQuotaHistory(bool addingNew) : base(addingNew)
		{
			if (addingNew)
			{
			}
		}

		[Obsolete("This default constructor is required by the DataClassAdapter, but should never be used directly in code.", true)]
		public Sales_SalesPersonQuotaHistory() : this(false)
		{ }

		#endregion

	}

}
