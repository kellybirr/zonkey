using System.Collections.Generic;
using System.Data;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Zonkey.Mocks;
using Zonkey.UnitTests.AdventureWorks.DataObjects;

namespace Zonkey.UnitTests
{
    [TestClass, Ignore]
    public class MockTests
    {
#if (false)
        [TestMethod]
        public void MockTest1()
        {
            var mc = new MockDbConnection
            {
                SetupCommandFunc = delegate(MockDbCommand cmdToSetup)
                {
                    cmdToSetup.DoExecuteReader = (cmdExec => new[]
                    {
                        new { RightId = 15, Edit = false, Share = false, Delete = true },
                        new { RightId = 17, Edit = true, Share = true, Delete = true }
                    });
                }
            };

            var da = new DataClassAdapter<Right>(mc);
            Right result = da.GetOne(r => r.RightId == 15).Result;

            Assert.IsNotNull(result);
            Assert.AreEqual(15, result.RightId);
            Assert.IsFalse(result.Share);
        }

        [TestMethod]
        public void MockTest2()
        {
            var mc = new MockDbConnection
            {
                SetupCommandFunc = delegate(MockDbCommand cmdToSetup)
                {
                    cmdToSetup.DoExecuteReader = (cmdExec => new[]
                    {
                        new Dictionary<string, object> {{"RightId",15},{"Edit",true},{"Share",false},{"Delete",true}},
                        new Dictionary<string, object> {{"RightId",18},{"Edit",false},{"Share",true},{"Delete",true}},
                        new Dictionary<string, object> {{"RightId",19},{"Edit",false},{"Share",false},{"Delete",false}},
                        new Dictionary<string, object> {{"RightId",20},{"Edit",false},{"Share",false},{"Delete",true}}
                    });
                }
            };

            var da = new DataClassAdapter<Right>(mc);
            Right result = da.GetOne(r => r.RightId == 15).Result;

            Assert.IsNotNull(result);
            Assert.AreEqual(15, result.RightId);
            Assert.IsFalse(result.Share);
        }

        [TestMethod]
        public void MockTest3()
        {
            var mc = new MockDbConnection
            {
                SetupCommandFunc = delegate(MockDbCommand cmdToSetup)
                {
                    cmdToSetup.DoExecuteReader = (cmd => new[]
                    {
                        new { RightId = 15, Edit = false, Share = false, Delete = true },
                        new { RightId = 17, Edit = true, Share = true, Delete = true }
                    });
                }
            };

            var da = new DataClassAdapter<Right>(mc) {SqlDialect = new Dialects.SqlServerDialect()};
            Right result = da.GetOne(r => r.RightId == 15).Result;

            Assert.IsNotNull(result);
            Assert.AreEqual(15, result.RightId);
            Assert.IsFalse(result.Share);

            mc.SetupCommandFunc = delegate(MockDbCommand cmdToSetup)
            {
                cmdToSetup.DoExecuteNonQuery = delegate(MockDbCommand cmdExec)
                {
                    Assert.IsTrue(cmdExec.CommandText.Contains("UPDATE [Rights] SET [Share] = @Share WHERE [RightId] = @old_RightId AND [Share] = @old_Share"));
                    return 1;
                };

                cmdToSetup.DoExecuteReader = (cmdExec => new[] { new { Edit = false, Delete = true } } );
            };

            result.Share = true;
            Assert.IsTrue( da.Save(result).Result );            
        }
#endif

        [TestMethod]
        public void MockTest4()
        {
            var mc = new MockDbConnection
            {
                SetupCommandFunc = delegate(MockDbCommand cmdToSetup)
                {
                    cmdToSetup.DoExecuteReader = delegate
                    {
                        var ds = new DataSet();
                        ds.ReadXml(".\\SampleData\\Persons.xml");

                        return ds.Tables[0];
                    };
                }
            };

            var da = new DataClassAdapter<Person_Person>(mc);
            var col = new List<Person_Person>();

            int r = da.FillAll(col).Result;

            Assert.AreEqual(r, 20);
            Assert.AreEqual(r, col.Count);            
        }

    }
}
