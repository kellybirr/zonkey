using System.Collections.Generic;
using System.IO;
using Xunit;
using Zonkey.Text;

namespace Zonkey.Tests.Unit
{
    /// <summary>
    /// Minimal coverage for the Zonkey.Text package (previously had zero tests): a write->read
    /// round trip through the primary TextClassWriter/TextClassReader types, and a check on the
    /// low-level CsvReader primitive they both build on for quoted fields containing the delimiter.
    /// </summary>
    public class TextSmokeTests
    {
        [TextRecord(TextRecordType.Delimited)]
        private class CsvRow
        {
            [TextField(0)]
            public string Name { get; set; }

            [TextField(1)]
            public int Age { get; set; }

            [TextField(2)]
            public string Notes { get; set; }
        }

        [Fact]
        public void WriteThenRead_RoundTrips_IncludingQuotedCommaField()
        {
            var rows = new[]
            {
                new CsvRow { Name = "Alice", Age = 30, Notes = "likes cats" },
                new CsvRow { Name = "Bob", Age = 45, Notes = "prefers dogs, and birds" }
            };

            string csvText;
            using (var sw = new StringWriter())
            {
                using (var writer = new TextClassWriter<CsvRow>(sw))
                {
                    writer.Write(rows);
                    writer.Flush();
                }
                csvText = sw.ToString();
            }

            var readRows = new List<CsvRow>();
            using (var reader = new TextClassReader<CsvRow>(new StringReader(csvText)))
            {
                reader.Fill(readRows);
            }

            Assert.Equal(2, readRows.Count);

            Assert.Equal("Alice", readRows[0].Name);
            Assert.Equal(30, readRows[0].Age);
            Assert.Equal("likes cats", readRows[0].Notes);

            Assert.Equal("Bob", readRows[1].Name);
            Assert.Equal(45, readRows[1].Age);
            Assert.Equal("prefers dogs, and birds", readRows[1].Notes);
        }

        [Fact]
        public void CsvReader_ParsesQuotedFieldContainingDelimiter()
        {
            using var reader = new CsvReader(new StringReader("Bob,\"prefers dogs, and birds\""), ',', '"');

            Assert.True(reader.Read());
            Assert.Equal("Bob", reader.GetString(0));
            Assert.Equal("prefers dogs, and birds", reader.GetString(1));
        }
    }
}
