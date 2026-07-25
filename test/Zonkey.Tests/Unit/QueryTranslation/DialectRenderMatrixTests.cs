using System;
using System.Linq.Expressions;
using Xunit;
using Zonkey;
using Zonkey.Dialects;
using Zonkey.ObjectModel;
using Zonkey.ObjectModel.QueryTranslation;
using Zonkey.Tests.Models;

namespace Zonkey.Tests.Unit.QueryTranslation
{
    // Golden matrix of RenderFunction output across all dialects, covering the Wave B
    // review fixes: MySql CHAR_LENGTH, PostgreSql ROUND2-as-numeric, Oracle/DB2 SUBSTR
    // and INDEXOF spellings, and Access date-part / TRIM / COALESCE / CASE_WHEN gaps.
    public class DialectRenderMatrixTests
    {
        private static SqlWhereClause T(Expression<Func<Animal, bool>> e, SqlDialect d)
            => TranslationTestHelper.Translate(e, d);

        [Fact]
        public void Length_PerDialect()
        {
            // MySql: LENGTH() is BYTE length, must use CHAR_LENGTH() instead. MySqlDialect
            // only quotes fields when explicitly requested, so unquoted here.
            Assert.Equal("(CHAR_LENGTH(Name) > $0)",
                T(a => a.Name.Length > 3, new MySqlDialect()).SqlText);

            // SqlServer quotes fields by default ([Name]) and uses LEN().
            Assert.Equal("(LEN([Name]) > $0)",
                T(a => a.Name.Length > 3, new SqlServerDialect()).SqlText);

            // ANSI-family dialects (Postgres/Oracle/DB2 inherit AnsiSqlDialect, unquoted by default)
            // fall through to base LENGTH().
            Assert.Equal("(LENGTH(Name) > $0)",
                T(a => a.Name.Length > 3, new PostgreSqlDialect()).SqlText);
            Assert.Equal("(LENGTH(Name) > $0)",
                T(a => a.Name.Length > 3, new OracleSqlDialect()).SqlText);
            Assert.Equal("(LENGTH(Name) > $0)",
                T(a => a.Name.Length > 3, new DB2SqlDialect()).SqlText);

            // Access quotes fields by default ([Name]) and uses Len().
            Assert.Equal("(Len([Name]) > $0)",
                T(a => a.Name.Length > 3, new AccessSqlDialect()).SqlText);
        }

        [Fact]
        public void Substring_BothOverloads_PerDialect()
        {
            // Oracle -> SUBSTR
            Assert.Equal("(SUBSTR(Name, $0, $1) = $2)",
                T(a => a.Name.Substring(0, 3) == "Mei", new OracleSqlDialect()).SqlText);
            Assert.Equal("(SUBSTR(Name, $0) = $1)",
                T(a => a.Name.Substring(2) == "i", new OracleSqlDialect()).SqlText);

            // DB2 -> SUBSTR
            Assert.Equal("(SUBSTR(Name, $0, $1) = $2)",
                T(a => a.Name.Substring(0, 3) == "Mei", new DB2SqlDialect()).SqlText);
            Assert.Equal("(SUBSTR(Name, $0) = $1)",
                T(a => a.Name.Substring(2) == "i", new DB2SqlDialect()).SqlText);

            // Access -> Mid, fields quoted by default
            Assert.Equal("(Mid([Name], $0, $1) = $2)",
                T(a => a.Name.Substring(0, 3) == "Mei", new AccessSqlDialect()).SqlText);
            Assert.Equal("(Mid([Name], $0) = $1)",
                T(a => a.Name.Substring(2) == "i", new AccessSqlDialect()).SqlText);

            // MySql -> SUBSTRING(x, start, len) / SUBSTRING(x, start)
            Assert.Equal("(SUBSTRING(Name, $0, $1) = $2)",
                T(a => a.Name.Substring(0, 3) == "Mei", new MySqlDialect()).SqlText);
            Assert.Equal("(SUBSTRING(Name, $0) = $1)",
                T(a => a.Name.Substring(2) == "i", new MySqlDialect()).SqlText);

            // SqlServer -> SUBSTRING(x, start, len) / SUBSTRING(x, start, MAXINT) (no 2-arg overload)
            Assert.Equal("(SUBSTRING([Name], $0, $1) = $2)",
                T(a => a.Name.Substring(0, 3) == "Mei", new SqlServerDialect()).SqlText);
            Assert.Equal("(SUBSTRING([Name], $0, 2147483647) = $1)",
                T(a => a.Name.Substring(2) == "i", new SqlServerDialect()).SqlText);
        }

