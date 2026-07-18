using System;
using System.Collections;
using System.Linq.Expressions;
using Xunit;
using Zonkey.Dialects;
using Zonkey.Extensions;
using Zonkey.ObjectModel;
using Zonkey.Tests.Models;

namespace Zonkey.Tests.Unit
{
    public class WhereExpressionParserTests
    {
        private SqlWhereClause Parse(Expression<Func<Animal, bool>> expr, SqlDialect dialect = null)
        {
            dialect ??= new GenericSqlDialect();
            var parser = new WhereExpressionParser<Animal>(dialect);
            return parser.Parse(expr);
        }

        private SqlWhereClause ParseExhibit(Expression<Func<Exhibit, bool>> expr, SqlDialect dialect = null)
        {
            dialect ??= new GenericSqlDialect();
            var parser = new WhereExpressionParser<Exhibit>(dialect);
            return parser.Parse(expr);
        }

        private SqlWhereClause ParseSpecies(Expression<Func<Species, bool>> expr, SqlDialect dialect = null)
        {
            dialect ??= new GenericSqlDialect();
            var parser = new WhereExpressionParser<Species>(dialect);
            return parser.Parse(expr);
        }

        // Basic comparisons

        [Fact]
        public void Equals_IntConstant()
        {
            var result = Parse(a => a.SpeciesId == 1);
            Assert.Contains("SpeciesId", result.SqlText);
            Assert.Contains("=", result.SqlText);
        }

        [Fact]
        public void NotEquals_IntConstant()
        {
            var result = Parse(a => a.SpeciesId != 1);
            Assert.Contains("!=", result.SqlText);
        }

        [Fact]
        public void GreaterThan()
        {
            var result = Parse(a => a.SpeciesId > 1);
            Assert.Contains(">", result.SqlText);
        }

        [Fact]
        public void LessThan()
        {
            var result = Parse(a => a.SpeciesId < 5);
            Assert.Contains("<", result.SqlText);
        }

        [Fact]
        public void GreaterThanOrEqual()
        {
            var result = Parse(a => a.SpeciesId >= 2);
            Assert.Contains(">=", result.SqlText);
        }

        [Fact]
        public void LessThanOrEqual()
        {
            var result = Parse(a => a.SpeciesId <= 3);
            Assert.Contains("<=", result.SqlText);
        }

        [Fact]
        public void Equals_StringConstant()
        {
            var result = Parse(a => a.Name == "Mei Mei");
            Assert.Contains("Name", result.SqlText);
        }

        // Null comparisons

        [Fact]
        public void EqualsNull_GeneratesIsNull()
        {
            var result = Parse(a => a.ExhibitId == null);
            Assert.Contains("IS NULL", result.SqlText);
        }

        [Fact]
        public void NotEqualsNull_GeneratesIsNotNull()
        {
            var result = Parse(a => a.ExhibitId != null);
            Assert.Contains("IS NOT NULL", result.SqlText);
        }

        // Boolean fields

        [Fact]
        public void BooleanField_TrueExpression()
        {
            var result = ParseSpecies(s => s.IsEndangered);
            Assert.NotNull(result.SqlText);
        }

        [Fact]
        public void BooleanField_NegatedExpression()
        {
            var result = ParseSpecies(s => !s.IsEndangered);
            Assert.NotNull(result.SqlText);
        }

        // Logical operators

        [Fact]
        public void And_CombinesTwoConditions()
        {
            var result = Parse(a => a.SpeciesId == 1 && a.Name == "Mei Mei");
            Assert.Contains("AND", result.SqlText);
        }

        [Fact]
        public void Or_CombinesTwoConditions()
        {
            var result = Parse(a => a.SpeciesId == 1 || a.SpeciesId == 2);
            Assert.Contains("OR", result.SqlText);
        }

        [Fact]
        public void NestedLogical_HasParentheses()
        {
            var result = Parse(a => (a.SpeciesId == 1 || a.SpeciesId == 2) && a.Name == "Test");
            Assert.Contains("(", result.SqlText);
            Assert.Contains(")", result.SqlText);
        }

        // SqlIn

        [Fact]
        public void SqlIn_IntArray()
        {
            var ids = new[] { 1, 2, 3 };
            var result = Parse(a => a.SpeciesId.SqlInInt(ids));
            Assert.Contains("IN", result.SqlText);
        }

        [Fact]
        public void SqlIn_GuidArray()
        {
            var guids = new[] { Guid.NewGuid(), Guid.NewGuid() };
            var result = Parse(a => a.ZookeeperId.SqlInGuid(guids));
            Assert.Contains("IN", result.SqlText);
        }

        [Fact]
        public void SqlIn_EmptyArray_ThrowsArgumentException()
        {
            var empty = Array.Empty<int>();
            Assert.Throws<ArgumentException>(() => Parse(a => a.SpeciesId.SqlInInt(empty)));
        }

        // String methods

        [Fact]
        public void Contains_GeneratesLike()
        {
            var result = Parse(a => a.Name.Contains("Mei"), new SqlServerDialect());
            Assert.Contains("LIKE", result.SqlText);
        }

        [Fact]
        public void StartsWith_GeneratesLike()
        {
            var result = Parse(a => a.Name.StartsWith("Mei"), new SqlServerDialect());
            Assert.Contains("LIKE", result.SqlText);
        }

        [Fact]
        public void EndsWith_GeneratesLike()
        {
            var result = Parse(a => a.Name.EndsWith("Mei"), new SqlServerDialect());
            Assert.Contains("LIKE", result.SqlText);
        }

        // Parameterization

        [Fact]
        public void Parameterization_CreatesParameters()
        {
            var parser = new WhereExpressionParser<Animal>(new GenericSqlDialect())
            {
                ParameterizeLiterals = true
            };
            var paramList = new ArrayList();
            Expression<Func<Animal, bool>> expr = a => a.SpeciesId == 1;
            var result = parser.Parse(expr, paramList);
            Assert.NotEmpty(paramList);
        }

        // Dialect-specific output

        [Fact]
        public void SqlServer_OutputContainsFieldName()
        {
            var result = Parse(a => a.SpeciesId == 1, new SqlServerDialect());
            Assert.Contains("SpeciesId", result.SqlText);
        }

        // Decimal comparisons

        [Fact]
        public void Decimal_GreaterThan()
        {
            var result = Parse(a => a.Weight > 5.0m);
            Assert.Contains(">", result.SqlText);
        }

        // Arithmetic

        [Fact]
        public void Arithmetic_InExpression()
        {
            var result = ParseExhibit(e => e.Capacity + 5 > 10);
            Assert.Contains("+", result.SqlText);
        }
    }
}
