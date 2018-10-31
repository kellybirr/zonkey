using System;
using System.Data;
using System.Data.SqlClient;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Zonkey.Dialects;
using Zonkey.ObjectModel;
using Zonkey.ObjectModel.Projection;

namespace Zonkey.UnitTests.Projection
{
    [TestClass]
    public class ProjectionParserTests
    {
        private const string TestTable = "TestTable";
        private const string TestIdColumn = "TestIdColumn";
        private const string TestStringColumn = "TestStringColumn";
        private const string TestDateTimeColumn = "TestDateTimeColumn";

        [TestMethod]
        public void GivenNullDialect_WhenCreatingProjectionParser_ThrowsException()
        {
            Assert.ThrowsException<ArgumentNullException>(() => new ProjectionParser(null));
        }

        [TestMethod]
        public void GivenNullProjectionMap_WhenParsing_ThrowsException()
        {
            using (var connection = new SqlConnection())
            {
                var parser = new ProjectionParser(SqlDialect.Create(connection));
                Assert.ThrowsException<ArgumentNullException>(() => parser.GetQueryString(null));
            }
        }

        [TestMethod]
        public void GivenProjectionMap_WhenParsing_BuildsSqlString()
        {
            var expectedSqlString = "[TestIdColumn], [TestStringColumn], [TestDateTimeColumn]";

            var dataMap = DataMap.GenerateCached(typeof(TestDC), null, null, 0);
            var projectionMap = new ProjectionMapBuilder().FromDataMap(dataMap);

            using (var connection = new SqlConnection())
            {
                var parser = new ProjectionParser(SqlDialect.Create(connection));
                var actualSqlString = parser.GetQueryString(projectionMap);

                Assert.AreEqual(expectedSqlString, actualSqlString);
            }
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
