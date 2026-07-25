using System;
using System.Linq.Expressions;
using System.Text.RegularExpressions;
using Xunit;
using Zonkey.Dialects;
using Zonkey.ObjectModel;
using Zonkey.ObjectModel.QueryTranslation;
using Zonkey.Tests.Models;

namespace Zonkey.Tests.Unit.QueryTranslation
{
    public class RegexTranslationTests
    {
        private static SqlWhereClause T(Expression<Func<Animal, bool>> e, SqlDialect d)
            => TranslationTestHelper.Translate(e, d);

        [Fact]
        public void IsMatch_OnPostgres_RendersTilde()
        {
            var r = T(a => Regex.IsMatch(a.Name, "^Mei"), new PostgreSqlDialect());
            Assert.Equal("(Name ~ $0)", r.SqlText);
            Assert.Equal(new object[] { "^Mei" }, r.Parameters);
        }

        [Fact]
        public void IsMatch_IgnoreCase_RendersTildeStar()
        {
            var r = T(a => Regex.IsMatch(a.Name, "^mei", RegexOptions.IgnoreCase), new PostgreSqlDialect());
            Assert.Equal("(Name ~* $0)", r.SqlText);
        }

        [Fact]
        public void IsMatch_OtherOptions_Throw()
        {
            Assert.Throws<SqlExpressionException>(
                () => T(a => Regex.IsMatch(a.Name, "^m", RegexOptions.Multiline), new PostgreSqlDialect()));
        }

        [Fact]
        public void IsMatch_OnNonPostgres_ThrowsNamingTheLimitation()
        {
            var ex = Assert.Throws<SqlExpressionException>(
                () => T(a => Regex.IsMatch(a.Name, "^Mei"), new SqlServerDialect()));
            Assert.Contains("PostgreSql", ex.Message);
        }

        [Fact]
        public void ParameterIndependentIsMatch_FoldsClientSide()
        {
            string s = "abc";
            var r = T(a => a.SpeciesId == 1 && Regex.IsMatch(s, "b"), new PostgreSqlDialect());
            // Regex.IsMatch(s, "b") depends on no query-parameter values, so PartialEvaluator folds
            // it client-side to the constant `true`, collapsing the AND to just the left operand.
            Assert.Equal("((SpeciesId = $0) AND 1 = 1)", r.SqlText);
            Assert.Equal(new object[] { 1 }, r.Parameters);
        }
    }
}
