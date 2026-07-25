using System;
using System.Linq.Expressions;
using Xunit;
using Zonkey;
using Zonkey.Dialects;
using Zonkey.ObjectModel;
using Zonkey.ObjectModel.QueryTranslation;
using Zonkey.Tests.Models;
using Zonkey.Extensions;

namespace Zonkey.Tests.Unit.QueryTranslation
{
    public class LikeAndCaseTests
    {
        private static SqlWhereClause T(Expression<Func<Animal, bool>> e, SqlDialect d = null)
            => TranslationTestHelper.Translate(e, d);

        [Fact]
        public void StartsWith_EndsWith_Contains_ApplyWildcards()
        {
            var r1 = T(a => a.Name.StartsWith("Mei"));
            Assert.Equal("(Name LIKE $0)", r1.SqlText);
            Assert.Equal("Mei%", r1.Parameters[0]);

            var r2 = T(a => a.Name.EndsWith("Mei"));
            Assert.Equal("%Mei", r2.Parameters[0]);

            var r3 = T(a => a.Name.Contains("Mei"));
            Assert.Equal("%Mei%", r3.Parameters[0]);
        }

        [Fact]
        public void WildcardsInValue_AreEscaped()
        {
            var r = T(a => a.Name.Contains("50%_off"));
            Assert.Equal("(Name LIKE $0 ESCAPE '\\')", r.SqlText);
            Assert.Equal("%50\\%\\_off%", r.Parameters[0]);
        }

        [Fact]
        public void WildcardsInValue_MySql_EscapeClauseUsesDoubledBackslash()
        {
            var r = T(a => a.Name.Contains("50%_off"), new MySqlDialect());
            Assert.Equal(@"(Name LIKE $0 ESCAPE '\\')", r.SqlText);
            Assert.Equal(@"%50\%\_off%", r.Parameters[0]);
        }

        [Fact]
        public void IgnoreCaseComparison_UsesUpperOnGenericDialect()
        {
            var r = T(a => a.Name.StartsWith("mei", StringComparison.OrdinalIgnoreCase));
            Assert.Equal("(UPPER(Name) LIKE UPPER($0))", r.SqlText);
            Assert.Equal("mei%", r.Parameters[0]);
        }

        [Fact]
        public void IgnoreCaseComparison_UsesIlikeOnPostgres()
        {
            var r = T(a => a.Name.StartsWith("mei", StringComparison.OrdinalIgnoreCase), new PostgreSqlDialect());
            Assert.Equal("(Name ILIKE $0)", r.SqlText);
        }

        [Fact]
        public void CaseSensitiveComparisonValue_BehavesLikePlainOverload()
        {
            var r = T(a => a.Name.StartsWith("Mei", StringComparison.Ordinal));
            Assert.Equal("(Name LIKE $0)", r.SqlText);
        }

        [Fact]
        public void EqualsIgnoreCase_UsesUpperEquality()
        {
            var r = T(a => a.Name.Equals("mei mei", StringComparison.OrdinalIgnoreCase));
            Assert.Equal("(UPPER(Name) = UPPER($0))", r.SqlText);
        }

        [Fact]
        public void SqlLike_PassesPatternRaw()
        {
            var r = T(a => a.Name.SqlLike("M__ M%"));
            Assert.Equal("(Name LIKE $0)", r.SqlText);
            Assert.Equal("M__ M%", r.Parameters[0]);
        }

        [Fact]
        public void SqlILike_IsCaseInsensitive()
        {
            Assert.Equal("(UPPER(Name) LIKE UPPER($0))", T(a => a.Name.SqlILike("m%")).SqlText);
            Assert.Equal("(Name ILIKE $0)", T(a => a.Name.SqlILike("m%"), new PostgreSqlDialect()).SqlText);
        }

        [Fact]
        public void NegatedContains_WrapsWithNot()
        {
            Assert.Equal("(NOT (Name LIKE $0))", T(a => !a.Name.Contains("Mei")).SqlText);
        }

        [Fact]
        public void NonConstantPattern_UsesConcat()
        {
            // pattern references the entity => cannot escape, wildcards concatenated in SQL
            Assert.Equal("(Name LIKE (Notes || $0))", T(a => a.Name.StartsWith(a.Notes)).SqlText);
        }

#if !NETFRAMEWORK
        [Fact]
        public void CharOverloads_TranslateLikeSingleCharString()
        {
            var r1 = T(a => a.Name.StartsWith('M'));
            Assert.Equal("(Name LIKE $0)", r1.SqlText);
            Assert.Equal("M%", r1.Parameters[0]);
            var r2 = T(a => a.Name.Contains('%'));
            Assert.Equal("(Name LIKE $0 ESCAPE '\\')", r2.SqlText);
            Assert.Equal("%\\%%", r2.Parameters[0]);
            var r3 = T(a => a.Name.EndsWith('i'));
            Assert.Equal("%i", r3.Parameters[0]);
        }
#endif
    }
}
