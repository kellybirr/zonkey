using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using Xunit;
using Zonkey;
using Zonkey.Dialects;
using Zonkey.Extensions;
using Zonkey.ObjectModel;
using Zonkey.ObjectModel.QueryTranslation;
using Zonkey.Tests.Models;

namespace Zonkey.Tests.Unit.QueryTranslation
{
    public class SubqueryTests
    {
        private static SqlWhereClause T(Expression<Func<Animal, bool>> e, SqlDialect d = null)
            => TranslationTestHelper.Translate(e, d);

        [Fact]
        public void ThreeArgSqlIn_GeneratesSubquery()
        {
            var r = T(a => a.ExhibitId.SqlIn((Exhibit e) => e.ExhibitId, e => e.IsOpen));
            Assert.Equal("(ExhibitId IN (SELECT ExhibitId FROM Exhibit WHERE (IsOpen = 1)))", r.SqlText);
            Assert.Empty(r.Parameters);
        }

        [Fact]
        public void TwoArgSqlIn_UsesOuterMemberName()
        {
            var r = T(a => a.ExhibitId.SqlIn((Exhibit e) => e.Capacity > 50));
            Assert.Equal("(ExhibitId IN (SELECT ExhibitId FROM Exhibit WHERE (Capacity > $0)))", r.SqlText);
            Assert.Equal(new object[] { 50 }, r.Parameters);
        }

        [Fact]
        public void SubqueryParameters_ShareNumberingWithOuterClause()
        {
            var r = T(a => a.SpeciesId == 9 && a.ExhibitId.SqlIn((Exhibit e) => e.ExhibitId, e => e.Capacity > 50));
            Assert.Equal("((SpeciesId = $0) AND (ExhibitId IN (SELECT ExhibitId FROM Exhibit WHERE (Capacity > $1))))", r.SqlText);
            Assert.Equal(new object[] { 9, 50 }, r.Parameters);
        }

        [Fact]
        public void SubqueryWhere_CanUseCapturedValues()
        {
            int min = 10;
            var r = T(a => a.ExhibitId.SqlIn((Exhibit e) => e.ExhibitId, e => e.Capacity >= min));
            Assert.Equal(new object[] { 10 }, r.Parameters);
        }

        [Fact]
        public void SqlServer_NoLock_AppendsHint()
        {
            var maps = new Dictionary<string, DataMap> { { "a", DataMap.GenerateCached(typeof(Animal)) } };
            Expression<Func<Animal, bool>> expr = a => a.ExhibitId.SqlIn((Exhibit e) => e.ExhibitId, e => e.IsOpen);
            var body = PartialEvaluator.Reduce(expr.Body);
            var translator = new ExpressionTranslator(maps, new SqlServerDialect());
            var gen = new SqlTextGenerator(new SqlServerDialect()) { NoLock = true };
            var r = gen.Generate(translator.TranslatePredicate(body));
            Assert.Contains("WITH (NOLOCK)", r.SqlText);
        }

        [Fact]
        public void TwoArgSqlIn_FieldMissingOnTarget_Throws()
        {
            Assert.ThrowsAny<ArgumentException>(() => T(a => a.ZookeeperId.SqlIn((Exhibit e) => e.IsOpen)));
        }

        [Fact]
        public void TwoArgSqlIn_ResolvesRenamedFieldThroughMap()
        {
            var r = T(a => a.SpeciesId.SqlIn((RenamedFieldTarget t) => t.Active));
            Assert.Equal("(SpeciesId IN (SELECT record_id FROM RenamedFieldTarget WHERE (is_active = 1)))", r.SqlText);
        }

        [Fact]
        public void ThreeArgSqlIn_NonPropertySelector_ThrowsDiagnostic()
        {
            var ex = Assert.Throws<SqlExpressionException>(
                () => T(a => a.ExhibitId.SqlIn((Exhibit e) => e.Capacity + 1, e => e.IsOpen)));
            Assert.Contains("field selector", ex.Message);
        }

        // T9: subquery completion.

        [Fact]
        public void SqlServer_UseQuotedIdentifier_BracketsSubqueryFieldAndTable()
        {
            // Verifies that WhereExpressionParser.UseQuotedIdentifier is actually forwarded into the
            // SqlTextGenerator used for the subquery branch (VisitInSubquery), not just the outer one.
            var parser = new WhereExpressionParser<Animal>(new SqlServerDialect()) { UseQuotedIdentifier = true };
            Expression<Func<Animal, bool>> expr = a => a.ExhibitId.SqlIn((Exhibit e) => e.ExhibitId, e => e.IsOpen);
            var r = parser.Parse(expr);
            Assert.Equal("([ExhibitId] IN (SELECT [ExhibitId] FROM [Exhibit] WHERE ([IsOpen] = 1)))", r.SqlText);
        }

        [Fact]
        public void SubqueryWhere_NullComparison_RendersIsNull()
        {
            var r = T(a => a.ExhibitId.SqlIn((Exhibit e) => e.ExhibitId, e => e.Location == null));
            Assert.Equal("(ExhibitId IN (SELECT ExhibitId FROM Exhibit WHERE (Location IS NULL)))", r.SqlText);
        }

        [Fact]
        public void SubqueryWhere_NestedLogicals_Translate()
        {
            var r = T(a => a.ExhibitId.SqlIn((Exhibit e) => e.ExhibitId,
                e => e.IsOpen && (e.Capacity > 10 || e.Capacity < 2)));
            Assert.Equal(
                "(ExhibitId IN (SELECT ExhibitId FROM Exhibit WHERE ((IsOpen = 1) AND ((Capacity > $0) OR (Capacity < $1)))))",
                r.SqlText);
            Assert.Equal(new object[] { 10, 2 }, r.Parameters);
        }

        [Fact]
        public void ThreeArgSqlIn_BoolFieldSelector_RendersAsSelectColumnNotPredicate()
        {
            // The field selector must be a plain property access - IsOpen is a bool property, and
            // even though bool properties render as "(IsOpen = 1)" in predicate position, the field
            // selector path only ever extracts the mapped column name, never wraps it as a predicate.
            var r = TranslationTestHelper.Translate<Species>(
                s => s.IsEndangered.SqlIn((Exhibit e) => e.IsOpen, e => e.Capacity > 10));
            Assert.Equal("(IsEndangered IN (SELECT IsOpen FROM Exhibit WHERE (Capacity > $0)))", r.SqlText);
        }
    }
}
