using System;
using System.Data;
using Xunit;
using Zonkey.Mocks;
using Zonkey.ObjectModel;

namespace Zonkey.Tests.Unit
{
    /// <summary>
    /// PostgreSQL array columns report their static field type as System.Array while the
    /// runtime values are concrete arrays (string[], int[]). Typed array properties must
    /// materialize on both the fast and reflection paths via a runtime downcast. The
    /// mock reader reproduces Npgsql's shape exactly: column type Array, values concrete.
    /// </summary>
    public class ArrayFillTests
    {
        [DataItem("ArrayRows")]
        public class ArrayRow : DataClass
        {
            private int _id;
            private string[] _tags;
            private int[] _nums;
            private Array _raw;

            public ArrayRow() : base(false) { }
            public ArrayRow(bool addingNew) : base(addingNew) { }

            [DataField("Id", DbType.Int32, IsKeyField = true)]
            public int Id { get => _id; set => SetFieldValue(ref _id, value); }

            [DataField("Tags", DbType.Object, true)]
            public string[] Tags { get => _tags; set => SetFieldValue(ref _tags, value); }

            [DataField("Nums", DbType.Object, true)]
            public int[] Nums { get => _nums; set => SetFieldValue(ref _nums, value); }

            [DataField("Raw", DbType.Object, true)]
            public Array Raw { get => _raw; set => SetFieldValue(ref _raw, value); }

            private System.Collections.Generic.IEnumerable<string> _tagsView;
            [DataField("TagsView", DbType.Object, true)]
            public System.Collections.Generic.IEnumerable<string> TagsView
            {
                get => _tagsView;
                set => SetFieldValue(ref _tagsView, value);
            } // interface-typed property: runtime value (string[]) is assignable, not identical
        }

        private static ArrayRow ReadOne(bool fast, bool withNulls)
        {
            var table = new DataTable("ArrayRows");
            table.Columns.Add("Id", typeof(int));
            table.Columns.Add("Tags", typeof(Array));
            table.Columns.Add("Nums", typeof(Array));
            table.Columns.Add("Raw", typeof(Array));
            table.Columns.Add("TagsView", typeof(Array));

            if (withNulls)
                table.Rows.Add(2, DBNull.Value, DBNull.Value, DBNull.Value, DBNull.Value);
            else
                table.Rows.Add(1, new[] { "alpha", "beta" }, new[] { 3, 5, 8 }, new[] { "raw1" }, new[] { "v1", "v2" });

            var conn = new MockDbConnection();
            conn.Open();
            conn.SetupCommandFunc = cmd => cmd.DoExecuteReader = _ => table;

            using var command = conn.CreateCommand();
            using var reader = new DataClassReader<ArrayRow>(command.ExecuteReader()) { UseFastBuilder = fast };
            return reader.Read();
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void TypedArrayProperties_MaterializeFromArrayColumns(bool fast)
        {
            var row = ReadOne(fast, withNulls: false);

            Assert.Equal(1, row.Id);
            Assert.Equal(new[] { "alpha", "beta" }, row.Tags);
            Assert.Equal(new[] { 3, 5, 8 }, row.Nums);
            Assert.Equal(new[] { "raw1" }, (string[])row.Raw); // Array-typed property keeps working
            Assert.Equal(new[] { "v1", "v2" }, row.TagsView);  // interface-typed property: runtime downcast
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void NullArrayColumns_LeavePropertiesNull(bool fast)
        {
            var row = ReadOne(fast, withNulls: true);

            Assert.Null(row.Tags);
            Assert.Null(row.Nums);
            Assert.Null(row.Raw);
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void WrongElementType_ThrowsPropertyReadException(bool fast)
        {
            var table = new DataTable("ArrayRows");
            table.Columns.Add("Id", typeof(int));
            table.Columns.Add("Nums", typeof(Array));
            table.Rows.Add(1, new[] { "not", "ints" }); // string[] into int[] property

            var conn = new MockDbConnection();
            conn.Open();
            conn.SetupCommandFunc = cmd => cmd.DoExecuteReader = _ => table;

            using var command = conn.CreateCommand();
            using var reader = new DataClassReader<ArrayRow>(command.ExecuteReader()) { UseFastBuilder = fast };

            var ex = Assert.Throws<PropertyReadException>(() => reader.Read());
            Assert.Equal(nameof(ArrayRow.Nums), ex.Property.Name);
        }
    }
}
