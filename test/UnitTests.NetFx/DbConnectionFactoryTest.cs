using System;
using System.Data.Odbc;
using System.Data.OleDb;
using System.Data.SqlClient;
using Microsoft.VisualStudio.TestTools.UnitTesting;

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
            DbConnectionFactory.LoadConnectionStrings(false);
        }

        [TestMethod]
        public void CreateConnectionTest_MsSql()
        {
            CreateConnectionTest_Internal("MsSql", typeof(SqlConnection));
            CreateConnectionTest_Internal("MSSQL", typeof(SqlConnection));
        }

        [TestMethod]
        public void CreateConnectionTest_OleDb()
        {
            CreateConnectionTest_Internal("OleDb", typeof(OleDbConnection));
            CreateConnectionTest_Internal("OleDB", typeof(OleDbConnection));
        }

        [TestMethod]
        public void CreateConnectionTest_Odbc()
        {
            CreateConnectionTest_Internal("Odbc", typeof(OdbcConnection));
            CreateConnectionTest_Internal("odbc", typeof(OdbcConnection));
        }


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
