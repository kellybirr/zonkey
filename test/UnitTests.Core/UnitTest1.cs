using System;
using System.Data;
using System.Data.SqlClient;
using System.IO;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Zonkey.UnitTests.AdventureWorks;
using Zonkey.UnitTests.AdventureWorks.DataObjects;

namespace Zonkey.UnitTests
{
    [TestClass]
    public class UnitTest1
    {
        [ClassInitialize]
        public static void Initialize(TestContext context)
        {
            DbConnectionFactory.Register<SqlConnection>(AdventureDb.Name, AdventureDb.ConnectionString);
        }

        [TestMethod]
        public async Task Test_Connect()
        {
            using (var cnxn = await DbConnectionFactory.OpenConnection(AdventureDb.Name))
            {
                Console.WriteLine("Connected!");
            }
        }

        [TestMethod]
        public async Task Test_Query_1()
        {
            using (var db = await AdventureDb.Open())
            {
                Person_Person person = await db.GetOne<Person_Person>(p => p.BusinessEntityID == 1);
                Assert.IsNotNull(person);

                Assert.AreEqual(person.BusinessEntityID, person.GetKey());
            }
        }

        [TestMethod]
        public void Test_TupleKey_1()
        {
            (int ProductId, int BusinessEntityId) key = (1, 2);

            var thing = new Purchasing_ProductVendor(false)
            {
                BusinessEntityID = key.BusinessEntityId,
                ProductID = key.ProductId
            };
            Assert.AreEqual(key, thing.GetKey());


            var thing2 = new Purchasing_Vendor(false) { BusinessEntityID = 357 };
            Assert.AreEqual(thing2.BusinessEntityID, thing2.GetKey());


            var thing3 = new Person_Person(true) { BusinessEntityID = 44 };
            Assert.AreEqual(thing3.BusinessEntityID, thing3.GetKey());
        }

        [TestMethod]
        public async Task Test_Update_1()
        {
            using (var db = await AdventureDb.Open())
            {
                Person_Person person1 = await db.GetOne<Person_Person>(p => p.BusinessEntityID == 1);
                Assert.IsNotNull(person1);

                string name1 = person1.FirstName;
                person1.FirstName = Path.GetTempFileName();
                await db.Save(person1);

                Person_Person person2 = await db.GetOne<Person_Person>(p => p.BusinessEntityID == 1);
                Assert.AreEqual(person1.FirstName, person2.FirstName);

                person1.FirstName = name1;
                bool saveResult = await db.Save(person1);
                Assert.AreEqual(true, saveResult);
            }
        }

        [TestMethod]
        public void GenericParameter_Test()
        {
            const int k = 0;
            Assert.IsTrue((new GenericParameter("id", ParameterDirection.Input, DbType.Int32, 0)).Value != null);
            Assert.IsTrue((new GenericParameter("id", ParameterDirection.Input, DbType.Int32, k)).Value != null);
            Assert.IsTrue((new GenericParameter("id", ParameterDirection.Input, DbType.Int32, 0L)).Value != null);
            Assert.IsTrue((new GenericParameter("id", ParameterDirection.Input, DbType.Int32, 0m)).Value != null);
            Assert.IsTrue((new GenericParameter("id", ParameterDirection.Input, DbType.Int32, 0UL)).Value != null);
            Assert.IsTrue((new GenericParameter("id", ParameterDirection.Input, DbType.Int32, 0f)).Value != null);
            Assert.IsTrue((new GenericParameter("id", ParameterDirection.Input, DbType.Int32, 0d)).Value != null);
            Assert.IsTrue((new GenericParameter("id", ParameterDirection.Input, DbType.Int32, 0x0)).Value != null);
        }

        [TestMethod]
        public async Task Sql_Time_Test()
        {
            var myTime = new TimeSpan(1, 2, 3);
            using (var db = await AdventureDb.Open())
            {
                var dataMgr = new DataManager(db.Connection);

                object result = await dataMgr.ExecuteScalar(
                    "SELECT @myTime AS MyTime",
                    new GenericParameter("myTime", DbType.Time, myTime)
                );

                Assert.AreEqual(myTime, result);
            }
        }
    }
}
