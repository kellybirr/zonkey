using System;
using System.Collections;
using Xunit;
using Zonkey.Dialects;
using Zonkey.ObjectModel;
using Zonkey.ObjectModel.QueryTranslation;
using Zonkey.Tests.Models;

namespace Zonkey.Tests.Unit.QueryTranslation
{
    public class SqlTextGeneratorTests
    {
        private static SqlColumn Col<T>(string propertyName)
        {
            var map = DataMap.GenerateCached(typeof(T));
            var field = map.GetFieldForProperty(typeof(T).GetProperty(propertyName));
            return new SqlColumn { Field = field, Map = map, IsBoolean = field.Property.PropertyType == typeof(bool) };
        }

        [Fact]
        public void Binary_Equal_EmitsParenthesizedComparisonAndParameter()
        {
            var node = new SqlBinary { Op = SqlBinaryOp.Equal, Left = Col<Animal>("SpeciesId"), Right = new SqlValue { Value = 1 } };
            var result = new SqlTextGenerator(new GenericSqlDialect()).Generate(node);
            Assert.Equal("(SpeciesId = $0)", result.SqlText);
            Assert.Equal(new object[] { 1 }, result.Parameters);
        }

        [Fact]
        public void Nested_AndOr_ParenthesizesStructurally()
        {
            var left = new SqlBinary { Op = SqlBinaryOp.Equal, Left = Col<Animal>("SpeciesId"), Right = new SqlValue { Value = 1 } };
            var right = new SqlBinary { Op = SqlBinaryOp.GreaterThan, Left = Col<Animal>("Weight"), Right = new SqlValue { Value = 5m } };
            var node = new SqlBinary { Op = SqlBinaryOp.And, Left = left, Right = right };
            var result = new SqlTextGenerator(new GenericSqlDialect()).Generate(node);
            Assert.Equal("((SpeciesId = $0) AND (Weight > $1))", result.SqlText);
            Assert.Equal(new object[] { 1, 5m }, result.Parameters);
        }

        [Fact]
        public void IsNull_And_IsNotNull()
        {
            var isNull = new SqlIsNull { Operand = Col<Animal>("ExhibitId") };
            var isNotNull = new SqlIsNull { Operand = Col<Animal>("ExhibitId"), Not = true };
            var g1 = new SqlTextGenerator(new GenericSqlDialect()).Generate(isNull);
            var g2 = new SqlTextGenerator(new GenericSqlDialect()).Generate(isNotNull);
            Assert.Equal("(ExhibitId IS NULL)", g1.SqlText);
            Assert.Equal("(ExhibitId IS NOT NULL)", g2.SqlText);
            Assert.Empty(g1.Parameters);
        }

        [Fact]
        public void BoolPredicate_UsesDialectUnaryBoolean()
        {
            var node = new SqlBoolPredicate { Column = Col<Species>("IsEndangered") };
            var generic = new SqlTextGenerator(new GenericSqlDialect()).Generate(node);
            var pg = new SqlTextGenerator(new PostgreSqlDialect()).Generate(node);
            Assert.Equal("(IsEndangered = 1)", generic.SqlText);
            Assert.Equal("(IsEndangered)", pg.SqlText);
        }

        [Fact]
        public void Not_WrapsOperand()
        {
            var node = new SqlNot { Operand = new SqlBoolPredicate { Column = Col<Species>("IsEndangered") } };
            var result = new SqlTextGenerator(new GenericSqlDialect()).Generate(node);
            Assert.Equal("(NOT (IsEndangered = 1))", result.SqlText);
        }

        [Fact]
        public void ParameterIndexModifier_OffsetsPlaceholders()
        {
            var node = new SqlBinary { Op = SqlBinaryOp.Equal, Left = Col<Animal>("SpeciesId"), Right = new SqlValue { Value = 7 } };
            var gen = new SqlTextGenerator(new GenericSqlDialect()) { ParameterIndexModifier = 3 };
            Assert.Equal("(SpeciesId = $3)", gen.Generate(node).SqlText);
        }

        [Fact]
        public void SeededParameterList_ContinuesNumbering()
        {
            var seed = new ArrayList { "existing" };
            var node = new SqlBinary { Op = SqlBinaryOp.Equal, Left = Col<Animal>("SpeciesId"), Right = new SqlValue { Value = 7 } };
            var result = new SqlTextGenerator(new GenericSqlDialect(), seed).Generate(node);
            Assert.Equal("(SpeciesId = $1)", result.SqlText);
            Assert.Equal(new object[] { "existing", 7 }, result.Parameters);
        }

        [Fact]
        public void InlineLiterals_WhenParameterizeLiteralsOff()
        {
            var node = new SqlBinary { Op = SqlBinaryOp.Equal, Left = Col<Animal>("Name"), Right = new SqlValue { Value = "O'Brien" } };
            var gen = new SqlTextGenerator(new GenericSqlDialect()) { ParameterizeLiterals = false };
            var result = gen.Generate(node);
            Assert.Equal("(Name = 'O''Brien')", result.SqlText);
            Assert.Empty(result.Parameters);
        }

        [Fact]
        public void QualifyColumns_PrependsTableName()
        {
            var node = new SqlBinary { Op = SqlBinaryOp.Equal, Left = Col<Animal>("SpeciesId"), Right = new SqlValue { Value = 1 } };
            var gen = new SqlTextGenerator(new GenericSqlDialect()) { QualifyColumns = true };
            Assert.Equal("(Animal.SpeciesId = $0)", gen.Generate(node).SqlText);
        }

        [Fact]
        public void Arithmetic_Negate_And_Modulo()
        {
            var node = new SqlBinary
            {
                Op = SqlBinaryOp.Modulo,
                Left = new SqlNegate { Operand = Col<Exhibit>("Capacity") },
                Right = new SqlValue { Value = 2 }
            };
            Assert.Equal("((-Capacity) % $0)", new SqlTextGenerator(new GenericSqlDialect()).Generate(node).SqlText);
        }

        [Fact]
        public void Literal_EmitsRawText()
        {
            var result = new SqlTextGenerator(new GenericSqlDialect()).Generate(new SqlLiteral { Text = "1 = 0" });
            Assert.Equal("1 = 0", result.SqlText);
        }

        [Fact]
        public void InlineLiterals_CharAndDateTime_AreQuotedGoldenStrings()
        {
            var charNode = new SqlBinary { Op = SqlBinaryOp.Equal, Left = Col<Animal>("Name"), Right = new SqlValue { Value = 'O' } };
            var charGen = new SqlTextGenerator(new GenericSqlDialect()) { ParameterizeLiterals = false };
            Assert.Equal("(Name = 'O')", charGen.Generate(charNode).SqlText);

            var quoteCharNode = new SqlBinary { Op = SqlBinaryOp.Equal, Left = Col<Animal>("Name"), Right = new SqlValue { Value = '\'' } };
            var quoteCharGen = new SqlTextGenerator(new GenericSqlDialect()) { ParameterizeLiterals = false };
            Assert.Equal("(Name = '''')", quoteCharGen.Generate(quoteCharNode).SqlText);

            var dt = new DateTime(2024, 3, 7, 13, 5, 9, 250);
            var dateNode = new SqlBinary { Op = SqlBinaryOp.Equal, Left = Col<Animal>("Name"), Right = new SqlValue { Value = dt } };
            var dateGen = new SqlTextGenerator(new GenericSqlDialect()) { ParameterizeLiterals = false };
            Assert.Equal("(Name = '2024-03-07 13:05:09.250')", dateGen.Generate(dateNode).SqlText);
        }
    }
}
