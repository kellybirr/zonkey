using System;
using System.Data;
using System.Linq.Expressions;
using Zonkey.Extensions;
using Zonkey.ObjectModel;
using Zonkey.UnitTests.AdventureWorks.DataObjects;

namespace Zonkey.UnitTests.Shared.AdventureWorks.DataJoin
{
    [DataJoin(typeof(OrderLineJoin))]
    public class OrderLineItem : DataClass<int>
    {
        [DataField("SalesOrderDetailID", DbType.Int32, false, IsKeyField = true, IsAutoIncrement = true)]
        public Int32 SalesOrderDetailID
        {
            get => _salesOrderDetailID;
            set => SetFieldValue(ref _salesOrderDetailID, value);
        }
        private Int32 _salesOrderDetailID;

        [DataField("SalesOrderID", DbType.Int32, false, SourceType = typeof(Sales_SalesOrderDetail))]
        public Int32 SalesOrderID
        {
            get => _salesOrderID;
            set => SetFieldValue(ref _salesOrderID, value);
        }
        private Int32 _salesOrderID;

        [DataField("CarrierTrackingNumber", DbType.String, true, Length = 25, SourceType = typeof(Sales_SalesOrderDetail))]
        public string CarrierTrackingNumber
        {
            get => _carrierTrackingNumber;
            set => SetFieldValue(ref _carrierTrackingNumber, value);
        }
        private string _carrierTrackingNumber;

        [DataField("OrderQty", DbType.Int16, false, SourceType = typeof(Sales_SalesOrderDetail))]
        public Int16 OrderQty
        {
            get => _orderQty;
            set => SetFieldValue(ref _orderQty, value);
        }
        private Int16 _orderQty;

        [DataField("ProductID", DbType.Int32, false, SourceType = typeof(Production_Product))]
        public Int32 ProductID
        {
            get => _productID;
            set => SetFieldValue(ref _productID, value);
        }
        private Int32 _productID;

        [DataField("SpecialOfferID", DbType.Int32, false, SourceType = typeof(Sales_SalesOrderDetail))]
        public Int32 SpecialOfferID
        {
            get => _specialOfferID;
            set => SetFieldValue(ref _specialOfferID, value);
        }
        private Int32 _specialOfferID;

        [DataField("UnitPrice", DbType.Decimal, false, SourceType = typeof(Sales_SalesOrderDetail))]
        public decimal UnitPrice
        {
            get => _unitPrice;
            set => SetFieldValue(ref _unitPrice, value);
        }
        private decimal _unitPrice;

        [DataField("UnitPriceDiscount", DbType.Decimal, false, SourceType = typeof(Sales_SalesOrderDetail))]
        public decimal UnitPriceDiscount
        {
            get => _unitPriceDiscount;
            set => SetFieldValue(ref _unitPriceDiscount, value);
        }
        private decimal _unitPriceDiscount;

        [DataField("LineTotal", DbType.Decimal, false, SourceType = typeof(Sales_SalesOrderDetail))]
        public decimal LineTotal
        {
            get => _lineTotal;
            set => SetFieldValue(ref _lineTotal, value);
        }
        private decimal _lineTotal;

        [DataField("Name", DbType.String, false, Length = 50, SourceType = typeof(Production_Product))]
        public string ProductName
        {
            get => _productName;
            set => SetFieldValue(ref _productName, value);
        }
        private string _productName;

        [DataField("ProductNumber", DbType.String, false, Length = 25, SourceType = typeof(Production_Product))]
        public string ProductNumber
        {
            get => _productNumber;
            set => SetFieldValue(ref _productNumber, value);
        }
        private string _productNumber;


        #region Constructors

        public OrderLineItem(bool addingNew) : base(addingNew)
        {
            if (addingNew)
            {
            }
        }

        [Obsolete("This default constructor is required by the DataClassAdapter, but should never be used directly in code.", true)]
        public OrderLineItem() : this(false)
        { }

        #endregion

        class OrderLineJoin : JoinDefinition<
            Sales_SalesOrderDetail, 
            Production_Product
        > {
            public override Expression<Func<Sales_SalesOrderDetail, Production_Product, bool>> JoinFunc 
                => (d, p) => d.ProductID == p.ProductID;
        }
    }
}
