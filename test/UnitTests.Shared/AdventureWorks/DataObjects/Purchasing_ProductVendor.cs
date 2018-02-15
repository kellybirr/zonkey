using System;
using System.Data;
using Zonkey.ObjectModel;

namespace Zonkey.UnitTests.AdventureWorks.DataObjects
{
    [DataItem("ProductVendor", SchemaName = "Purchasing")]
    public class Purchasing_ProductVendor : DataClass<(int ProductID, int BusinessEntityID)>
    {
        #region Data Columns (Properties)

        [DataField("ProductID", DbType.Int32, false, IsKeyField = true)]
        public Int32 ProductID
        {
            get { return _productID; }
            set { SetFieldValue(ref _productID, value); }
        }
        private Int32 _productID;

        [DataField("BusinessEntityID", DbType.Int32, false, IsKeyField = true)]
        public Int32 BusinessEntityID
        {
            get { return _businessEntityID; }
            set { SetFieldValue(ref _businessEntityID, value); }
        }
        private Int32 _businessEntityID;

        [DataField("AverageLeadTime", DbType.Int32, false)]
        public Int32 AverageLeadTime
        {
            get { return _averageLeadTime; }
            set { SetFieldValue(ref _averageLeadTime, value); }
        }
        private Int32 _averageLeadTime;

        [DataField("StandardPrice", DbType.Decimal, false)]
        public decimal StandardPrice
        {
            get { return _standardPrice; }
            set { SetFieldValue(ref _standardPrice, value); }
        }
        private decimal _standardPrice;

        [DataField("LastReceiptCost", DbType.Decimal, true)]
        public decimal LastReceiptCost
        {
            get { return _lastReceiptCost; }
            set { SetFieldValue(ref _lastReceiptCost, value); }
        }
        private decimal _lastReceiptCost;

        [DataField("LastReceiptDate", DbType.DateTime, true)]
        public DateTime? LastReceiptDate
        {
            get { return _lastReceiptDate; }
            set { SetFieldValue(ref _lastReceiptDate, value); }
        }
        private DateTime? _lastReceiptDate;

        [DataField("MinOrderQty", DbType.Int32, false)]
        public Int32 MinOrderQty
        {
            get { return _minOrderQty; }
            set { SetFieldValue(ref _minOrderQty, value); }
        }
        private Int32 _minOrderQty;

        [DataField("MaxOrderQty", DbType.Int32, false)]
        public Int32 MaxOrderQty
        {
            get { return _maxOrderQty; }
            set { SetFieldValue(ref _maxOrderQty, value); }
        }
        private Int32 _maxOrderQty;

        [DataField("OnOrderQty", DbType.Int32, true)]
        public Int32? OnOrderQty
        {
            get { return _onOrderQty; }
            set { SetFieldValue(ref _onOrderQty, value); }
        }
        private Int32? _onOrderQty;

        [DataField("UnitMeasureCode", DbType.StringFixedLength, false, Length = 3)]
        public string UnitMeasureCode
        {
            get { return _unitMeasureCode; }
            set { SetFieldValue(ref _unitMeasureCode, value); }
        }
        private string _unitMeasureCode;

        [DataField("ModifiedDate", DbType.DateTime, false)]
        public DateTime ModifiedDate
        {
            get { return _modifiedDate; }
            set { SetFieldValue(ref _modifiedDate, value); }
        }
        private DateTime _modifiedDate;

        #endregion

        #region Constructors

        public Purchasing_ProductVendor(bool addingNew) : base(addingNew)
        {
            if (addingNew)
            {
            }
        }

        [Obsolete("This default constructor is required by the DataClassAdapter, but should never be used directly in code.", true)]
        public Purchasing_ProductVendor() : this(false)
        { }

        #endregion

        public override (int ProductID, int BusinessEntityID) GetKey() => (ProductID, BusinessEntityID);
    }

}
