using System;
using System.Linq.Expressions;
using System.Text;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Zonkey.Dialects;
using Zonkey.Extensions;
using Zonkey.UnitTests.AdventureWorks.DataObjects;
using Zonkey.UnitTests.SqlServer;

namespace Zonkey.UnitTests
{
    [TestClass]
    public class WhereExpressionParserTest
    {
        [TestMethod]
        public void ParseTest_Constants()
        {
            DoParseTest1(true, '$', "((id < $0) AND (type = $1))", 2);

            DoParseTest1(true, 'ë', "((id < ë0) AND (type = ë1))", 2);

            DoParseTest1(false, '$', "((id < 1234) AND (type = 'U'))", 0);
        }

        static void DoParseTest1(bool pl, char pf, string expectedSql, int expectedParms)
        {
            var parser = new ObjectModel.WhereExpressionParser<SysObject>
            {
                ParameterizeLiterals = pl,
                ParameterPrefix = pf
            };

            var result = parser.Parse(o => o.id < 1234 && o.type == "U");

            Assert.AreEqual(expectedSql, result.SqlText);
            Assert.AreEqual(expectedParms, result.Parameters.Length);
        }

        [TestMethod]
        public void ParseTest_IsNull_String()
        {
            DoParseTest2<Person_Person>(c => (c.Title == null), "(Title IS NULL)", 0);

            DoParseTest2<SysObject>(o => (o.type == null), "(type IS NULL)", 0);
        }

        [TestMethod]
        public void ParseTest_AnsiNull()
        {
            string s = null;
            DateTime? myDate = null;
            int id = default(int);

            var parser = new ObjectModel.WhereExpressionParser<Person_Person>
            {
                AnsiNullCompensation = true, 
                ParameterizeLiterals = true
            };

            var result = parser.Parse(c => (c.Title == s) && (c.MiddleName != s) &&  (c.BusinessEntityID == id));
            Assert.AreEqual("(((Title IS NULL) AND (MiddleName IS NOT NULL)) AND (BusinessEntityID = $0))", result.SqlText);
            Assert.AreEqual(1, result.Parameters.Length);

            result = parser.Parse(c => (c.BusinessEntityID == id) && (c.FirstName == "Zonkey") && (c.Title == s));
            Assert.AreEqual("(((BusinessEntityID = $0) AND (FirstName = $1)) AND (Title IS NULL))", result.SqlText);
            Assert.AreEqual(2, result.Parameters.Length);

            result = parser.Parse(c => (c.BusinessEntityID == id) && (c.ModifiedDate > myDate));
            Assert.AreEqual("((BusinessEntityID = $0) AND (ModifiedDate > $1))", result.SqlText);
            Assert.AreEqual(2, result.Parameters.Length);

            result = parser.Parse(c => (c.Title == s) && (c.Suffix != s) && (c.ModifiedDate == myDate));
            Assert.AreEqual("(((Title IS NULL) AND (Suffix IS NOT NULL)) AND (ModifiedDate IS NULL))", result.SqlText);
            Assert.AreEqual(0, result.Parameters.Length);

            myDate = DateTime.Today;    // run again with non-null
            result = parser.Parse(c => (c.Title == s) && (c.MiddleName != s) && (c.ModifiedDate == myDate));
            Assert.AreEqual("(((Title IS NULL) AND (MiddleName IS NOT NULL)) AND (ModifiedDate = $0))", result.SqlText);
            Assert.AreEqual(1, result.Parameters.Length);
        }

        [TestMethod]
        public void ParseTest_IsNotNull_String()
        {
           DoParseTest2<Person_Person>(c => (c.MiddleName != null), "(MiddleName IS NOT NULL)", 0);
        }

        [TestMethod]
        public void ParseTest_IsNull_Date()
        {
            DoParseTest2<Person_Person>(c => (c.ModifiedDate == null), "(ModifiedDate IS NULL)", 0);
        }

