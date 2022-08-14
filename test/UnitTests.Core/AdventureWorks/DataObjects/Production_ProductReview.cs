using System;
using System.Data;
using Zonkey.ObjectModel;

namespace Zonkey.UnitTests.AdventureWorks.DataObjects
{
	[DataItem("ProductReview", SchemaName = "Production")]
	public class Production_ProductReview : DataClass
	{
		#region Data Columns (Properties)

		[DataField("ProductReviewID", DbType.Int32, false, IsKeyField = true, IsAutoIncrement = true)]
		public Int32 ProductReviewID
		{
			get { return _productReviewID; }
			set { SetFieldValue(ref _productReviewID, value); }
		}
		private Int32 _productReviewID;

		[DataField("ProductID", DbType.Int32, false)]
		public Int32 ProductID
		{
			get { return _productID; }
			set { SetFieldValue(ref _productID, value); }
		}
		private Int32 _productID;

		[DataField("ReviewerName", DbType.String, false, Length = 50)]
		public string ReviewerName
		{
			get { return _reviewerName; }
			set { SetFieldValue(ref _reviewerName, value); }
		}
		private string _reviewerName;

		[DataField("ReviewDate", DbType.DateTime, false)]
		public DateTime ReviewDate
		{
			get { return _reviewDate; }
			set { SetFieldValue(ref _reviewDate, value); }
		}
		private DateTime _reviewDate;

		[DataField("EmailAddress", DbType.String, false, Length = 50)]
		public string EmailAddress
		{
			get { return _emailAddress; }
			set { SetFieldValue(ref _emailAddress, value); }
		}
		private string _emailAddress;

		[DataField("Rating", DbType.Int32, false)]
		public Int32 Rating
		{
			get { return _rating; }
			set { SetFieldValue(ref _rating, value); }
		}
		private Int32 _rating;

		[DataField("Comments", DbType.String, true, Length = 3850)]
		public string Comments
		{
			get { return _comments; }
			set { SetFieldValue(ref _comments, value); }
		}
		private string _comments;

		[DataField("ModifiedDate", DbType.DateTime, false)]
		public DateTime ModifiedDate
		{
			get { return _modifiedDate; }
			set { SetFieldValue(ref _modifiedDate, value); }
		}
		private DateTime _modifiedDate;

		#endregion

		#region Constructors

		public Production_ProductReview(bool addingNew) : base(addingNew)
		{
			if (addingNew)
			{
			}
		}

		[Obsolete("This default constructor is required by the DataClassAdapter, but should never be used directly in code.", true)]
		public Production_ProductReview() : this(false)
		{ }

		#endregion

	}

}
