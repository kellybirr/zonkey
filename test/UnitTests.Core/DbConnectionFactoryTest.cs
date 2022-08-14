using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;

#if (NET48)
    using System.Data.SqlClient;
#else
    using Microsoft.Data.SqlClient;
#endif

namespace Zonkey.UnitTests
{

    /// <summary>
    /// Tests for the Db Connection Factory Class
    /// Documentation: 
    /// </summary>
    [TestClass]
    public class DbConnectionFactoryTest
    {
        [ClassInitialize]
        public static void Initialize(TestContext context)
        {
#if (NET48)
            DbConnectionFactory.LoadConnectionStrings(false);
#endif
        }

        [TestMethod]
        public void CreateConnectionTest_MsSql()
        {
            CreateConnectionTest_Internal("MsSql", typeof(SqlConnection));
            CreateConnectionTest_Internal("MSSQL", typeof(SqlConnection));
        }

#if (NET48)
        [TestMethod]
        public void CreateConnectionTest_OleDb()
        {
            CreateConnectionTest_Internal("OleDb", typeof(System.Data.OleDb.OleDbConnection));
            CreateConnectionTest_Internal("OleDB", typeof(System.Data.OleDb.OleDbConnection));
        }

        [TestMethod]
        public void CreateConnectionTest_Odbc()
        {
            CreateConnectionTest_Internal("Odbc", typeof(System.Data.Odbc.OdbcConnection));
            CreateConnectionTest_Internal("odbc", typeof(System.Data.Odbc.OdbcConnection));
        }
#endif

        static void CreateConnectionTest_Internal(string name, Type expectedType)
        {
            DateTime methodStartTime = DateTime.Now;

            var connection = DbConnectionFactory.CreateConnection(name);
            Assert.IsInstanceOfType(connection, expectedType);

            Assert.AreEqual(connection.State, System.Data.ConnectionState.Closed);

            var methodDuration = DateTime.Now.Subtract(methodStartTime);
            Console.WriteLine($"Zonkey.DbConnectionFactory.GetConnection Time Elapsed: {methodDuration}");
        }
    }
}
