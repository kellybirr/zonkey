using System.Data;
using Zonkey.ObjectModel;

namespace Zonkey.Tests.Models
{
    /// <summary>
    /// Table and column names are deliberately awkward ("Order Log" has a space,
    /// "Order" is a reserved word) so they REQUIRE identifier quoting on every
    /// dialect. Used by the UseQuotedIdentifier tests.
    /// </summary>
    [DataItem("Order Log")]
    public class OrderLog : DataClass
    {
        private int _id;
        private int _order;
        private string _note;

        public OrderLog() : base(true) { }
        public OrderLog(bool addingNew) : base(addingNew) { }

        [DataField("Id", DbType.Int32, IsKeyField = true, IsAutoIncrement = true)]
        public int Id
        {
            get => _id;
            set => SetFieldValue(ref _id, value);
        }

        [DataField("Order", DbType.Int32)]
        public int Order
        {
            get => _order;
            set => SetFieldValue(ref _order, value);
        }

        [DataField("Note", DbType.String, true)]
        public string Note
        {
            get => _note;
            set => SetFieldValue(ref _note, value);
        }
    }
}
