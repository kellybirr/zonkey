using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Diagnostics;
using System.Globalization;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Zonkey.ObjectModel;
using Zonkey.UnitTests.AdventureWorks;
using Zonkey.UnitTests.AdventureWorks.DataObjects;
using Zonkey.UnitTests.Shared.AdventureWorks.DataJoin;

namespace Zonkey.UnitTests
{
    [TestClass]
    public class DataClassAdapterTest
    {
        [ClassInitialize]
        public static void Initialize(TestContext context)
        {
            DbConnectionFactory.Register<SqlConnection>(AdventureDb.Name, AdventureDb.ConnectionString);
        }

        [TestMethod]
        public async Task Join_Test_1()
        {
            using (var db = await AdventureDb.Open())
            {
                var adapter = db.Adapter<OrderLineItem>();
                adapter.BeforeExecuteCommand += (sender, args) => Console.WriteLine(args.Command.CommandText);

                var lineItem = await adapter.GetOne(d => d.SalesOrderID == 43659);
                Assert.IsNotNull(lineItem);

                Assert.AreEqual(lineItem.SalesOrderDetailID, lineItem.GetKey());
            }
        }

#if (false)
        [TestMethod]
        public void FillRange_Test_String()
        {
            var cxn = DbConnectionFactory.OpenConnection("AAFX_Test").Result;
            var da = new DataClassAdapter<ContactDC>(cxn) { NoLock = true };
            var col = new ContactDCCollection();

            da.OrderBy = "FirstName";
            var r = da.FillRange(col, 100, 200, "WorkgroupID = $0 AND Addr1City = $1", 47037373, "Spokane").Result;

            Assert.AreEqual(200, r);
            Assert.AreEqual(200, col.Count);
        }

        [TestMethod]
        public void FillRange_Test_Lambda()
        {
            var cxn = DbConnectionFactory.OpenConnection("AAFX_Test").Result;
            var da = new DataClassAdapter<ContactDC>(cxn) { NoLock = true };
            da.BeforeExecuteCommand += delegate(object sender, CommandExecuteEventArgs args)
                                        {
                                            Debug.WriteLine(args.Command.CommandText);
                                        };

            var col = new ContactDCCollection();

            da.OrderBy = "FirstName";
            var r = da.FillRange(col, 100, 2000, c => c.WorkgroupID == 47037373 && c.Addr1City == "Spokane").Result;

            Assert.AreEqual(2000, r);
            Assert.AreEqual(2000, col.Count);
        }

        [TestMethod]
        public void Test_Ctor_Speed()
        {
            ContactDC dc;

            long start = Environment.TickCount;
            for (int i = 0; i < 10000000; i++)
                dc = new ContactDC(false);
            Console.WriteLine("'new' keyword = {0}ms", Environment.TickCount-start);

            start = Environment.TickCount;
            for (int i = 0; i < 10000000; i++)
                dc = Activator.CreateInstance<ContactDC>();
            Console.WriteLine("Activator = {0}ms", Environment.TickCount - start);

            Func<ContactDC> lambda = ClassFactory.CompileDefaultFactory<ContactDC>();
            start = Environment.TickCount;
            for (int i = 0; i < 10000000; i++)
                dc = lambda();
            Console.WriteLine("Complied Lambda = {0}ms", Environment.TickCount - start);

            Func<ContactDC> dynamicFunc = ClassFactory.EmitDefaultFactory<ContactDC>();
            start = Environment.TickCount;
            for (int i = 0; i < 10000000; i++)
                dc = dynamicFunc();
            Console.WriteLine("Dynamic Method = {0}ms", Environment.TickCount - start);
        }

        [TestMethod]
        public void FillRange_Test_Filter()
        {
            var cxn = DbConnectionFactory.OpenConnection("AAFX_Test").Result;
            var da = new DataClassAdapter<ContactDC>(cxn);
            var col = new ContactDCCollection();

            da.OrderBy = "FirstName";
            var r = da.FillRange(col, 100, 200, SqlFilter.EQ("LastName", "Smith")).Result;

            Assert.AreEqual(200, r);
            Assert.AreEqual(200, col.Count);
        }

