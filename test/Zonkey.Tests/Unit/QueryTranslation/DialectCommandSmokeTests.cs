using System;
using System.Data;
using System.Linq;
using System.Linq.Expressions;
using Xunit;
using Zonkey;
using Zonkey.Dialects;
using Zonkey.ObjectModel;
using Zonkey.ObjectModel.QueryTranslation;
using Zonkey.Tests.Models;

namespace Zonkey.Tests.Unit.QueryTranslation
{
    // No Docker container exists for Oracle, DB2, or Access, so these dialects get no live
    // integration coverage. This pins the SQL-text generation layer directly - a representative
    // WHERE-expression set through the translator, plus direct calls to the command-shaping
    // members (FormatLimitQuery/FormatExistsQuery/FormatParameterName/FormatFieldName) - so a
    // future refactor can't silently change any of these dialects' output without a test
    // noticing. These pin CURRENT behavior, including gaps (e.g. FormatLimitQuery isn't
    // implemented for any of the three and throws), not aspirations about what "should" work.
    public class DialectCommandSmokeTests
    {
        private static SqlWhereClause T(Expression<Func<Animal, bool>> e, SqlDialect d)
            => TranslationTestHelper.Translate(e, d);

        // ======================= Oracle =======================

        [Fact]
        public void Oracle_ComparisonAndLogic()
        {
            Assert.Equal("((SpeciesId = $0) AND (Name = $1))",
                T(a => a.SpeciesId == 1 && a.Name == "x", new OracleSqlDialect()).SqlText);
        }

        [Fact]
        public void Oracle_LikeViaStartsWith()
        {
            var r = T(a => a.Name.StartsWith("Mei"), new OracleSqlDialect());
            Assert.Equal("(Name LIKE $0)", r.SqlText);
            Assert.Equal("Mei%", r.Parameters[0]);
        }

        [Fact]
        public void Oracle_InList()
        {
            var ids = new[] { 1, 2, 3 };
            var r = TranslationTestHelper.Translate<Animal>(a => ids.Contains(a.SpeciesId), new OracleSqlDialect());
            Assert.Equal("(SpeciesId IN ($0,$1,$2))", r.SqlText);
        }

        [Fact]
        public void Oracle_DatePart()
        {
            Assert.Equal("(EXTRACT(YEAR FROM DateOfBirth) = $0)",
                T(a => a.DateOfBirth.Value.Year == 2020, new OracleSqlDialect()).SqlText);
        }

        [Fact]
        public void Oracle_MathAbs()
        {
            Assert.Equal("(ABS(Weight) > $0)",
                T(a => Math.Abs(a.Weight.Value) > 5m, new OracleSqlDialect()).SqlText);
        }

        [Fact]
        public void Oracle_StringFuncChain()
        {
            Assert.Equal("(UPPER(TRIM(Name)) = $0)",
                T(a => a.Name.Trim().ToUpper() == "MEI MEI", new OracleSqlDialect()).SqlText);
        }

        [Fact]
        public void Oracle_FormatLimitQuery_NotSupported()
        {
            Assert.Throws<NotSupportedException>(() =>
                new OracleSqlDialect().FormatLimitQuery("*", "Animal", "", "", 0, 10));
        }

        [Fact]
        public void Oracle_FormatExistsQuery_UsesDualTable()
        {
            Assert.Equal(
                "SELECT CASE WHEN EXISTS(SELECT 1 FROM Animal) THEN 1 ELSE 0 END AS ZONKEY_EXISTS FROM DUAL",
                new OracleSqlDialect().FormatExistsQuery("Animal", ""));
        }

        [Fact]
        public void Oracle_FormatParameterName()
        {
            var d = new OracleSqlDialect();
            Assert.Equal(":Name", d.FormatParameterName("Name", CommandType.Text));
            Assert.Equal(":p0", d.FormatParameterName(0, CommandType.Text));
        }

        [Fact]
        public void Oracle_FormatFieldName()
        {
            var d = new OracleSqlDialect();
            Assert.Equal("Name", d.FormatFieldName("Name", null));
            Assert.Equal("\"Name\"", d.FormatFieldName("Name", true));
        }

        // ======================= DB2 =======================

        [Fact]
        public void Db2_ComparisonAndLogic()
        {
            Assert.Equal("((SpeciesId = $0) AND (Name = $1))",
                T(a => a.SpeciesId == 1 && a.Name == "x", new DB2SqlDialect()).SqlText);
        }

        [Fact]
        public void Db2_LikeViaStartsWith()
        {
            var r = T(a => a.Name.StartsWith("Mei"), new DB2SqlDialect());
            Assert.Equal("(Name LIKE $0)", r.SqlText);
            Assert.Equal("Mei%", r.Parameters[0]);
        }

        [Fact]
        public void Db2_InList()
        {
            var ids = new[] { 1, 2, 3 };
            var r = TranslationTestHelper.Translate<Animal>(a => ids.Contains(a.SpeciesId), new DB2SqlDialect());
            Assert.Equal("(SpeciesId IN ($0,$1,$2))", r.SqlText);
        }

