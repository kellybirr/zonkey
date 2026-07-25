using System;
using System.Collections.Generic;
using System.Data;
using System.Linq.Expressions;
using Xunit;
using Zonkey;
using Zonkey.Dialects;
using Zonkey.ObjectModel;
using Zonkey.ObjectModel.QueryTranslation;
using Zonkey.Tests.Models;

namespace Zonkey.Tests.Unit.QueryTranslation
{
    // Shared pattern: reduce then translate then generate against a chosen dialect.
    internal static class TranslationTestHelper
    {
        public static SqlWhereClause Translate<T>(Expression<Func<T, bool>> expr, SqlDialect dialect = null)
        {
            dialect = dialect ?? new GenericSqlDialect();
            var maps = new Dictionary<string, DataMap> { { expr.Parameters[0].Name, DataMap.GenerateCached(typeof(T)) } };
            var body = PartialEvaluator.Reduce(expr.Body);
            var translator = new ExpressionTranslator(maps, dialect);
            SqlNode root = translator.TranslatePredicate(body);
            return new SqlTextGenerator(dialect).Generate(root);
        }
    }

    public class TranslatorCoreTests
    {
        private static SqlWhereClause T(Expression<Func<Animal, bool>> e) => TranslationTestHelper.Translate(e);
        private static SqlWhereClause TS(Expression<Func<Species, bool>> e) => TranslationTestHelper.Translate(e);
        private static SqlWhereClause TE(Expression<Func<Exhibit, bool>> e) => TranslationTestHelper.Translate(e);

        [Fact]
        public void Comparisons_AllSixOperators()
        {
            Assert.Equal("(SpeciesId = $0)", T(a => a.SpeciesId == 1).SqlText);
            Assert.Equal("(SpeciesId != $0)", T(a => a.SpeciesId != 1).SqlText);
            Assert.Equal("(SpeciesId < $0)", T(a => a.SpeciesId < 1).SqlText);
            Assert.Equal("(SpeciesId <= $0)", T(a => a.SpeciesId <= 1).SqlText);
            Assert.Equal("(SpeciesId > $0)", T(a => a.SpeciesId > 1).SqlText);
            Assert.Equal("(SpeciesId >= $0)", T(a => a.SpeciesId >= 1).SqlText);
        }

        [Fact]
        public void Logical_AndOrNot()
        {
            Assert.Equal("((SpeciesId = $0) AND (Name = $1))", T(a => a.SpeciesId == 1 && a.Name == "x").SqlText);
            Assert.Equal("((SpeciesId = $0) OR (SpeciesId = $1))", T(a => a.SpeciesId == 1 || a.SpeciesId == 2).SqlText);
            Assert.Equal("(NOT (SpeciesId = $0))", T(a => !(a.SpeciesId == 1)).SqlText);
        }

        [Fact]
        public void NullLiteral_And_NullVariable_BecomeIsNull()
        {
            Assert.Equal("(ExhibitId IS NULL)", T(a => a.ExhibitId == null).SqlText);
            Assert.Equal("(ExhibitId IS NOT NULL)", T(a => a.ExhibitId != null).SqlText);
            string name = null;
            var byVar = T(a => a.Name == name);
            Assert.Equal("(Name IS NULL)", byVar.SqlText);
            Assert.Empty(byVar.Parameters);
        }

        [Fact]
        public void BoolColumn_PredicateAndValuePositions()
        {
            Assert.Equal("(IsEndangered = 1)", TS(s => s.IsEndangered).SqlText);
            Assert.Equal("(NOT (IsEndangered = 1))", TS(s => !s.IsEndangered).SqlText);
            Assert.Equal("(IsEndangered = $0)", TS(s => s.IsEndangered == true).SqlText);
            Assert.Equal("((IsEndangered = 1) AND (SpeciesId > $0))", TS(s => s.IsEndangered && s.SpeciesId > 0).SqlText);
        }

        [Fact]
        public void ConstantTrue_And_False_Predicates()
        {
            Assert.Equal("1 = 1", T(a => true).SqlText);
            Assert.Equal("1 = 0", T(a => false).SqlText);
        }

