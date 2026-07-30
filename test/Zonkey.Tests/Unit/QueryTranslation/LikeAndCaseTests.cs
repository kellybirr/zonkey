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

        // T3: empty-pattern LIKE overloads. An empty search string has nothing to escape, so the
        // pattern is pure wildcard(s) and no ESCAPE clause is emitted.
        [Fact]
        public void StartsWith_EmptyString_PatternIsJustWildcard()
        {
            var r = T(a => a.Name.StartsWith(""));
            Assert.Equal("(Name LIKE $0)", r.SqlText);
            Assert.Equal("%", r.Parameters[0]);
        }

        [Fact]
        public void EndsWith_EmptyString_PatternIsJustWildcard()
        {
            var r = T(a => a.Name.EndsWith(""));
            Assert.Equal("(Name LIKE $0)", r.SqlText);
            Assert.Equal("%", r.Parameters[0]);
        }

        [Fact]
        public void Contains_EmptyString_PatternIsDoubleWildcard()
        {
            var r = T(a => a.Name.Contains(""));
            Assert.Equal("(Name LIKE $0)", r.SqlText);
            Assert.Equal("%%", r.Parameters[0]);
        }

        [Fact]
        public void Contains_UnicodePattern_RoundTripsWithoutEscaping()
        {
            var r = T(a => a.Name.Contains("pâté🦓"));
            Assert.Equal("(Name LIKE $0)", r.SqlText);
            Assert.Equal("%pâté🦓%", r.Parameters[0]);
        }

        [Fact]
        public void Contains_BackslashInData_EscapesBackslashAndAddsEscapeClause()
        {
            var r = T(a => a.Name.Contains(@"a\b"));
            Assert.Equal("(Name LIKE $0 ESCAPE '\\')", r.SqlText);
            Assert.Equal(@"%a\\b%", r.Parameters[0]);
        }

        [Fact]
        public void Contains_AllMetacharsInData_EscapesEveryOne()
        {
            var r = T(a => a.Name.Contains(@"100%_a\[b"));
            Assert.Equal("(Name LIKE $0 ESCAPE '\\')", r.SqlText);
            Assert.Equal("%100\\%\\_a\\\\\\[b%", r.Parameters[0]);
        }

        // T4: negation matrix - StartsWith/EndsWith/SqlLike/SqlILike.
        [Fact]
        public void NegatedStartsWith_WrapsWithNot()
        {
            Assert.Equal("(NOT (Name LIKE $0))", T(a => !a.Name.StartsWith("Mei")).SqlText);
        }

        [Fact]
        public void NegatedEndsWith_WrapsWithNot()
        {
            Assert.Equal("(NOT (Name LIKE $0))", T(a => !a.Name.EndsWith("Mei")).SqlText);
        }

        [Fact]
        public void NegatedSqlLike_WrapsWithNot()
        {
            Assert.Equal("(NOT (Name LIKE $0))", T(a => !a.Name.SqlLike("M%")).SqlText);
        }

        [Fact]
        public void NegatedSqlILike_WrapsWithNot()
        {
            Assert.Equal("(NOT (UPPER(Name) LIKE UPPER($0)))", T(a => !a.Name.SqlILike("m%")).SqlText);
            Assert.Equal("(NOT (Name ILIKE $0))", T(a => !a.Name.SqlILike("m%"), new PostgreSqlDialect()).SqlText);
        }

        // T5: dynamic (entity-referencing) LIKE patterns render dialect-specific concatenation
        // since the pattern can't be pre-escaped client-side.
        [Fact]
        public void StartsWith_EntityPattern_PerDialect()
        {
            Assert.Equal("(Name LIKE (Notes || $0))", T(a => a.Name.StartsWith(a.Notes)).SqlText);
            Assert.Equal("([Name] LIKE ([Notes] + $0))", T(a => a.Name.StartsWith(a.Notes), new SqlServerDialect()).SqlText);
            Assert.Equal("(Name LIKE CONCAT(Notes, $0))", T(a => a.Name.StartsWith(a.Notes), new MySqlDialect()).SqlText);
            Assert.Equal("([Name] LIKE ([Notes] & $0))", T(a => a.Name.StartsWith(a.Notes), new AccessSqlDialect()).SqlText);
            Assert.Equal("([Name] LIKE ([Notes] || $0))", T(a => a.Name.StartsWith(a.Notes), new SqliteDialect()).SqlText);
        }

        [Fact]
        public void EndsWith_EntityPattern_NestedConcat_OnTwoDialects()
        {
            Assert.Equal("(Name LIKE ($0 || Notes))", T(a => a.Name.EndsWith(a.Notes)).SqlText);
            Assert.Equal("([Name] LIKE ($0 + [Notes]))", T(a => a.Name.EndsWith(a.Notes), new SqlServerDialect()).SqlText);
        }

        [Fact]
        public void Contains_EntityPattern_NestedConcat_OnTwoDialects()
        {
            Assert.Equal("(Name LIKE (($0 || Notes) || $1))", T(a => a.Name.Contains(a.Notes)).SqlText);
            Assert.Equal("([Name] LIKE (($0 + [Notes]) + $1))", T(a => a.Name.Contains(a.Notes), new SqlServerDialect()).SqlText);
        }
    }
}
