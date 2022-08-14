using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Xml.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Zonkey.ObjectModel;
using Zonkey.UnitTests.AdventureWorks;
using Zonkey.UnitTests.AdventureWorks.DataObjects;

#if (NET5_0_OR_GREATER)
    using Microsoft.Data.SqlClient;
#else
    using System.Data.SqlClient;
#endif

namespace Zonkey.UnitTests
{
    [TestClass]
    public class SaveAuditorTests
    {
        [ClassInitialize]
        public static void Initialize(TestContext context)
        {
            DbConnectionFactory.Register<SqlConnection>(AdventureDb.Name, AdventureDb.ConnectionString);
        }

        [TestMethod]
        public async Task Fill_Test_Filter()
        {
            using (var db = await AdventureDb.Open())
            {
                using (var trx = db.Connection.BeginTransaction())
                {
                    var da = new DataClassAdapter<Person_Person>(db.Connection) {Transaction = trx};
                    var col = new List<Person_Person>();

                    da.OrderBy = "FirstName";
                    var r = await da.Fill(col, SqlFilter.EQ("LastName", "Smith"), SqlFilter.GT("ModifiedDate", new DateTime(1957, 1, 1)));

                    var itmX = col[0];
                    itmX.Title = "TEST";
                    itmX.EmailPromotion = 2;
                    itmX.MiddleName = "A New Value";

                    using (new SaveAuditor(da, AuditHandler))
                        da.Save(itmX).Wait();

                    Assert.IsTrue(r > 0);
                    Assert.IsTrue(col.Count > 0);

                    trx.Rollback();
                }
            }
        }

        private void AuditHandler(SaveAudit audit)
        {
            Console.WriteLine(audit.ToString(SaveOptions.None));
        }
    }
}