        [TestMethod]
        public void ParseTest_IsNotNull_Date()
        {
            DoParseTest2<Person_Person>(c => (c.ModifiedDate != null), "(ModifiedDate IS NOT NULL)", 0);
        }

#if (fasle)
        [TestMethod]
        public void ParseTest_Converted()
        {
            DoParseTest2<Contact1>(c => (c.NumKids != null), "(NumKids IS NOT NULL)", 0);

            int intNumKids = 14;
            DoParseTest2<Contact1>(c => (c.NumKids == intNumKids), "(NumKids = $0)", 1);

            byte byteNumKids = 221;
            DoParseTest2<Contact1>(c => (c.NumKids != byteNumKids), "(NumKids != $0)", 1);

            long longNumKids = 35435;
            DoParseTest2<Contact1>(c => (c.NumKids < longNumKids), "(NumKids < $0)", 1);

            byte? testType = 11;
            DoParseTest2<Right>(r => r.RightId == testType, "(RightId = $0)", 1);

            DoParseTest2<Right>(r => r.RightId < 100, "(RightId < $0)", 1);
        }
#endif

        [TestMethod]
        public void Join_Test()
        {
            Expression<Func<Sales_SalesOrderHeader, Sales_SalesOrderDetail, Sales_SalesReason, bool>> exp =
                ((h, d, r) => h.SalesOrderID == d.SalesOrderID && h.Comment.Contains(r.Name));

            var parser = new ObjectModel.WhereExpressionParser();
            var result = parser.Parse(exp);

            Assert.AreEqual("((SalesOrderHeader.SalesOrderID = SalesOrderDetail.SalesOrderID) AND (SalesOrderHeader.Comment Like ('%'+SalesReason.Name+'%')))", result.SqlText);
            Assert.AreEqual(0, result.Parameters.Length);
        }

        [TestMethod]
        public void Linq_SqlInGuid_Test_1()
        {
            var guidArray = new[] { Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid() };

            Expression<Func<Person_Person, bool>> exp = (c => c.rowguid.SqlInGuid(guidArray));

            var parser = new ObjectModel.WhereExpressionParser();
            var result = parser.Parse(exp);

            Assert.AreEqual(string.Format("(rowguid IN ('{0}','{1}','{2}','{3}'))", guidArray[0], guidArray[1], guidArray[2], guidArray[3])
                , result.SqlText);
            Assert.AreEqual(0, result.Parameters.Length);

        }

        [TestMethod]
        public void Linq_SqlInGuid_Test_2()
        {
            var gArray = new Guid[125000];
            for (int i = 0; i < gArray.Length; i++)
                gArray[i] = Guid.NewGuid();

            Expression<Func<Person_Person, bool>> exp = (c => c.rowguid.SqlInGuid(gArray));

            var parser = new ObjectModel.WhereExpressionParser();
            var result = parser.Parse(exp);

            var sb = new StringBuilder();
            foreach (Guid g in gArray)
            {
                if (sb.Length == 0)
                    sb.Append("(rowguid IN (");
                else
                    sb.Append(',');

                sb.AppendFormat("'{0}'", g);
            }
            sb.Append("))");

            Assert.AreEqual(sb.ToString(), result.SqlText);
            Assert.AreEqual(0, result.Parameters.Length);

        }

        [TestMethod]
        public void Linq_SqlInInt_Test_1()
        {
            var intArray = new Int32?[] { 1, 2, 3, 4, 5, 6, 7, 8, 9 };

            Expression<Func<Production_Product, bool>> exp = (c => c.ProductSubcategoryID.SqlInInt(intArray));

            var parser = new ObjectModel.WhereExpressionParser();
            var result = parser.Parse(exp);

            Assert.AreEqual("(ProductSubcategoryID IN (1,2,3,4,5,6,7,8,9))", result.SqlText);
            Assert.AreEqual(0, result.Parameters.Length);

        }

        [TestMethod]
        public void Linq_SqlInInt_Test_2()
        {
            var intArray = new Int32[125000];
            for (int i = 0; i < intArray.Length; i++)
                intArray[i] = Environment.TickCount + i;

            Expression<Func<Person_Person, bool>> exp = (c => c.BusinessEntityID.SqlInInt(intArray));

            var parser = new ObjectModel.WhereExpressionParser();
            var result = parser.Parse(exp);

            var sb = new StringBuilder();
            foreach (int i in intArray)
            {
                if (sb.Length == 0)
                    sb.Append("(BusinessEntityID IN (");
                else
                    sb.Append(',');

                sb.Append(i);
            }
            sb.Append("))");

            Assert.AreEqual(sb.ToString(), result.SqlText);
            Assert.AreEqual(0, result.Parameters.Length);

        }

