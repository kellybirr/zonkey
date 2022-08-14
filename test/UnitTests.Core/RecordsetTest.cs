using System;
using System.Data;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Zonkey.Ado;
using Zonkey.UnitTests.AdventureWorks;

#if (NET48)
    using System.Data.SqlClient;
#else
    using Microsoft.Data.SqlClient;
#endif

namespace Zonkey.UnitTests
{
    [TestClass]
    public class RecordsetTest
    {
        [ClassInitialize]
        public static void Initialize(TestContext context)
        {
            DbConnectionFactory.Register<SqlConnection>(AdventureDb.Name, AdventureDb.ConnectionString);
        }

        [TestMethod]
        public async Task OpenRS_Test()
        {
            using (var rs = new Recordset(AdventureDb.Name))
            {
                await rs.Open("SELECT TOP 100 * FROM HumanResources.Employee");
                Assert.AreEqual(100, rs.RecordCount);
            }
        }

        [TestMethod]
        public async Task OpenRS_Sproc_Test()
        {
            using (var rs = new Recordset(AdventureDb.Name))
            {
                var idParam = new GenericParameter<int>("BusinessEntityID", 5);
                int rows = await rs.Open("uspGetEmployeeManagers", CommandType.StoredProcedure, idParam);
                
                Assert.IsTrue(rows > 0);
            }
        }

        [TestMethod]
        public async Task UpdateRS_Test()
        {
            using (var rs = new Recordset(AdventureDb.Name))
            {
                await rs.Open("SELECT TOP 10 * FROM Person.PersonPhone WHERE BusinessEntityID > 200");
                Assert.AreEqual(10, rs.RecordCount);

                rs.MoveNext();
                rs["PhoneNumber"] = Guid.NewGuid().ToString().Substring(0, 10);

                rs.InitUpdate("Person.PersonPhone", "BusinessEntityID");
                rs.UseQuotedIdentifier = false;

                int recordsUpdated = await rs.UpdateBatch();
                Assert.AreEqual(1, recordsUpdated);
            }
        }
    }
}
