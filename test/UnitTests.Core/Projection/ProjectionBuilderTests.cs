using System;
using System.Data;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Zonkey.ObjectModel;
using Zonkey.ObjectModel.Projection;

namespace Zonkey.UnitTests.Projection
{
    [TestClass]
    public class ProjectionBuilderTests
    {
        private const string TestTable = "TestTable";
        private const string TestIdColumn = "TestIdColumn";
        private const string TestStringColumn = "TestStringColumn";
        private const string TestDateTimeColumn = "TestDateTimeColumn";

        [TestMethod]
        public void GivenNullDataMap_WhenBuildingProjectionMap_ThrowsException()
        {
            var sut = new ProjectionMapBuilder();
            Assert.ThrowsException<ArgumentNullException>(() => sut.FromDataMap(null));
        }

        [TestMethod]
        public void GivenDataMap_WhenBuildingProjectionMap_KeepsFields()
        {
            var dataMap = DataMap.GenerateCached(typeof(TestDC), null, null, 0);

            var sut = new ProjectionMapBuilder();
            var projectionMap = sut.FromDataMap(dataMap);

            var idProperty = typeof(TestDC).GetProperty(nameof(TestDC.TestIdProperty));
            var stringProperty = typeof(TestDC).GetProperty(nameof(TestDC.TestStringProperty));
            var dateTimeProperty = typeof(TestDC).GetProperty(nameof(TestDC.TestDateTimeProperty));

            Assert.AreEqual(TestIdColumn, projectionMap.Map[idProperty].ExpressionField);
            Assert.AreEqual(TestStringColumn, projectionMap.Map[stringProperty].ExpressionField);
            Assert.AreEqual(TestDateTimeColumn, projectionMap.Map[dateTimeProperty].ExpressionField);
        }

        [DataItem(TestTable)]
        private class TestDC : DataClass<int>
        {
            public override int GetKey() => _testId;

            [DataField(TestIdColumn, DbType.Int32, false, IsKeyField = true)]
            public int TestIdProperty
            {
                get => _testId;
                set => SetFieldValue(ref _testId, value);
            }
            private int _testId;

            [DataField(TestStringColumn, DbType.String, true, Length = 100)]
            public string TestStringProperty
            {
                get => _testStringColumn;
                set => SetFieldValue(ref _testStringColumn, value);
            }
            private string _testStringColumn;

            [DataField(TestDateTimeColumn, DbType.String, false, Length = 500)]
            public string TestDateTimeProperty
            {
                get => _testDateTimeColumn;
                set => SetFieldValue(ref _testDateTimeColumn, value);
            }
            private string _testDateTimeColumn;

            public TestDC(bool addingNew) : base(addingNew)
            { }

            public TestDC() : this(false)
            { }
        }
    }
}