        [Fact]
        public void IndexOf_PerDialect()
        {
            // Oracle -> INSTR(haystack, needle) - 1
            Assert.Equal("((INSTR(Name, $0) - 1) = $1)",
                T(a => a.Name.IndexOf("Mei") == 0, new OracleSqlDialect()).SqlText);

            // DB2 -> LOCATE(needle, haystack) - 1
            Assert.Equal("((LOCATE($0, Name) - 1) = $1)",
                T(a => a.Name.IndexOf("Mei") == 0, new DB2SqlDialect()).SqlText);

            // MySql -> LOCATE(needle, haystack) - 1
            Assert.Equal("((LOCATE($0, Name) - 1) = $1)",
                T(a => a.Name.IndexOf("Mei") == 0, new MySqlDialect()).SqlText);

            // Access -> InStr(haystack, needle) - 1, fields quoted by default
            Assert.Equal("((InStr([Name], $0) - 1) = $1)",
                T(a => a.Name.IndexOf("Mei") == 0, new AccessSqlDialect()).SqlText);

            // SqlServer -> CHARINDEX(needle, haystack) - 1
            Assert.Equal("((CHARINDEX($0, [Name]) - 1) = $1)",
                T(a => a.Name.IndexOf("Mei") == 0, new SqlServerDialect()).SqlText);

            // ANSI fallback (Postgres) -> POSITION(needle IN haystack) - 1
            Assert.Equal("((POSITION($0 IN Name) - 1) = $1)",
                T(a => a.Name.IndexOf("Mei") == 0, new PostgreSqlDialect()).SqlText);
        }

        [Fact]
        public void Round2_PostgreSql_CastsToNumeric()
        {
            // Postgres has no round(double precision, int) overload, only round(numeric, int).
            Assert.Equal("(ROUND(CAST(Weight AS numeric), $0) = $1)",
                T(a => Math.Round(a.Weight.Value, 1) == 5.5m, new PostgreSqlDialect()).SqlText);
        }

        [Fact]
        public void Round1_PostgreSql_UnaffectedByOverride()
        {
            // Single-arg ROUND1 is unaffected by the ROUND2 override and falls through to base.
            Assert.Equal("(ROUND(Weight) = $0)",
                T(a => Math.Round(a.Weight.Value) == 5m, new PostgreSqlDialect()).SqlText);
        }

        [Fact]
        public void DateParts_Access_UseDatePart()
        {
            Assert.Equal("(DatePart('yyyy', [DateOfBirth]) = $0)",
                T(a => a.DateOfBirth.Value.Year == 2020, new AccessSqlDialect()).SqlText);
            Assert.Equal("(DatePart('m', [DateOfBirth]) = $0)",
                T(a => a.DateOfBirth.Value.Month == 6, new AccessSqlDialect()).SqlText);
            Assert.Equal("(DatePart('d', [DateOfBirth]) = $0)",
                T(a => a.DateOfBirth.Value.Day == 15, new AccessSqlDialect()).SqlText);
        }

        [Fact]
        public void DateProperty_Access_UsesDateValue()
        {
            Assert.Equal("(DateValue([DateOfBirth]) = $0)",
                T(a => a.DateOfBirth.Value.Date == new DateTime(2020, 6, 15), new AccessSqlDialect()).SqlText);
        }

        [Fact]
        public void DateParts_Oracle_FallsThroughToAnsiExtract()
        {
            // Oracle only overrides SUBSTRING/INDEXOF/CEILING; EXTRACT is valid Oracle syntax
            // and comes from the base ANSI implementation.
            Assert.Equal("(EXTRACT(YEAR FROM DateOfBirth) = $0)",
                T(a => a.DateOfBirth.Value.Year == 2020, new OracleSqlDialect()).SqlText);
        }

        [Fact]
        public void Trim_Access_UsesTrimFunction()
        {
            Assert.Equal("(Trim([Name]) = $0)",
                T(a => a.Name.Trim() == "x", new AccessSqlDialect()).SqlText);
        }

        [Fact]
        public void Coalesce_Access_UsesIIf()
        {
            Assert.Equal("(IIf([Notes] IS NULL, $0, [Notes]) = $1)",
                T(a => (a.Notes ?? "none") == "x", new AccessSqlDialect()).SqlText);
        }

        [Fact]
        public void CaseWhen_Access_UsesIIf()
        {
            Assert.Equal("(IIf(([ExhibitId] IS NULL), $0, $1) = $2)",
                T(a => (a.ExhibitId == null ? 0 : 1) == 1, new AccessSqlDialect()).SqlText);
        }

        [Fact]
        public void Ceiling_Oracle_UsesCeil()
        {
            Assert.Equal("(CEIL(Weight) = $0)",
                T(a => Math.Ceiling(a.Weight.Value) == 6m, new OracleSqlDialect()).SqlText);
        }
    }
}
