using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
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

        [Fact]
        public void BoolCoalesce_PredicatePosition_AllDialects()
        {
            var generic = TranslationTestHelper.Translate<NullBoolModel>(m => m.MaybeFlag ?? false);
            Assert.Equal("(COALESCE(MaybeFlag, $0) = 1)", generic.SqlText);

            var mssql = TranslationTestHelper.Translate<NullBoolModel>(m => m.MaybeFlag ?? false, new SqlServerDialect());
            Assert.Equal("(ISNULL([MaybeFlag], $0) = 1)", mssql.SqlText);

            var pg = TranslationTestHelper.Translate<NullBoolModel>(m => m.MaybeFlag ?? false, new PostgreSqlDialect());
            Assert.Equal("(COALESCE(MaybeFlag, $0))", pg.SqlText);
        }

        [Fact]
        public void BoolCoalesce_Negated_WrapsCorrectly()
        {
            var r = TranslationTestHelper.Translate<NullBoolModel>(m => !(m.MaybeFlag ?? false));
            Assert.Equal("(NOT (COALESCE(MaybeFlag, $0) = 1))", r.SqlText);
        }

        [Fact]
        public void BoolTernary_PredicatePosition_WrapsCaseWhen()
        {
            var r = TS(s => s.SpeciesId > 1 ? true : s.IsEndangered);
            Assert.Equal("(CASE WHEN (SpeciesId > $0) THEN $1 ELSE IsEndangered END = 1)", r.SqlText);
        }

        // T1: one predicate chaining equality, an escaped Contains, Math.Round, a coalesce, an IN
        // list, and a subquery SqlIn, all combined with left-associative AndAlso. This guards that
        // SqlTextGenerator.Render()'s mark/truncate mechanism keeps parameter numbering in strict
        // left-to-right appearance order even when nested through SqlFunction args and a subquery.
        [Fact]
        public void ComplexPredicate_NestedParameterOrdering_MatchesAppearanceOrder()
        {
            var ids3 = new[] { 1, 2, 3 };
            Expression<Func<Animal, bool>> expr = a =>
                a.SpeciesId == 9
                && a.Name.Contains("50%")
                && Math.Round(a.Weight.Value) > 5m
                && (a.ExhibitId ?? -1) != -2
                && ids3.Contains(a.SpeciesId)
                && a.ExhibitId.SqlIn((Exhibit e) => e.ExhibitId, e => e.Capacity > 10);

            var r = TranslationTestHelper.Translate(expr);

            Assert.Equal(
                "((((((SpeciesId = $0) AND (Name LIKE $1 ESCAPE '\\')) AND (ROUND(Weight) > $2)) AND " +
                "(COALESCE(ExhibitId, $3) != $4)) AND (SpeciesId IN ($5,$6,$7))) AND " +
                "(ExhibitId IN (SELECT ExhibitId FROM Exhibit WHERE (Capacity > $8))))",
                r.SqlText);
            Assert.Equal(new object[] { 9, "%50\\%%", 5m, -1, -2, 1, 2, 3, 10 }, r.Parameters);
        }

        // T2: Convert/ConvertChecked nodes must be transparent - the translator unwraps them and
        // translates the operand directly, so the column renders bare and the constant keeps its
        // original CLR type (no coercion to the target conversion type).
        [Fact]
        public void Conversion_LongCast_IsTransparentAndPreservesParameterType()
        {
            var r = T(a => (long)a.SpeciesId == 5L);
            Assert.Equal("(SpeciesId = $0)", r.SqlText);
            Assert.IsType<long>(r.Parameters[0]);
            Assert.Equal(5L, r.Parameters[0]);
        }

        [Fact]
        public void Conversion_DecimalCast_IsTransparentAndPreservesParameterType()
        {
            var r = T(a => (decimal)a.SpeciesId > 1m);
            Assert.Equal("(SpeciesId > $0)", r.SqlText);
            Assert.IsType<decimal>(r.Parameters[0]);
        }

        [Fact]
        public void NullableLift_RendersSameColumnAsExplicitValue()
        {
            // a.Weight is decimal?; comparing it directly (without .Value) goes through the
            // lifted-to-null GreaterThan operator, but the translator only looks at Left/Right
            // expressions, so it renders identically to a.Weight.Value > 5m.
            Assert.Equal("(Weight > $0)", T(a => a.Weight > 5m).SqlText);
        }

        [Fact]
        public void ManuallyBuiltConvertChecked_TranslatesTransparently()
        {
            // C# never emits ConvertChecked for an int -> long widening conversion (it can't
            // overflow), so build the tree directly to exercise the ConvertChecked branch in
            // ExpressionTranslator.Translate.
            ParameterExpression param = Expression.Parameter(typeof(Animal), "a");
            MemberExpression access = Expression.Property(param, nameof(Animal.SpeciesId));
            UnaryExpression convert = Expression.ConvertChecked(access, typeof(long));
            BinaryExpression eq = Expression.Equal(convert, Expression.Constant(5L));
            var lambda = Expression.Lambda<Func<Animal, bool>>(eq, param);

            var r = TranslationTestHelper.Translate(lambda);
            Assert.Equal("(SpeciesId = $0)", r.SqlText);
            Assert.Equal(new object[] { 5L }, r.Parameters);
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