        [TestMethod]
        public void Linq_SqlIn_Test_1()
        {
            var intArray = new Int32?[] { 1, 2, 3, 4, 5, 6, 7, 8, 9 };

            Expression<Func<Production_Product, bool>> exp = (c => c.ProductSubcategoryID.SqlIn(intArray));

            var parser = new ObjectModel.WhereExpressionParser();
            var result = parser.Parse(exp);

            Assert.AreEqual("(ProductSubcategoryID IN ($0,$1,$2,$3,$4,$5,$6,$7,$8))", result.SqlText);
            Assert.AreEqual(9, result.Parameters.Length);

        }


        [TestMethod]
        public void Linq_SqlIn_Test_2()
        {
            var intArray = new[] { "1", "2", "3", "4", "5" };

            Expression<Func<Person_Person, bool>> exp = (c => c.Title.SqlIn(intArray));

            var parser = new ObjectModel.WhereExpressionParser();
            var result = parser.Parse(exp);

            Assert.AreEqual("(Title IN ($0,$1,$2,$3,$4))", result.SqlText);
            Assert.AreEqual(5, result.Parameters.Length);

        }

        [TestMethod]
        public void Linq_SqlIn_Test_2_b()
        {
            var intArray = new[] { "1", "2", "3", "4", "5" };

            Expression<Func<Person_Person, bool>> exp = (c => c.Title.SqlIn(intArray));

            var parser = new ObjectModel.WhereExpressionParser {ParameterIndexModifier = 6};
            var result = parser.Parse(exp);

            Assert.AreEqual("(Title IN ($6,$7,$8,$9,$10))", result.SqlText);
            Assert.AreEqual(5, result.Parameters.Length);

        }

        [TestMethod, ExpectedException(typeof(ArgumentException))]
        public void Linq_SqlIn_Test_3()
        {
            var intArray = new Int32[2500];
            for (int i = 0; i < intArray.Length; i++)
                intArray[i] = Environment.TickCount;

            Expression<Func<Person_Person, bool>> exp = (c => c.BusinessEntityID.SqlIn(intArray));

            var parser = new ObjectModel.WhereExpressionParser();
            parser.Parse(exp);
        }
#if (false)
        [TestMethod]
        public void Linq_SqlIn_Test_4()
        {
            Expression<Func<Contact1, bool>> exp = (c => c.ContactID.SqlIn(
                    (ContactDC dc) => dc.ContactID, dc => dc.Email.Contains("kelly")
                )
            );

            var parser = new ObjectModel.WhereExpressionParser();
            var result = parser.Parse(exp);

            Assert.AreEqual("(ContactID IN (SELECT ContactID FROM Contacts WHERE (Email Like ('%'+$0+'%'))))", result.SqlText);
            Assert.AreEqual(1, result.Parameters.Length);

        }

        [TestMethod]
        public void Linq_SqlIn_Test_5()
        {
            Expression<Func<Contact1, bool>> exp = (c => c.ContactID.SqlIn((ContactDC dc) => (!dc.BirthDate.HasValue)) && c.IsPrimary);

            var parser = new ObjectModel.WhereExpressionParser {SqlDialect = new SqlServerDialect()};
            var result = parser.Parse(exp);

            Assert.AreEqual("(([ContactID] IN (SELECT [ContactID] FROM [Contacts] WHERE ( NOT ([BirthDate] IS NULL)))) AND ([IsPrimary] = 1))", result.SqlText);
            Assert.AreEqual(0, result.Parameters.Length);

        }

        [TestMethod]
        public void Linq_SqlIn_Test_6()
        {
            string dummyVal = "zdjhsadlkjh";
            DateTime testDate = DateTime.Now;
            Expression<Func<Contact1, bool>> exp = (c => 
                    c.Fax.Contains("test") 
                &&  c.ContactID.SqlIn(
                        (ContactDC dc) => 
                                (!dc.BirthDate.HasValue) 
                            &&  dc.AutoQualPolicyNum == dummyVal
                    ) 
                &&  c.BirthDate > testDate
            );

            var parser = new ObjectModel.WhereExpressionParser();
            var result = parser.Parse(exp);

            Assert.AreEqual("(((Fax Like ('%'+$0+'%')) AND (ContactID IN (SELECT ContactID FROM Contacts WHERE (( NOT (BirthDate IS NULL)) AND (AutoQualPolicyNum = $1))))) AND (BirthDate > $2))", result.SqlText);
            Assert.AreEqual(3, result.Parameters.Length);
        }

