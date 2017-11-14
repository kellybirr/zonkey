using Microsoft.VisualStudio.TestTools.UnitTesting;
using Zonkey.SqlServer;

namespace Zonkey.UnitTests
{
    [TestClass]
    public class SqlXmlAdapterTest
    {
        [TestMethod, Ignore]
        public void GetXmlDocument_Test()
        {
            var cxn = DbConnectionFactory.OpenConnection("AAFX_Test").Result;
            var xa = new SqlXmlAdapter(cxn);

            var doc = xa.GetXmlDocument("myXml",
                                        "SELECT TOP 3 Contact.DisplayName, Contact.ContactID, Driver.FirstName, Driver.LastName FROM Contacts Contact INNER JOIN SubContacts Driver ON Contact.ContactID = Driver.ContactID FOR XML AUTO",
                                        false).Result;

            Assert.IsTrue(doc.OuterXml.Length > 100);
        }
    }
}
