using System;
using System.Data;
using Zonkey.ObjectModel;

namespace Zonkey.UnitTests.AdventureWorks.DataObjects
{
	[DataItem("ProductPhoto", SchemaName = "Production")]
	public class Production_ProductPhoto : DataClass
	{
		#region Data Columns (Properties)

		[DataField("ProductPhotoID", DbType.Int32, false, IsKeyField = true, IsAutoIncrement = true)]
		public Int32 ProductPhotoID
		{
			get { return _productPhotoID; }
			set { SetFieldValue(ref _productPhotoID, value); }
		}
		private Int32 _productPhotoID;

		[DataField("ThumbNailPhoto", DbType.Binary, true, Length = 2147483647)]
		public byte[] ThumbNailPhoto
		{
			get { return _thumbNailPhoto; }
			set { SetFieldValue(ref _thumbNailPhoto, value); }
		}
		private byte[] _thumbNailPhoto;

		[DataField("ThumbnailPhotoFileName", DbType.String, true, Length = 50)]
		public string ThumbnailPhotoFileName
		{
			get { return _thumbnailPhotoFileName; }
			set { SetFieldValue(ref _thumbnailPhotoFileName, value); }
		}
		private string _thumbnailPhotoFileName;

		[DataField("LargePhoto", DbType.Binary, true, Length = 2147483647)]
		public byte[] LargePhoto
		{
			get { return _largePhoto; }
			set { SetFieldValue(ref _largePhoto, value); }
		}
		private byte[] _largePhoto;

		[DataField("LargePhotoFileName", DbType.String, true, Length = 50)]
		public string LargePhotoFileName
		{
			get { return _largePhotoFileName; }
			set { SetFieldValue(ref _largePhotoFileName, value); }
		}
		private string _largePhotoFileName;

		[DataField("ModifiedDate", DbType.DateTime, false)]
		public DateTime ModifiedDate
		{
			get { return _modifiedDate; }
			set { SetFieldValue(ref _modifiedDate, value); }
		}
		private DateTime _modifiedDate;

		#endregion

		#region Constructors

		public Production_ProductPhoto(bool addingNew) : base(addingNew)
		{
			if (addingNew)
			{
			}
		}

		[Obsolete("This default constructor is required by the DataClassAdapter, but should never be used directly in code.", true)]
		public Production_ProductPhoto() : this(false)
		{ }

		#endregion

	}

}
