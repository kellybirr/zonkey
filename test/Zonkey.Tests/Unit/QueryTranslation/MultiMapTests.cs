using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using Xunit;
using Zonkey.Dialects;
using Zonkey.ObjectModel;
using Zonkey.Tests.Models;

namespace Zonkey.Tests.Unit.QueryTranslation
{
    // T8: multi-parameter lambda translation through the internal, non-generic WhereExpressionParser
    // ctor. Passing more than one DataMap hint makes the parser auto-qualify columns with their
    // table name (maps.Count > 1), unless UseTableWithFieldNames is explicitly overridden.
    public class MultiMapTests
    {
        private static IEnumerable<DataMap> Maps() => new[]
        {
            DataMap.GenerateCached(typeof(Animal)),
            DataMap.GenerateCached(typeof(Exhibit))
        };

        [Fact]
        public void TwoParameterLambda_AutoQualifiesWithTableNames()
        {
            var parser = new WhereExpressionParser(Maps(), new GenericSqlDialect());
            Expression<Func<Animal, Exhibit, bool>> expr = (a, e) => a.ExhibitId == e.ExhibitId && e.IsOpen;

            var r = parser.Parse(expr);

            Assert.Equal("((Animal.ExhibitId = Exhibit.ExhibitId) AND (Exhibit.IsOpen = 1))", r.SqlText);
            Assert.Empty(r.Parameters);
        }

        [Fact]
        public void TwoParameterLambda_UseTableWithFieldNamesFalse_DisablesQualification()
        {
            var parser = new WhereExpressionParser(Maps(), new GenericSqlDialect())
            {
                UseTableWithFieldNames = false
            };
            Expression<Func<Animal, Exhibit, bool>> expr = (a, e) => a.ExhibitId == e.ExhibitId && e.IsOpen;

            var r = parser.Parse(expr);

            Assert.Equal("((ExhibitId = ExhibitId) AND (IsOpen = 1))", r.SqlText);
        }
    }
}