        [TestMethod, ExpectedException(typeof(ArgumentOutOfRangeException))]
        public void Linq_SqlIn_Test_7()
        {
            Expression<Func<Contact1, bool>> exp = (c => c.ContactID.SqlIn((SysObject obj) => (obj.type == "table")) && c.IsPrimary);

            var parser = new ObjectModel.WhereExpressionParser();
            var result = parser.Parse(exp);

            Assert.AreEqual("((ContactID IN (SELECT ContactID FROM sysobjects WHERE (type = $0))) AND (IsPrimary = 1))", result.SqlText);
            Assert.AreEqual(1, result.Parameters.Length);
        }

        [TestMethod]
        public void Linq_SqlIn_Test_7_b()
        {
            try
            {
                Expression<Func<Contact1, bool>> exp = (c => c.ContactID.SqlIn((SysObject obj) => (obj.type == "table")) && c.IsPrimary);

                var parser = new ObjectModel.WhereExpressionParser();
                parser.Parse(exp);

                Assert.Fail("Did not throw expected exception");
            }
            catch (ArgumentOutOfRangeException ex)
            {
                Assert.AreEqual("Field `ContactID` not found on object `sysobjects`\r\nParameter name: fieldExpression", ex.Message);
            }			
        }

        [TestMethod]
        public void Linq_SqlIn_Test_8()
        {
            const string login = "C2V8";
            const string orgCode = "PacNW";

            Expression<Func<UserDC, bool>> exp = (u => 
                u.Login == login && u.RowStatus == 0 && 
                u.OrgID.SqlIn((OrganizationDC org) => org.Code == orgCode)
                );

            var parser = new ObjectModel.WhereExpressionParser {SqlDialect = new SqlServerDialect()};
            var result = parser.Parse(exp);

            Assert.AreEqual("((([Login] = $0) AND ([RowStatus] = $1)) AND ([OrgID] IN (SELECT [OrgID] FROM [Organizations] WHERE ([Code] = $2))))", result.SqlText);
            Assert.AreEqual(3, result.Parameters.Length);
        }
#endif
        [TestMethod]
        public void ParseTest_Bool_Unary_True()
        {
            DoParseTest2<Production_Product>(c => (c.FinishedGoodsFlag), "(FinishedGoodsFlag = 1)", 0);
        }

        [TestMethod]
        public void ParseTest_Bool_Unary_False()
        {
            DoParseTest2<Production_Product>(c => (! c.FinishedGoodsFlag), "( NOT (FinishedGoodsFlag = 1))", 0);
        }

        static void DoParseTest2<T>(Expression<Func<T, bool>> expression, string expectedSql, int expectedParms)
        {
            var parser = new ObjectModel.WhereExpressionParser<T>();
            var result = parser.Parse(expression);

            Assert.AreEqual(expectedSql, result.SqlText);
            Assert.AreEqual(expectedParms, result.Parameters.Length);
        }

#if (fasle)
        [TestMethod]
        public void DoParaseTest_User_funky()
        {
            string orgUnitId = "/1/";
            var ugids = new[] { Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid() };

            DoParseTest2<UserDC>(
                u => u.OrgUnitID != orgUnitId
                    && u.UserID.SqlIn((UserGroupUserDC ugu) => ugu.UserGroupID.SqlIn(ugids))
                    , "((OrgUnitID != $0) AND (UserID IN (SELECT UserID FROM UserGroupUsers WHERE (UserGroupID IN ($1,$2,$3)))))", 4);

        }

        [TestMethod]
        public void StaticParse_Test_1()
        {
            DoParseTest2<ImpFile>(
                f => f.ComputerID == _cid && f.LastCheckUTC < _scanTime,
                "((ComputerID = $0) AND (LastCheckUTC < $1))", 2);
        }
        private static readonly DateTime _scanTime = DateTime.UtcNow;
        private static int _cid = 12;

        [TestMethod]
        public void StaticParse_Test_2()
        {
            DoParseTest2<ImpFile>(
                f => f.ComputerID == _f2.ComputerID && f.LastCheckUTC < _scanTime,
                "((ComputerID = $0) AND (LastCheckUTC < $1))", 2);
        }

        private static readonly ImpFile _f2 = new ImpFile(false) {ComputerID = 15};
#endif
    }
}
