using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Zonkey.UnitTests
{
    [TestClass]
    public class EnvironmentTests
    {
        [TestMethod]
        public void ProcessString_Test()
        {
            string computername = Environment.MachineName;
            string name44 = computername.Substring(4,4);

            Assert.AreEqual(computername, EnvironmentHelper.ProcessString("%COMPUTERNAME%"));
            Assert.AreEqual(name44, EnvironmentHelper.ProcessString("%COMPUTERNAME[4,4]%"));
            Assert.AreEqual(name44, EnvironmentHelper.ProcessString("%computerName[4,4]%"));

            Assert.AreEqual(
                $"Server={computername};Database={name44};",
                EnvironmentHelper.ProcessString(
                    "Server=%COMPUTERNAME%;Database=%COMPUTERNAME[4,4]%;"
                    )
                );
        }
    }
}