        [TestMethod]
        public void GetCount_Test_String()
        {
            var cxn = DbConnectionFactory.OpenConnection("AAFX_Test").Result;
            var da = new DataClassAdapter<ContactDC>(cxn) { NoLock = true };

            var r = da.GetCount("WorkgroupID = $0 AND Addr1City = $1", 47037373, "Spokane").Result;

            Assert.IsTrue(r > 0);
        }

        [TestMethod]
        public void GetCount_Test_Lambda()
        {
            var cxn = DbConnectionFactory.OpenConnection("AAFX_Test").Result;
            var da = new DataClassAdapter<ContactDC>(cxn);

            var r = da.GetCount(c => c.WorkgroupID == 47037373 && c.Addr1City == "Spokane").Result;

            Assert.IsTrue(r > 0);
        }

        [TestMethod]
        public void GetSingleItems_Test_Lambda()
        {
            var cxn = DbConnectionFactory.OpenConnection("AAFX_Test").Result;
            var da = new DataClassAdapter<ContactDC>(cxn) { NoLock = true };

            var r = da.GetOne(c => c.WorkgroupID == 47037373 && c.Addr1City == "Spokane").Result;

            Assert.IsInstanceOfType(r, typeof(ContactDC));
        }

        [TestMethod]
        public void GetCount_Test_Filter()
        {
            var cxn = DbConnectionFactory.OpenConnection("AAFX_Test").Result;
            var da = new DataClassAdapter<ContactDC>(cxn);

            var r = da.GetCount(SqlFilter.EQ("LastName", "Smith")).Result;

            Assert.IsTrue(r > 0);
        }

        [TestMethod]
        public void Fill_Test_Filter()
        {
            var cxn = DbConnectionFactory.OpenConnection("AAFX_Test").Result;
            var da = new DataClassAdapter<ContactDC>(cxn);
            var col = new ContactDCCollection();

            da.OrderBy = "FirstName";
            var r = da.Fill(col, SqlFilter.EQ("LastName", "Smith"), SqlFilter.LT("BirthDate", new DateTime(1957, 1, 1))).Result;

            Assert.IsTrue(r > 0);
            Assert.IsTrue(col.Count > 0);
        }

        [TestMethod, Ignore]
        public void FillWithSP_Test()
        {
            var cxn = DbConnectionFactory.OpenConnection("AAFX_Test").Result;
            var da = new DataClassAdapter<ContactDC>(cxn);
            var col = new List<ContactDC>();

            var r = da.FillWithSP(col, "sp_GetContactsByWorkgroup",
                                  new GenericParameter("workgroupId", DbType.Int32, 47037373)
                                  ).Result;

            Assert.IsTrue(r > 0);
            Assert.IsTrue(col.Count > 0);            
        }

        [TestMethod]
        public void ImplicitFill_Test()
        {
            var cxn = DbConnectionFactory.OpenConnection("AAFX_Test").Result;
            var da = new DataClassAdapter<ImplicitContact>(cxn, "Contacts");
            var col = new List<ImplicitContact>();

            da.OrderBy = "FirstName";
            var r = da.Fill(col, "LastName = $0 AND BirthDate < $1", "Smith", new DateTime(1980, 1, 1)).Result;

            Assert.IsTrue(r > 0);
            Assert.IsTrue(col.Count > 0);            
        }

        [TestMethod, Ignore]
        public void ImplicitFill_Test_WithSP()
        {
            var cxn = DbConnectionFactory.OpenConnection("AAFX_Test").Result;
            var da = new DataClassAdapter<ImplicitContact>(cxn);
            var col = new List<ImplicitContact>();

            var r = da.FillWithSP(col, "sp_GetContactsByWorkgroup",
                                  new GenericParameter("workgroupId", DbType.Int32, 47037373)
                                  ).Result;

            Assert.IsTrue(r > 0);
            Assert.IsTrue(col.Count > 0);
        }
        