        [Fact]
        public void Db2_DatePart()
        {
            Assert.Equal("(EXTRACT(YEAR FROM DateOfBirth) = $0)",
                T(a => a.DateOfBirth.Value.Year == 2020, new DB2SqlDialect()).SqlText);
        }

        [Fact]
        public void Db2_MathAbs()
        {
            Assert.Equal("(ABS(Weight) > $0)",
                T(a => Math.Abs(a.Weight.Value) > 5m, new DB2SqlDialect()).SqlText);
        }

        [Fact]
        public void Db2_StringFuncChain()
        {
            Assert.Equal("(UPPER(TRIM(Name)) = $0)",
                T(a => a.Name.Trim().ToUpper() == "MEI MEI", new DB2SqlDialect()).SqlText);
        }

        [Fact]
        public void Db2_FormatLimitQuery_NotSupported()
        {
            Assert.Throws<NotSupportedException>(() =>
                new DB2SqlDialect().FormatLimitQuery("*", "Animal", "", "", 0, 10));
        }

        [Fact]
        public void Db2_FormatExistsQuery_UsesSysDummy()
        {
            Assert.Equal(
                "SELECT CASE WHEN EXISTS(SELECT 1 FROM Animal) THEN 1 ELSE 0 END AS ZONKEY_EXISTS FROM SYSIBM.SYSDUMMY1",
                new DB2SqlDialect().FormatExistsQuery("Animal", ""));
        }

        [Fact]
        public void Db2_FormatParameterName()
        {
            var d = new DB2SqlDialect();
            Assert.Equal("?", d.FormatParameterName("Name", CommandType.Text));
            Assert.Equal("?", d.FormatParameterName(0, CommandType.Text));
        }

        [Fact]
        public void Db2_FormatFieldName()
        {
            var d = new DB2SqlDialect();
            Assert.Equal("Name", d.FormatFieldName("Name", null));
            Assert.Equal("\"Name\"", d.FormatFieldName("Name", true));
        }

        // ======================= Access =======================

        [Fact]
        public void Access_ComparisonAndLogic()
        {
            Assert.Equal("(([SpeciesId] = $0) AND ([Name] = $1))",
                T(a => a.SpeciesId == 1 && a.Name == "x", new AccessSqlDialect()).SqlText);
        }

        [Fact]
        public void Access_LikeViaStartsWith()
        {
            var r = T(a => a.Name.StartsWith("Mei"), new AccessSqlDialect());
            Assert.Equal("([Name] LIKE $0)", r.SqlText);
            Assert.Equal("Mei%", r.Parameters[0]);
        }

        [Fact]
        public void Access_InList()
        {
            var ids = new[] { 1, 2, 3 };
            var r = TranslationTestHelper.Translate<Animal>(a => ids.Contains(a.SpeciesId), new AccessSqlDialect());
            Assert.Equal("([SpeciesId] IN ($0,$1,$2))", r.SqlText);
        }

        [Fact]
        public void Access_DatePart()
        {
            Assert.Equal("(DatePart('yyyy', [DateOfBirth]) = $0)",
                T(a => a.DateOfBirth.Value.Year == 2020, new AccessSqlDialect()).SqlText);
        }

        [Fact]
        public void Access_MathAbs()
        {
            Assert.Equal("(ABS([Weight]) > $0)",
                T(a => Math.Abs(a.Weight.Value) > 5m, new AccessSqlDialect()).SqlText);
        }

        [Fact]
        public void Access_StringFuncChain()
        {
            Assert.Equal("(UCase(Trim([Name])) = $0)",
                T(a => a.Name.Trim().ToUpper() == "MEI MEI", new AccessSqlDialect()).SqlText);
        }

        [Fact]
        public void Access_FormatLimitQuery_NotSupported()
        {
            Assert.Throws<NotSupportedException>(() =>
                new AccessSqlDialect().FormatLimitQuery("*", "Animal", "", "", 0, 10));
        }

        [Fact]
        public void Access_FormatExistsQuery_UsesAnsiForm_NoSpecialFromClause()
        {
            // Access has no FormatExistsQuery override - it falls through to the base ANSI
            // "CASE WHEN EXISTS" form, unlike Oracle/DB2 which need a dummy FROM table.
            Assert.Equal(
                "SELECT CASE WHEN EXISTS(SELECT 1 FROM Animal) THEN 1 ELSE 0 END AS ZONKEY_EXISTS",
                new AccessSqlDialect().FormatExistsQuery("Animal", ""));
        }

        [Fact]
        public void Access_FormatParameterName()
        {
            var d = new AccessSqlDialect();
            Assert.Equal("?", d.FormatParameterName("Name", CommandType.Text));
            Assert.Equal("?", d.FormatParameterName(0, CommandType.Text));
        }

        [Fact]
        public void Access_FormatFieldName()
        {
            var d = new AccessSqlDialect();
            Assert.Equal("[Name]", d.FormatFieldName("Name", null));
            Assert.Equal("Name", d.FormatFieldName("Name", false));
        }
    }
}
