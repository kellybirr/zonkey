using System;
using System.Data;
using Zonkey.ObjectModel;

namespace Zonkey.UnitTests.AdventureWorks.DataObjects
{
	[DataItem("WorkOrderRouting", SchemaName = "Production")]
	public class Production_WorkOrderRouting : DataClass
	{
		#region Data Columns (Properties)

		[DataField("WorkOrderID", DbType.Int32, false, IsKeyField = true)]
		public Int32 WorkOrderID
		{
			get { return _workOrderID; }
			set { SetFieldValue(ref _workOrderID, value); }
		}
		private Int32 _workOrderID;

		[DataField("ProductID", DbType.Int32, false, IsKeyField = true)]
		public Int32 ProductID
		{
			get { return _productID; }
			set { SetFieldValue(ref _productID, value); }
		}
		private Int32 _productID;

		[DataField("OperationSequence", DbType.Int16, false, IsKeyField = true)]
		public Int16 OperationSequence
		{
			get { return _operationSequence; }
			set { SetFieldValue(ref _operationSequence, value); }
		}
		private Int16 _operationSequence;

		[DataField("LocationID", DbType.Int16, false)]
		public Int16 LocationID
		{
			get { return _locationID; }
			set { SetFieldValue(ref _locationID, value); }
		}
		private Int16 _locationID;

		[DataField("ScheduledStartDate", DbType.DateTime, false)]
		public DateTime ScheduledStartDate
		{
			get { return _scheduledStartDate; }
			set { SetFieldValue(ref _scheduledStartDate, value); }
		}
		private DateTime _scheduledStartDate;

		[DataField("ScheduledEndDate", DbType.DateTime, false)]
		public DateTime ScheduledEndDate
		{
			get { return _scheduledEndDate; }
			set { SetFieldValue(ref _scheduledEndDate, value); }
		}
		private DateTime _scheduledEndDate;

		[DataField("ActualStartDate", DbType.DateTime, true)]
		public DateTime? ActualStartDate
		{
			get { return _actualStartDate; }
			set { SetFieldValue(ref _actualStartDate, value); }
		}
		private DateTime? _actualStartDate;

		[DataField("ActualEndDate", DbType.DateTime, true)]
		public DateTime? ActualEndDate
		{
			get { return _actualEndDate; }
			set { SetFieldValue(ref _actualEndDate, value); }
		}
		private DateTime? _actualEndDate;

		[DataField("ActualResourceHrs", DbType.Decimal, true)]
		public decimal ActualResourceHrs
		{
			get { return _actualResourceHrs; }
			set { SetFieldValue(ref _actualResourceHrs, value); }
		}
		private decimal _actualResourceHrs;

		[DataField("PlannedCost", DbType.Decimal, false)]
		public decimal PlannedCost
		{
			get { return _plannedCost; }
			set { SetFieldValue(ref _plannedCost, value); }
		}
		private decimal _plannedCost;

		[DataField("ActualCost", DbType.Decimal, true)]
		public decimal ActualCost
		{
			get { return _actualCost; }
			set { SetFieldValue(ref _actualCost, value); }
		}
		private decimal _actualCost;

		[DataField("ModifiedDate", DbType.DateTime, false)]
		public DateTime ModifiedDate
		{
			get { return _modifiedDate; }
			set { SetFieldValue(ref _modifiedDate, value); }
		}
		private DateTime _modifiedDate;

		#endregion

		#region Constructors

		public Production_WorkOrderRouting(bool addingNew) : base(addingNew)
		{
			if (addingNew)
			{
			}
		}

		[Obsolete("This default constructor is required by the DataClassAdapter, but should never be used directly in code.", true)]
		public Production_WorkOrderRouting() : this(false)
		{ }

		#endregion

	}

}
