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
            Assert.Equal("(SpeciesId = $0)", result.SqlText);
            Assert.Equal(new object[] { 1 }, result.Parameters);
        }

        [Fact]
        public void NotEquals_IntConstant()
        {
            var result = Parse(a => a.SpeciesId != 1);
            Assert.Equal("(SpeciesId != $0)", result.SqlText);
            Assert.Equal(new object[] { 1 }, result.Parameters);
        }

        [Fact]
        public void GreaterThan()
        {
            var result = Parse(a => a.SpeciesId > 1);
            Assert.Equal("(SpeciesId > $0)", result.SqlText);
            Assert.Equal(new object[] { 1 }, result.Parameters);
        }

        [Fact]
        public void LessThan()
        {
            var result = Parse(a => a.SpeciesId < 5);
            Assert.Equal("(SpeciesId < $0)", result.SqlText);
            Assert.Equal(new object[] { 5 }, result.Parameters);
        }

        [Fact]
        public void GreaterThanOrEqual()
        {
            var result = Parse(a => a.SpeciesId >= 2);
            Assert.Equal("(SpeciesId >= $0)", result.SqlText);
            Assert.Equal(new object[] { 2 }, result.Parameters);
        }

        [Fact]
        public void LessThanOrEqual()
        {
            var result = Parse(a => a.SpeciesId <= 3);
            Assert.Equal("(SpeciesId <= $0)", result.SqlText);
            Assert.Equal(new object[] { 3 }, result.Parameters);
        }

        [Fact]
        public void Equals_StringConstant()
        {
            var result = Parse(a => a.Name == "Mei Mei");
            Assert.Equal("(Name = $0)", result.SqlText);
            Assert.Equal(new object[] { "Mei Mei" }, result.Parameters);
        }

        // Null comparisons

        [Fact]
        public void EqualsNull_GeneratesIsNull()
        {
            var result = Parse(a => a.ExhibitId == null);
            Assert.Equal("(ExhibitId IS NULL)", result.SqlText);
            Assert.Empty(result.Parameters);
        }

        [Fact]
        public void NotEqualsNull_GeneratesIsNotNull()
        {
            var result = Parse(a => a.ExhibitId != null);
            Assert.Equal("(ExhibitId IS NOT NULL)", result.SqlText);
            Assert.Empty(result.Parameters);
        }

        // Boolean fields

        [Fact]
        public void BooleanField_TrueExpression()
        {
            var result = ParseSpecies(s => s.IsEndangered);
            Assert.Equal("(IsEndangered = 1)", result.SqlText);
            Assert.Empty(result.Parameters);
        }

        [Fact]
        public void BooleanField_NegatedExpression()
        {
            var result = ParseSpecies(s => !s.IsEndangered);
            Assert.Equal("(NOT (IsEndangered = 1))", result.SqlText);
            Assert.Empty(result.Parameters);
        }

        // Logical operators

        [Fact]
        public void And_CombinesTwoConditions()
        {
            var result = Parse(a => a.SpeciesId == 1 && a.Name == "Mei Mei");
            Assert.Equal("((SpeciesId = $0) AND (Name = $1))", result.SqlText);
            Assert.Equal(new object[] { 1, "Mei Mei" }, result.Parameters);
        }

        [Fact]
        public void Or_CombinesTwoConditions()
        {
            var result = Parse(a => a.SpeciesId == 1 || a.SpeciesId == 2);
            Assert.Equal("((SpeciesId = $0) OR (SpeciesId = $1))", result.SqlText);
            Assert.Equal(new object[] { 1, 2 }, result.Parameters);
        }

        [Fact]
        public void NestedLogical_HasParentheses()
        {
            var result = Parse(a => (a.SpeciesId == 1 || a.SpeciesId == 2) && a.Name == "Test");
            Assert.Equal("(((SpeciesId = $0) OR (SpeciesId = $1)) AND (Name = $2))", result.SqlText);
            Assert.Equal(new object[] { 1, 2, "Test" }, result.Parameters);
        }

        // SqlIn

        [Fact]
        public void SqlIn_IntArray()
        {
            var ids = new[] { 1, 2, 3 };
#pragma warning disable 618
            var result = Parse(a => a.SpeciesId.SqlInInt(ids));
#pragma warning restore 618
            Assert.Equal("(SpeciesId IN (1,2,3))", result.SqlText);
            Assert.Empty(result.Parameters);
        }

        [Fact]
        public void SqlIn_GuidArray()
        {
            var guids = new[] { Guid.NewGuid(), Guid.NewGuid() };
#pragma warning disable 618
            var result = Parse(a => a.ZookeeperId.SqlInGuid(guids));
#pragma warning restore 618
            Assert.Equal($"(ZookeeperId IN ('{guids[0]}','{guids[1]}'))", result.SqlText);
            Assert.Empty(result.Parameters);
        }

        [Fact]
        public void SqlIn_EmptyArray_ThrowsArgumentException()
        {
            var empty = Array.Empty<int>();
#pragma warning disable 618
            Assert.Throws<ArgumentException>(() => Parse(a => a.SpeciesId.SqlInInt(empty)));
#pragma warning restore 618
        }

        // String methods

        [Fact]
        public void Contains_GeneratesLike()
        {
            var result = Parse(a => a.Name.Contains("Mei"), new SqlServerDialect());
            Assert.Equal("([Name] LIKE $0)", result.SqlText);
            Assert.Equal(new object[] { "%Mei%" }, result.Parameters);
        }

        [Fact]
        public void StartsWith_GeneratesLike()
        {
            var result = Parse(a => a.Name.StartsWith("Mei"), new SqlServerDialect());
            Assert.Equal("([Name] LIKE $0)", result.SqlText);
            Assert.Equal(new object[] { "Mei%" }, result.Parameters);
        }

        [Fact]
        public void EndsWith_GeneratesLike()
        {
            var result = Parse(a => a.Name.EndsWith("Mei"), new SqlServerDialect());
            Assert.Equal("([Name] LIKE $0)", result.SqlText);
            Assert.Equal(new object[] { "%Mei" }, result.Parameters);
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
            Assert.Equal("(SpeciesId = $0)", result.SqlText);
            Assert.Equal(new object[] { 1 }, paramList.ToArray());
            Assert.Equal(new object[] { 1 }, result.Parameters);
        }

        // Dialect-specific output

        [Fact]
        public void SqlServer_OutputContainsFieldName()
        {
            var result = Parse(a => a.SpeciesId == 1, new SqlServerDialect());
            Assert.Equal("([SpeciesId] = $0)", result.SqlText);
            Assert.Equal(new object[] { 1 }, result.Parameters);
        }

        // Decimal comparisons

        [Fact]
        public void Decimal_GreaterThan()
        {
            var result = Parse(a => a.Weight > 5.0m);
            Assert.Equal("(Weight > $0)", result.SqlText);
            Assert.Equal(new object[] { 5.0m }, result.Parameters);
        }

        // Arithmetic

        [Fact]
        public void Arithmetic_InExpression()
        {
            var result = ParseExhibit(e => e.Capacity + 5 > 10);
            Assert.Equal("((Capacity + $0) > $1)", result.SqlText);
            Assert.Equal(new object[] { 5, 10 }, result.Parameters);
        }
    }
}