        [Fact]
        public void HasValue_And_Value()
        {
            Assert.Equal("(ExhibitId IS NOT NULL)", T(a => a.ExhibitId.HasValue).SqlText);
            Assert.Equal("(NOT (ExhibitId IS NOT NULL))", T(a => !a.ExhibitId.HasValue).SqlText);
            Assert.Equal("(Weight > $0)", T(a => a.Weight.Value > 5m).SqlText);
        }

        [Fact]
        public void Arithmetic_WithCorrectGrouping()
        {
            Assert.Equal("((Capacity + $0) > $1)", TE(e => e.Capacity + 5 > 10).SqlText);
            Assert.Equal("(((Capacity * $0) - $1) >= $2)", TE(e => e.Capacity * 2 - 1 >= 9).SqlText);
            Assert.Equal("((Capacity % $0) = $1)", TE(e => e.Capacity % 2 == 0).SqlText);
            Assert.Equal("((-Capacity) < $0)", TE(e => -e.Capacity < 0).SqlText);
        }

        [Fact]
        public void MultiplyTwoColumns_Translates()
        {
            Assert.Equal("((Capacity * Capacity) < $0)", TE(e => e.Capacity * e.Capacity < 100).SqlText);
        }

        [Fact]
        public void Coalesce_And_Ternary()
        {
            Assert.Equal("(COALESCE(Notes, $0) = $1)", T(a => (a.Notes ?? "none") == "x").SqlText);
            Assert.Equal("(CASE WHEN (ExhibitId IS NULL) THEN $0 ELSE $1 END = $2)",
                T(a => (a.ExhibitId == null ? 0 : 1) == 1).SqlText);
        }

        [Fact]
        public void CapturedValues_ProduceParameters()
        {
            int min = 3; string prefix = "Mei";
            var r = T(a => a.SpeciesId >= min && a.Name == prefix);
            Assert.Equal("((SpeciesId >= $0) AND (Name = $1))", r.SqlText);
            Assert.Equal(new object[] { 3, "Mei" }, r.Parameters);
        }

        [Fact]
        public void UnmappedProperty_ThrowsSqlExpressionException()
        {
            var ex = Assert.Throws<SqlExpressionException>(() => T(a => a.DataRowState == DataRowState.Added));
            Assert.Contains("DataRowState", ex.Message);
        }

        [Fact]
        public void UntranslatableMethod_ThrowsWithMethodName()
        {
            var ex = Assert.Throws<SqlExpressionException>(() => T(a => a.Name.PadLeft(5) == "x"));
            Assert.Contains("PadLeft", ex.Message);
        }

        [Fact]
        public void NullableBoolValue_PredicatePosition_RendersEqualsOne()
        {
            var r = TranslationTestHelper.Translate<NullBoolModel>(m => m.MaybeFlag.Value);
            Assert.Equal("(MaybeFlag = 1)", r.SqlText);
            var r2 = TranslationTestHelper.Translate<NullBoolModel>(m => !m.MaybeFlag.Value);
            Assert.Equal("(NOT (MaybeFlag = 1))", r2.SqlText);
            var r3 = TranslationTestHelper.Translate<NullBoolModel>(m => m.MaybeFlag == null);
            Assert.Equal("(MaybeFlag IS NULL)", r3.SqlText);
        }

        [Fact]
        public void BitwiseAnd_OnIntOperands_RendersAmpersand()
        {
            Assert.Equal("((SpeciesId & $0) = $1)", T(a => (a.SpeciesId & 4) == 4).SqlText);
            Assert.Equal("((SpeciesId | $0) > $1)", T(a => (a.SpeciesId | 1) > 0).SqlText);
        }
    }

    [DataItem("NullBoolModel")]
    public class NullBoolModel : DataClass
    {
        private int _id;
        private bool? _maybeFlag;

        public NullBoolModel() : base(true) { }
        public NullBoolModel(bool addingNew) : base(addingNew) { }

        [DataField("Id", DbType.Int32, IsKeyField = true, IsAutoIncrement = true)]
        public int Id
        {
            get => _id;
            set => SetFieldValue(ref _id, value);
        }

        [DataField("MaybeFlag", DbType.Boolean, true)]
        public bool? MaybeFlag
        {
            get => _maybeFlag;
            set => SetFieldValue(ref _maybeFlag, value);
        }
    }
}
