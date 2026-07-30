using System;
using System.Collections.Generic;
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
    public class InListTests
    {
        private static SqlWhereClause T(Expression<Func<Animal, bool>> e) => TranslationTestHelper.Translate(e);

        private static SqlColumn Col<T>(string propertyName)
        {
            var map = DataMap.GenerateCached(typeof(T));
            var field = map.GetFieldForProperty(typeof(T).GetProperty(propertyName));
            return new SqlColumn { Field = field, Map = map, IsBoolean = field.Property.PropertyType == typeof(bool) };
        }

        [Fact]
        public void EnumerableContains_SmallList_Parameterizes()
        {
            var ids = new[] { 1, 2, 3 };
            var r = T(a => ids.Contains(a.SpeciesId));
            Assert.Equal("(SpeciesId IN ($0,$1,$2))", r.SqlText);
            Assert.Equal(new object[] { 1, 2, 3 }, r.Parameters);
        }

        [Fact]
        public void ListContains_InstanceMethod_Works()
        {
            var ids = new List<int> { 4, 5 };
            Assert.Equal("(SpeciesId IN ($0,$1))", T(a => ids.Contains(a.SpeciesId)).SqlText);
        }

        [Fact]
        public void Contains_EmptyList_RendersConstantFalse()
        {
            var ids = new int[0];
            var r = T(a => ids.Contains(a.SpeciesId));
            Assert.Equal("1 = 0", r.SqlText);
            Assert.Empty(r.Parameters);
        }

        [Fact]
        public void Contains_LargeSafeList_InlinesLiterals()
        {
            var ids = Enumerable.Range(1, 100).ToArray();
            var r = T(a => ids.Contains(a.SpeciesId));
            Assert.Empty(r.Parameters);
            string expected = "(SpeciesId IN (" + string.Join(",", Enumerable.Range(1, 100)) + "))";
            Assert.Equal(expected, r.SqlText);
        }

        [Fact]
        public void Contains_LargeUnsafeList_StaysParameterized()
        {
            var names = Enumerable.Range(1, 100).Select(i => "n" + i).ToArray();
            var r = T(a => names.Contains(a.Name));
            Assert.Equal(100, r.Parameters.Length);
            Assert.Equal("n1", r.Parameters[0]);
            Assert.Equal("n100", r.Parameters[99]);
            Assert.Contains("$0", r.SqlText);
            Assert.Contains("$99", r.SqlText);
        }

        [Fact]
        public void Contains_ExactlyAtThreshold_StaysParameterized()
        {
            // threshold is InlineThreshold(64); values.Count > threshold triggers inlining, so 64 stays parameterized
            var ids = Enumerable.Range(1, 64).ToArray();
            var r = T(a => ids.Contains(a.SpeciesId));
            Assert.Equal(64, r.Parameters.Length);
            Assert.Contains("$0", r.SqlText);
            Assert.Contains("$63", r.SqlText);
            Assert.DoesNotContain("IN (1,2,3", r.SqlText);
        }

        [Fact]
        public void Contains_OneOverThreshold_Inlines()
        {
            // 65 > InlineThreshold(64) triggers literal inlining, with zero parameters
            var ids = Enumerable.Range(1, 65).ToArray();
            var r = T(a => ids.Contains(a.SpeciesId));
            Assert.Empty(r.Parameters);
            string expected = "(SpeciesId IN (" + string.Join(",", Enumerable.Range(1, 65)) + "))";
            Assert.Equal(expected, r.SqlText);
        }

        [Fact]
        public void Contains_OverParameterLimit_UnsafeType_Throws()
        {
            var names = Enumerable.Range(1, 2101).Select(i => "n" + i).ToArray();
            var ex = Assert.Throws<SqlExpressionException>(() => T(a => names.Contains(a.Name)));
            Assert.Contains("SplitList", ex.Message);
        }

        [Fact]
        public void Contains_LargeUnsignedList_InlinesLiterals()
        {
            var ids = Enumerable.Range(1, 100).Select(i => (uint)i).ToArray();
            var r = T(a => ids.Contains((uint)a.SpeciesId));
            Assert.Empty(r.Parameters);
            Assert.Contains("IN (1,2,3", r.SqlText);
        }

        [Fact]
        public void Contains_GuidList_Works()
        {
            var g1 = Guid.NewGuid(); var g2 = Guid.NewGuid();
            var ids = new[] { g1, g2 };
            var r = T(a => ids.Contains(a.ZookeeperId));
            Assert.Equal("(ZookeeperId IN ($0,$1))", r.SqlText);
        }

        [Fact]
        public void Contains_NullsInList_AddsIsNullCheck()
        {
            var ids = new int?[] { 1, null, 3 };
            var r = T(a => ids.Contains(a.ExhibitId));
            Assert.Equal("((ExhibitId IN ($0,$1)) OR (ExhibitId IS NULL))", r.SqlText);
            Assert.Equal(new object[] { 1, 3 }, r.Parameters);
        }

        [Fact]
        public void Contains_OnlyNullInList_RendersIsNull()
        {
            var ids = new int?[] { null };
            var r = T(a => ids.Contains(a.ExhibitId));
            Assert.Equal("(ExhibitId IS NULL)", r.SqlText);
            Assert.Empty(r.Parameters);
        }

        [Fact]
        public void Contains_ParameterCap_AccountsForWholeCommand()
        {
            var names = Enumerable.Range(1, 2100).Select(i => "n" + i).ToArray();
            // exactly 2100 with nothing else: OK
            var ok = T(a => names.Contains(a.Name));
            Assert.Equal(2100, ok.Parameters.Length);
            // one prior parameter pushes the same list over the cap
            var ex = Assert.Throws<SqlExpressionException>(() => T(a => a.SpeciesId == 1 && names.Contains(a.Name)));
            Assert.Contains("SplitList", ex.Message);
        }

        [Fact]
        public void Contains_LargeList_OnPostgres_BindsSingleArrayParameter()
        {
            var ids = Enumerable.Range(1, 65).ToArray();
            var r = TranslationTestHelper.Translate<Animal>(a => ids.Contains(a.SpeciesId), new PostgreSqlDialect());
            Assert.Equal("(SpeciesId = ANY($0))", r.SqlText);
            var arr = Assert.IsType<int[]>(r.Parameters[0]);
            Assert.Equal(65, arr.Length);
            Assert.Equal(1, arr[0]);
            Assert.Equal(65, arr[64]);
        }

        [Fact]
        public void Contains_SmallList_OnPostgres_KeepsIndividualParameters()
        {
            var ids = Enumerable.Range(1, 64).ToArray();
            var r = TranslationTestHelper.Translate<Animal>(a => ids.Contains(a.SpeciesId), new PostgreSqlDialect());
            Assert.Equal(64, r.Parameters.Length);
            Assert.Contains("IN (", r.SqlText);
        }

        [Fact]
        public void Contains_LargeStringList_OnPostgres_UsesArrayNotThrow()
        {
            var names = Enumerable.Range(1, 2500).Select(i => "n" + i).ToArray();
            var r = TranslationTestHelper.Translate<Animal>(a => names.Contains(a.Name), new PostgreSqlDialect());
            Assert.Single(r.Parameters);
            Assert.IsType<string[]>(r.Parameters[0]);
        }

        [Fact]
        public void Contains_NullInLargeList_OnPostgres_ArrayPlusIsNull()
        {
            var ids = Enumerable.Range(1, 65).Select(i => (int?)i).Concat(new int?[] { null }).ToArray();
            var r = TranslationTestHelper.Translate<Animal>(a => ids.Contains(a.ExhibitId), new PostgreSqlDialect());
            Assert.Equal("((ExhibitId = ANY($0)) OR (ExhibitId IS NULL))", r.SqlText);
        }

        [Fact]
        public void Contains_LargeList_OnPostgres_BindsTypedArrays()
        {
            var longs = Enumerable.Range(1, 65).Select(i => (long)i).ToArray();
            var rl = TranslationTestHelper.Translate<Animal>(a => longs.Contains((long)a.SpeciesId), new PostgreSqlDialect());
            Assert.IsType<long[]>(Assert.Single(rl.Parameters));

            var guids = Enumerable.Range(1, 65).Select(_ => Guid.NewGuid()).ToArray();
            var rg = TranslationTestHelper.Translate<Animal>(a => guids.Contains(a.ZookeeperId), new PostgreSqlDialect());
            Assert.Equal("(ZookeeperId = ANY($0))", rg.SqlText);
            Assert.IsType<Guid[]>(Assert.Single(rg.Parameters));

            var names = Enumerable.Range(1, 65).Select(i => "n" + i).ToArray();
            var rs = TranslationTestHelper.Translate<Animal>(a => names.Contains(a.Name), new PostgreSqlDialect());
            Assert.IsType<string[]>(Assert.Single(rs.Parameters));
        }

        [Fact]
        public void Contains_LargeByteList_OnPostgres_DoesNotUseArrayParameter()
        {
            var bytes = Enumerable.Range(1, 65).Select(i => (byte)i).ToArray();
            var r = TranslationTestHelper.Translate<Animal>(a => bytes.Contains((byte)a.SpeciesId), new PostgreSqlDialect());
            Assert.DoesNotContain("ANY", r.SqlText);   // falls back to inline-safe path (byte is a safe literal type)
            Assert.Empty(r.Parameters);
        }

        [Fact]
        public void SupportsInCollectionParameter_OnPostgres_ExcludesEnumsAndUnmappedIntegerTypes()
        {
            var dialect = new PostgreSqlDialect();

            // enums: FixParameter converts scalar enum params, but that hook never runs for array
            // elements, and Npgsql only binds arrays for explicitly mapped native enums - which the
            // dialect has no way to detect - so enum lists must stay individually parameterized.
            Assert.False(dialect.SupportsInCollectionParameter(typeof(DayOfWeek)));

            // byte[] binds as bytea, not smallint[]; sbyte/ushort/uint/ulong have no Npgsql array mapping.
            Assert.False(dialect.SupportsInCollectionParameter(typeof(byte)));
            Assert.False(dialect.SupportsInCollectionParameter(typeof(sbyte)));
            Assert.False(dialect.SupportsInCollectionParameter(typeof(ushort)));
            Assert.False(dialect.SupportsInCollectionParameter(typeof(uint)));
            Assert.False(dialect.SupportsInCollectionParameter(typeof(ulong)));

            // ordinary scalar-mappable types remain supported.
            Assert.True(dialect.SupportsInCollectionParameter(typeof(int)));
            Assert.True(dialect.SupportsInCollectionParameter(typeof(DateTime)));
        }

        [Fact]
        public void MixedRuntimeTypeList_OnPostgres_FallsBackToIndividualParameters()
        {
            // A list with a heterogeneous runtime element type (e.g. an IEnumerable<object> mixing int
            // and string) must not be forced into Array.CreateInstance/SetValue - that throws
            // InvalidCastException on the mismatched element. Build the SqlInValues node directly since
            // a natural C# expression can't produce mixed boxed types through Contains<object>.
            var values = new List<object>();
            for (int i = 0; i < 64; i++) values.Add("s" + i);
            values.Add(42); // 65th element has a different runtime type than values[0] (string)

            var node = new SqlInValues { Operand = Col<Animal>("Name"), Values = values };
            var result = new SqlTextGenerator(new PostgreSqlDialect()).Generate(node);

            Assert.DoesNotContain("ANY", result.SqlText);
            Assert.Contains("IN (", result.SqlText);
            Assert.Equal(65, result.Parameters.Length);
        }

        [Fact]
        public void Contains_ParameterCap_ReversedOrder_AccountsForWholeCommand()
        {
            // The IN list alone is exactly at the 2100 cap and passes its own in-place check; a
            // trailing predicate parameter (rendered AFTER the list) pushes the whole command over the
            // cap. Only the post-generation check in SqlTextGenerator.Generate catches this ordering.
            var names = Enumerable.Range(1, 2100).Select(i => "n" + i).ToArray();
            var ex = Assert.Throws<SqlExpressionException>(() => T(a => names.Contains(a.Name) && a.SpeciesId == 1));
            Assert.Contains("SplitList", ex.Message);
        }

        [Fact]
        public void MaxParameters_ComesFromDialect()
        {
            Assert.Equal(2100, new SqlServerDialect().MaxParameters);
            Assert.Equal(65535, new PostgreSqlDialect().MaxParameters);
            Assert.Equal(65535, new MySqlDialect().MaxParameters);
            Assert.Equal(32766, new SqliteDialect().MaxParameters);
            // MySQL has no collection parameters but a high cap: 2101 strings parameterize instead of throwing
            var names = Enumerable.Range(1, 2101).Select(i => "n" + i).ToArray();
            var r = TranslationTestHelper.Translate<Animal>(a => names.Contains(a.Name), new MySqlDialect());
            Assert.Equal(2101, r.Parameters.Length);
        }

#pragma warning disable 618
        [Fact]
        public void LegacySqlIn_StillTranslates()
        {
            var ids = new[] { 1, 2 };
            Assert.Equal("(SpeciesId IN ($0,$1))", T(a => a.SpeciesId.SqlIn(ids)).SqlText);
        }

        [Fact]
        public void LegacySqlInInt_InlinesRegardlessOfCount()
        {
            var ids = new[] { 1, 2, 3 };
            var r = T(a => a.SpeciesId.SqlInInt(ids));
            Assert.Equal("(SpeciesId IN (1,2,3))", r.SqlText);
            Assert.Empty(r.Parameters);
        }

        [Fact]
        public void LegacySqlIn_EmptyList_ThrowsArgumentException()
        {
            var empty = new int[0];
            Assert.Throws<ArgumentException>(() => T(a => a.SpeciesId.SqlInInt(empty)));
        }
#pragma warning restore 618

        // T4: negation matrix - IN list forms.
        [Fact]
        public void NegatedContains_SmallList_WrapsWithNot()
        {
            var ids = new[] { 1, 2, 3 };
            var r = T(a => !ids.Contains(a.SpeciesId));
            Assert.Equal("(NOT (SpeciesId IN ($0,$1,$2)))", r.SqlText);
            Assert.Equal(new object[] { 1, 2, 3 }, r.Parameters);
        }

        [Fact]
        public void NegatedContains_LargeList_OnPostgres_WrapsArrayFormWithNot()
        {
            var ids = Enumerable.Range(1, 65).ToArray();
            var r = TranslationTestHelper.Translate<Animal>(a => !ids.Contains(a.SpeciesId), new PostgreSqlDialect());
            Assert.Equal("(NOT (SpeciesId = ANY($0)))", r.SqlText);
            Assert.IsType<int[]>(Assert.Single(r.Parameters));
        }

        [Fact]
        public void NegatedContains_EmptyList_WrapsConstantFalseWithNot()
        {
            var ids = new int[0];
            var r = T(a => !ids.Contains(a.SpeciesId));
            Assert.Equal("(NOT 1 = 0)", r.SqlText);
            Assert.Empty(r.Parameters);
        }

        [Fact]
        public void NegatedThreeArgSqlIn_WrapsSubqueryWithNot()
        {
            var r = T(a => !a.ExhibitId.SqlIn((Exhibit e) => e.ExhibitId, e => e.IsOpen));
            Assert.Equal("(NOT (ExhibitId IN (SELECT ExhibitId FROM Exhibit WHERE (IsOpen = 1))))", r.SqlText);
        }
    }
}
