#if !NETFRAMEWORK
using System.Linq;
using System.Threading.Tasks;
using System.Xml;
using Xunit;
using Zonkey.SqlServer;
using Zonkey.Tests.Infrastructure;

namespace Zonkey.Tests.Integration.Mssql
{
    /// <summary>
    /// Minimal round-trip coverage for SqlXmlAdapter (SQL Server's FOR XML / ExecuteXmlReader
    /// support), which has no portable equivalent on SQLite/PostgreSQL and so, unlike
    /// ExpressionFilterTests, gets no shared-base coverage across providers. Exercises the
    /// three read paths (GetXmlDocument, FillXmlNode, GetXmlString) against the zoo schema's
    /// Animal table using plain FOR XML AUTO / FOR XML PATH queries.
    /// </summary>
    public class MssqlSqlXmlAdapterTests : IClassFixture<MssqlFixture>
    {
        private readonly MssqlFixture _db;

        public MssqlSqlXmlAdapterTests(MssqlFixture db) => _db = db;

        [Fact]
        public async Task GetXmlDocument_ForXmlAuto_ProducesOneElementPerRow()
        {
            if (!_db.IsAvailable) Assert.Skip(_db.SkipReason);

            using var conn = _db.CreateConnection();
            var adapter = new SqlXmlAdapter(conn);

            XmlDocument xdoc = await adapter.GetXmlDocument("Animals",
                "SELECT AnimalId, Name FROM Animal WHERE SpeciesId = 1 FOR XML AUTO", false);

            Assert.Equal("Animals", xdoc.DocumentElement.Name);
            Assert.Equal(2, xdoc.DocumentElement.ChildNodes.Count);

            var names = xdoc.DocumentElement.ChildNodes.Cast<XmlElement>()
                .Select(e => e.GetAttribute("Name"))
                .OrderBy(n => n)
                .ToArray();
            Assert.Equal(new[] { "Bao Bao", "Mei Mei" }, names);
        }

        [Fact]
        public async Task FillXmlNode_ReturnsTopLevelElementCount()
        {
            if (!_db.IsAvailable) Assert.Skip(_db.SkipReason);

            using var conn = _db.CreateConnection();
            var adapter = new SqlXmlAdapter(conn);

            var xdoc = new XmlDocument();
            XmlElement root = xdoc.CreateElement("Root");
            xdoc.AppendChild(root);

            int count = await adapter.FillXmlNode(root,
                "SELECT AnimalId, Name FROM Animal WHERE SpeciesId = 1 FOR XML AUTO", false);

            Assert.Equal(2, count);
            Assert.Equal(2, root.ChildNodes.Count);
        }

        [Fact]
        public async Task GetXmlString_ForXmlPath_ContainsExpectedNames()
        {
            if (!_db.IsAvailable) Assert.Skip(_db.SkipReason);

            using var conn = _db.CreateConnection();
            var adapter = new SqlXmlAdapter(conn);

            string xml = await adapter.GetXmlString(
                "SELECT Name FROM Animal WHERE SpeciesId = 1 ORDER BY Name FOR XML PATH('Animal'), ROOT('Animals')", false);

            Assert.NotNull(xml);
            Assert.Contains("<Name>Bao Bao</Name>", xml);
            Assert.Contains("<Name>Mei Mei</Name>", xml);
        }

        [Fact]
        public async Task GetXmlDocument_NoMatchingRows_ReturnsEmptyRoot()
        {
            if (!_db.IsAvailable) Assert.Skip(_db.SkipReason);

            using var conn = _db.CreateConnection();
            var adapter = new SqlXmlAdapter(conn);

            XmlDocument xdoc = await adapter.GetXmlDocument("Animals",
                "SELECT AnimalId, Name FROM Animal WHERE SpeciesId = 999 FOR XML AUTO", false);

            Assert.Empty(xdoc.DocumentElement.ChildNodes);
        }
    }
}
#endif
