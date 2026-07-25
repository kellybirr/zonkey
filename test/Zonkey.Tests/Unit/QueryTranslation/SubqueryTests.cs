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
    }
}
