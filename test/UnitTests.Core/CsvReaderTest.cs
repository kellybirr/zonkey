using System.IO;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Zonkey.Text;

namespace Zonkey.UnitTests
{
    [TestClass]
    public class CsvReaderTest
    {
        private static string _testFileName;
        private static readonly string[] _testLines = {
            @"""LastName"",""FirstName"",""Address"",""County"",""City"",""State"",""ZipCode"",""Zip+4"",""Key"",""LineOfTravel"",""AutomatedZip"",""Zip+2"",""CheckDigit"",""LineOfTravelAltSeqCode"""
            ,@"""ABERNATHY"",""JAMES"",""3110 HAMMOCK CREEK CT"",""247"",""CONYERS"",""GA"",""30012"",""-2787"",,""0109"",""Y"",""10"",""9"",""L0"""
            ,@"""ADAMS"",""EDWARD"",""26 LAKEVIEW DR NW"",""247"",""CONYERS"",""GA"",""30012"",""-3169"",,""0135"",""Y"",""26"",""7"",""H3"""
            ,@"""ALEXANDER SR"",""JAMES"",""3240 GLENCREE"",""247"",""CONYERS"",""GA"",""30012"",""-8101"",,""0127"",""Y"",""40"",""0"",""F9"""
            ,@"""ALLEN"",""ARA"",""1411 EASTVIEW RD NE"",""247"",""CONYERS"",""GA"",""30012"",""-3809"",,""0014"",""Y"",""11"",""2"",""L1"""
            ,@"""ALLEN"",""ROBERTA"",""2907 BARCELONA WAY"",""247"",""CONYERS"",""GA"",""30012"",""-2776"",,""0080"",""Y"",""07"",""5"",""K7"""
            ,@"""ALMAND"",""HAROLD"",""2400 VENETIAN CIR"",""247"",""CONYERS"",""GA"",""30012"",""-2953"",,""0060"",""Y"",""00"",""5"",""J9"""
            ,@"""ANDERSEN"",""JOHN"",""2630 KING GEORGE CT NE"",""247"",""CONYERS"",""GA"",""30012"",""-2632"",,""0006"",""Y"",""30"",""8"",""G9"""
            ,@"""ANDERSON"",""BOYNEVA"",""1359 LATTA DR NW"",""247"",""CONYERS"",""GA"",""30012"",""-4128"",,""0034"",""Y"",""59"",""5"",""P9"""
            ,@"""ANDERSON"",""OPAL"",""574 OAK CT NW"",""247"",""CONYERS"",""GA"",""30012"",""-4141"",,""0000"",""Y"",""74"",""3"","
            ,@"""ANDREW"",""JOHN"",""1187 CARDINAL RD NE"",""247"",""CONYERS"",""GA"",""30012"",""-4810"",,""0076"",""Y"",""87"",""6"",""S7"""
            ,@"""ANDROS"",""JUNE"",""1383 NORTHSIDE DR NW"",""247"",""CONYERS"",""GA"",""30012"",""-4101"",,""0056"",""Y"",""83"",""7"",""S3"""
            ,@"""ANSLEY"",""JAMES"",""982 BETHEL RD NW"",""247"",""CONYERS"",""GA"",""30012"",""-2172"",,""0072"",""Y"",""82"",""2"",""B7"""
            ,@"""ANTHONY"",""JACK"",""3459 E HIGHTOWER TRL"",""247"",""CONYERS"",""GA"",""30012"",""-1938"",,""0097"",""Y"",""59"",""9"",""E0"""
            ,@"""AUXIER"",""JAMES"",""2067 MONTEREY DR"",""247"",""CONYERS"",""GA"",""30012"",""-2752"",,""0084"",""Y"",""67"",""5"",""Q7"""
        };

        [ClassInitialize]
        public static void CreateTestFile(TestContext context)
        {
            _testFileName = Path.GetTempFileName();

            File.WriteAllLines(_testFileName, _testLines);
        }

        [ClassCleanup]
        public static void Cleanup()
        {
            if (File.Exists(_testFileName))
                File.Delete(_testFileName);
        }

        [TestMethod]
        public void ViewSourceLine_Test()
        {
            using (var stream = File.OpenText(_testFileName))
            {
                using (var csvReader = new CsvReader(stream))
                {
                    for (int i = 0; i < _testLines.Length; ++i)
                    {
                        csvReader.Read();

                        Assert.AreEqual(_testLines[i], csvReader.SourceLine, string.Format("Source Line check fails on line {0}", csvReader.LineNumber));
                    }
                }
            }
        }

        [TestMethod]
        public void DyanmicReader_Cased_Test()
        {
            using (var _reader = new DynamicCsvReader(File.OpenText(_testFileName)))
            {
                dynamic obj1 = _reader.Read();
                Assert.AreEqual("CONYERS", obj1.City);
            }
        }

        [TestMethod]
        public void DyanmicReader_Lower_Test()
        {

            using (var _reader = new DynamicCsvReader(File.OpenText(_testFileName))
            {
                ForceLowerCaseNames = true
            })
            {
                dynamic obj1 = _reader.Read();
                Assert.AreEqual("3110 HAMMOCK CREEK CT", obj1.address);
            }
        }
    }

    [TextRecord(TextRecordType.Delimited, true)]
    class TextPerson
    {
        [TextField]
        public string LastName { get; set; }

        [TextField]
        public string FirstName { get; set; }

        [TextField]
        public string Address { get; set; }

        [TextField]
        public string County { get; set; }

        [TextField]
        public string City { get; set; }

        [TextField]
        public string State { get; set; }

        [TextField]
        public string ZipCode { get; set; }

        [TextField]
        public string Zip4 { get; set; }

        [TextField]
        public string Key { get; set; }

        [TextField]
        public string LineOfTravel { get; set; }

        [TextField]
        public string AutomatedZip { get; set; }

        [TextField]
        public int Zip2 { get; set; }

        [TextField]
        public char CheckDigit { get; set; }

        [TextField]
        public string LineOfTravelAltSeqCodepublic { get; set; }
    }
}