        [TestMethod, Ignore]
        public void GenericParameter_Test()
        {
            var cxn = DbConnectionFactory.OpenConnection("Kelly_Test").Result;
            var dm = new DataManager(cxn);

            var inp1 = new GenericParameter("inp1", DbType.Int32, 456789);
            var outp1 = new GenericParameter("outp1", ParameterDirection.Output, DbType.Int32);
            var retVal = new GenericParameter("RETURN_VALUE", ParameterDirection.ReturnValue, DbType.Int32);

            var r = dm.ExecuteNonQuery("sp_outputtest", true, inp1, outp1, retVal).Result;

            Assert.AreEqual(inp1.Value, outp1.Value);
            Assert.AreEqual(inp1.Value, retVal.Value);
        }

        [TestMethod]
        public void TimeColumn_Test()
        {
            using (var cxn = DbConnectionFactory.OpenConnection("OPK_Test").Result)
            {
                DbTransactionRegistry.RegisterNewTransaction(cxn);

                var da = new DataClassAdapter<RuleDC>(cxn);

                var r = new RuleDC(true)
                            {
                                Amount = 10m,
                                DOW = "MT",
                                StartDate = new DateTime(2009, 1, 1),
                                EndDate = new DateTime(2009, 12, 31),
                                StartTime = new TimeSpan(7, 0, 0),
                                EndTime = new TimeSpan(10, 0, 0),
                                OrgID = 1,
                                LocationID = new Guid("84770519-FA41-4601-9AEB-4607071166EE"),
                                PaysFor = 300
                            };

                da.Save(r).Wait();
            }
        }

        [TestMethod]
        public void UpdateRows_Test()
        {
            var cxn = DbConnectionFactory.OpenConnection("AAFX_Test").Result;
            var da = new DataClassAdapter<ContactDC>(cxn);

            da.BeforeExecuteCommand += (sender, args) => Console.WriteLine(args.Command.CommandText);
            int rows = da.UpdateRows(new {ReferredBy = "test"}, dc => dc.MarketSource == "test").Result;            
        }

        [TestMethod]
        public void ChangeTracking_Test()
        {
            var cxn = new SqlConnection("Server=(local);Database=CT;Integrated Security=SSPI;");
            cxn.Open();

            var da = new DataClassAdapter<ShinyHappyPeople>(cxn)
            {
                ChangeTrackingContext = "Technical support can help you with questions related to your call to park account and payments by phone.  If your question is regarding an issue with the parking facility or on-site payment kiosk, please call the parking management company, whose name and phone number can be found on other signs posted at the location"
            };

            da.BeforeExecuteCommand += (sender, args) => Console.WriteLine(args.Command.CommandText);

            var kelly = da.GetOne(p => p.PKid == 1).Result;
            kelly.Phone = Environment.TickCount.ToString(CultureInfo.InvariantCulture);

            da.Save(kelly).Wait();
        }

        [TestMethod]
        public void NoLock_Test()
        {
            var cxn = DbConnectionFactory.OpenConnection("AAFX_Test").Result;
            
            var da = new DataClassAdapter<ContactDC>(cxn) {NoLock = true};

            var col = new ContactDCCollection();

            da.OrderBy = "FirstName";
            var r = da.Fill(col, SqlFilter.EQ("LastName", "Smith"), SqlFilter.LT("BirthDate", new DateTime(1957, 1, 1))).Result;

            Assert.IsTrue(r > 0);
            Assert.IsTrue(col.Count > 0);

            var t = da.Fill(col, c => c.LastName == "Birr").Result;

            Assert.IsTrue(r > 0);
            Assert.IsTrue(col.Count == (r + t));
        }

#endif
    }
}
