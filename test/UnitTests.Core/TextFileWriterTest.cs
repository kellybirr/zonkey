using System;
using System.Data;
using System.Data.Common;
using System.IO;
using System.Text;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Zonkey.Text;

namespace Zonkey.UnitTests
{
    [TestClass]
    public class TextFileWriterTest
    {
        [TestMethod, Ignore]
        public void WriteMissingDelimited_Test()
        {
            using (var swOut = new StreamWriter(File.OpenWrite("c:\\temp\\ilead_flat_file.txt")))
            using (var writer = new TextClassWriter<ExportRecord>(swOut))
            {
                writer.TextQualifyAllFields = true;
                writer.MissingDelimitedFieldBehavior = MissingDelimitedFieldBehavior.WriteAsEmptyString;

                swOut.WriteLine("BEGIN CONTACT HISTORY REPORT");

                using (DbConnection cnxn = DbConnectionFactory.OpenConnection("ccil_test").Result)
                {
                    var da = new DataManager(cnxn);
                    using ( DbDataReader reader = da.GetDataReader("spExportSF30", CommandType.StoredProcedure).Result )
                    {
                        while (reader.Read())
                        {
                            var rec = new ExportRecord
                            {
                                ID = "AggLead-" + reader["StateCode"],
                                Date = DateTime.Now,
                                Time = DateTime.Now,
                                NumReceived = (int) reader["NumReceived"],
                                NumHandled = (int) reader["NumHandled"],
                                ServiceLevel = (int) reader["ServiceLevel"],
                                AvgHandleTime = (int) reader["AvgHandleTime"],
                                Backlog = (int) reader["Backlog"]
                            };

                            writer.Write(rec);
                        }
                    }
                }

                swOut.WriteLine("END CONTACT HISTORY REPORT");
            }
        }
    }

    [TextRecord(TextRecordType.Delimited)]
    class ExportRecord
    {
        [TextField(0)]
        public string ID { get; set; }

        [TextField(1, OutputFormat = "MM/dd/yyyy")]
        public DateTime Date { get; set; }

        [TextField(2, OutputFormat = "H:mm")]
        public DateTime Time { get; set; }

        [TextField(3)]
        public int NumReceived { get; set; }

        [TextField(4)]
        public int NumHandled { get; set; }

        [TextField(10)]
        public int ServiceLevel { get; set; }

        [TextField(12)]
        public int AvgHandleTime { get; set; }

        [TextField(19)]
        public int Backlog { get; set; }
    }
}
