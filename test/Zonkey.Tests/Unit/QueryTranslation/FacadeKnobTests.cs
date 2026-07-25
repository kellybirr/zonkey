using System;
using System.Collections;
using System.Linq.Expressions;
using Xunit;
using Zonkey.Dialects;
using Zonkey.Extensions;
using Zonkey.ObjectModel;
using Zonkey.Tests.Models;

namespace Zonkey.Tests.Unit.QueryTranslation
{
    /// <summary>
    /// Confirms that every knob exposed on the WhereExpressionParser facade
    /// (ParameterPrefix, ParameterIndexModifier, ParameterizeLiterals, UseTableWithFieldNames, NoLock,
    /// and the seeded-ArrayList overload) is actually forwarded into the SqlTextGenerator.
    /// </summary>
    public class FacadeKnobTests
    {
        [Fact]
        public void ParameterPrefixAndIndexModifier_AreForwarded()
        {
            var parser = new WhereExpressionParser<Animal>(new GenericSqlDialect())
            {
                ParameterPrefix = '#',
                ParameterIndexModifier = 5
            };

            Expression<Func<Animal, bool>> expr = a => a.SpeciesId == 3 && a.Name == "x";
            var r = parser.Parse(expr);

            Assert.Equal("((SpeciesId = #5) AND (Name = #6))", r.SqlText);
            Assert.Equal(new object[] { 3, "x" }, r.Parameters);
        }

        [Fact]
        public void ParameterizeLiteralsFalse_InlinesValuesWithEmptyParameters()
        {
            var parser = new WhereExpressionParser<Animal>(new GenericSqlDialect())
            {
                ParameterizeLiterals = false
            };

            Expression<Func<Animal, bool>> expr = a => a.SpeciesId == 3;
            var r = parser.Parse(expr);

            Assert.Equal("(SpeciesId = 3)", r.SqlText);
            Assert.Empty(r.Parameters);
        }

        [Fact]
        public void SeededParameterList_ContinuesNumberingAndRetainsSeeds()
        {
            var seed = new ArrayList { "x", "y" };
            var parser = new WhereExpressionParser<Animal>(new GenericSqlDialect())
            {
                ParameterIndexModifier = 2
            };

            Expression<Func<Animal, bool>> expr = a => a.SpeciesId == 3;
            var r = parser.Parse(expr, seed);

            Assert.Equal("(SpeciesId = $4)", r.SqlText);
            Assert.Equal(new object[] { "x", "y", 3 }, r.Parameters);
        }

        [Fact]
        public void UseTableWithFieldNames_QualifiesColumnsForSingleMap()
        {
            var parser = new WhereExpressionParser<Animal>(new GenericSqlDialect())
            {
                UseTableWithFieldNames = true
            };

            Expression<Func<Animal, bool>> expr = a => a.SpeciesId == 3;
            var r = parser.Parse(expr);

            Assert.Equal("(Animal.SpeciesId = $0)", r.SqlText);
        }

        [Fact]
        public void NoLock_ThroughFacade_AppendsHintOnSubquery()
        {
            var parser = new WhereExpressionParser<Animal>(new SqlServerDialect())
            {
                NoLock = true
            };

            Expression<Func<Animal, bool>> expr = a => a.ExhibitId.SqlIn((Exhibit e) => e.ExhibitId, e => e.IsOpen);
            var r = parser.Parse(expr);

            Assert.Contains("WITH (NOLOCK)", r.SqlText);
        }
    }
}
