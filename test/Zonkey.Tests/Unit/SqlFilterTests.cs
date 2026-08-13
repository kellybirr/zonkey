using System.Linq;
using Xunit;
using Zonkey.Dialects;

namespace Zonkey.Tests.Unit
{
    public class SqlFilterTests
    {
        private readonly SqlDialect _sqlServer = new SqlServerDialect();
        private readonly SqlDialect _generic = new GenericSqlDialect();

        [Fact]
        public void EQ_GeneratesEqualsClause()
        {
            var filter = SqlFilter.EQ("Name", "Test");
            var sql = filter.ToString(_generic, 0);
            Assert.Contains("Name", sql);
            Assert.Contains("=", sql);
        }

        [Fact]
        public void NEQ_GeneratesNotEqualsClause()
        {
            var filter = SqlFilter.NEQ("Name", "Test");
            var sql = filter.ToString(_generic, 0);
            Assert.Contains("!=", sql);
        }

        [Fact]
        public void GT_GeneratesGreaterThan()
        {
            var filter = SqlFilter.GT("Capacity", 10);
            var sql = filter.ToString(_generic, 0);
            Assert.Contains(">", sql);
        }

        [Fact]
        public void GTE_GeneratesGreaterThanOrEqual()
        {
            var filter = SqlFilter.GTE("Capacity", 10);
            var sql = filter.ToString(_generic, 0);
            Assert.Contains(">=", sql);
        }

        [Fact]
        public void LT_GeneratesLessThan()
        {
            var filter = SqlFilter.LT("Weight", 5.0m);
            var sql = filter.ToString(_generic, 0);
            Assert.Contains("<", sql);
        }

        [Fact]
        public void LTE_GeneratesLessThanOrEqual()
        {
            var filter = SqlFilter.LTE("Weight", 5.0m);
            var sql = filter.ToString(_generic, 0);
            Assert.Contains("<=", sql);
        }

        [Fact]
        public void NULL_GeneratesIsNull()
        {
            var filter = SqlFilter.NULL("ExhibitId");
            var sql = filter.ToString(_generic, 0);
            Assert.Contains("IS NULL", sql);
        }

        [Fact]
        public void NOTNULL_GeneratesIsNotNull()
        {
            var filter = SqlFilter.NOTNULL("ExhibitId");
            var sql = filter.ToString(_generic, 0);
            Assert.Contains("IS NOT NULL", sql);
        }

        [Fact]
        public void LIKE_GeneratesLikeClause()
        {
            var filter = SqlFilter.LIKE("Name", "%panda%");
            var sql = filter.ToString(_generic, 0);
            Assert.Contains("LIKE", sql);
        }

        [Fact]
        public void NOTLIKE_GeneratesNotLikeClause()
        {
            var filter = SqlFilter.NOTLIKE("Name", "%test%");
            var sql = filter.ToString(_generic, 0);
            Assert.Contains("NOT LIKE", sql);
        }

        [Fact]
        public void EQ_SqlServer_UsesNamedParameter()
        {
            var filter = SqlFilter.EQ("Name", "Test");
            var sql = filter.ToString(_sqlServer, 0);
            Assert.Contains("@", sql);
        }

        [Fact]
        public void EQ_FieldNamePreserved()
        {
            var filter = SqlFilter.EQ("SpeciesId", 1);
            Assert.Equal("SpeciesId", filter.FieldName);
        }

        [Fact]
        public void EQ_ValuePreserved()
        {
            var filter = SqlFilter.EQ("SpeciesId", 42);
            Assert.Equal(42, filter.Value);
        }

        [Fact]
        public void NULL_HasDbNullValue()
        {
            var filter = SqlFilter.NULL("ExhibitId");
            Assert.Equal(System.DBNull.Value, filter.Value);
        }

        [Fact]
        public void ParameterIndex_AffectsParameterName()
        {
            var filter = SqlFilter.EQ("Name", "Test");
            var sql0 = filter.ToString(_sqlServer, 0);
            var sql1 = filter.ToString(_sqlServer, 1);
            Assert.NotEqual(sql0, sql1);
        }

        [Fact]
        public void IN_MultiValue_RendersInListWithSuffixedParameters()
        {
            var filter = SqlFilter.IN("ClientId", new long[] { 1, 2, 3 });
            var sql = filter.ToString(_sqlServer, 4);
            Assert.Contains("ClientId", sql);
            Assert.Contains(" IN (", sql);
            Assert.Contains("p4_0", sql);
            Assert.Contains("p4_1", sql);
            Assert.Contains("p4_2", sql);
        }

        [Fact]
        public void IN_MultiValue_PositionalDialect_RendersOnePlaceholderPerValue()
        {
            var filter = SqlFilter.IN("ClientId", new long[] { 1, 2, 3 });
            var sql = filter.ToString(_generic, 4);
            Assert.Contains("ClientId IN (", sql);
            Assert.Equal(3, sql.Count(c => c == '?'));
        }

        [Fact]
        public void IN_MultiValue_AddsOneParameterPerValue()
        {
            var filter = SqlFilter.IN("ClientId", new long[] { 7, 8 });
            using var command = new Zonkey.Mocks.MockDbConnection().CreateCommand();
            filter.ToString(_generic, 0);
            filter.AddToCommandParams(command, _generic, 0);
            Assert.Equal(2, command.Parameters.Count);
        }

        [Fact]
        public void IN_SingleValue_CollapsesToEquality()
        {
            var filter = SqlFilter.IN("ClientId", new long[] { 42 });
            var sql = filter.ToString(_generic, 0);
            Assert.Contains("=", sql);
            Assert.DoesNotContain("IN (", sql);

            using var command = new Zonkey.Mocks.MockDbConnection().CreateCommand();
            filter.AddToCommandParams(command, _generic, 0);
            Assert.Single(command.Parameters.Cast<System.Data.Common.DbParameter>());
        }

        [Fact]
        public void IN_EmptyList_RendersAlwaysFalse_AddsNoParameters()
        {
            var filter = SqlFilter.IN("ClientId", System.Array.Empty<long>());
            var sql = filter.ToString(_generic, 0);
            Assert.StartsWith("1 = 0", sql);

            using var command = new Zonkey.Mocks.MockDbConnection().CreateCommand();
            filter.AddToCommandParams(command, _generic, 0);
            Assert.Empty(command.Parameters.Cast<System.Data.Common.DbParameter>());
        }

        [Fact]
        public void IN_NullElements_AreIgnored()
        {
            var filter = SqlFilter.IN("Name", new object[] { "a", null, System.DBNull.Value });
            var sql = filter.ToString(_generic, 0);
            Assert.Contains("=", sql); // collapses to single-value equality
        }

        [Fact]
        public void IN_Postgres_MultiValue_UsesCollectionParameter()
        {
            var filter = SqlFilter.IN("ClientId", new long[] { 1, 2, 3 });
            var sql = filter.ToString(new PostgreSqlDialect(), 2);
            Assert.Contains("= ANY(", sql);
            Assert.Contains(":p2", sql);

            using var command = new Zonkey.Mocks.MockDbConnection().CreateCommand();
            filter.AddToCommandParams(command, new PostgreSqlDialect(), 2);
            var parm = Assert.Single(command.Parameters.Cast<System.Data.Common.DbParameter>());
            Assert.IsType<long[]>(parm.Value);
        }

        [Fact]
        public void IN_MixedRuntimeTypes_FallsBackToIndividualParameters()
        {
            var filter = SqlFilter.IN("Id", new object[] { 1L, 2 }); // long + int
            var sql = filter.ToString(new PostgreSqlDialect(), 0);
            Assert.Contains("IN (", sql);
            Assert.DoesNotContain("ANY", sql);
        }

        [Fact]
        public void IN_ValueExposesFilteredValues()
        {
            var filter = SqlFilter.IN("Id", new long[] { 5, 6 });
            var values = Assert.IsType<object[]>(filter.Value);
            Assert.Equal(2, values.Length);
        }

        [Fact]
        public void IN_ValueSetter_ReplacesTheList()
        {
            var filter = SqlFilter.IN("ClientId", new long[] { 1, 2, 3 });
            filter.Value = new long[] { 9 };

            // collapses to single-value equality with the NEW value bound
            var sql = filter.ToString(_generic, 0);
            Assert.Contains("=", sql);
            Assert.DoesNotContain("IN (", sql);

            using var command = new Zonkey.Mocks.MockDbConnection().CreateCommand();
            filter.AddToCommandParams(command, _generic, 0);
            var parm = Assert.Single(command.Parameters.Cast<System.Data.Common.DbParameter>());
            Assert.Equal(9L, parm.Value);
        }

        [Fact]
        public void IN_ValueSetter_ScalarBecomesSingleElementList()
        {
            var filter = SqlFilter.IN("ClientId", new long[] { 1, 2 });
            filter.Value = 42L;
            var values = Assert.IsType<object[]>(filter.Value);
            Assert.Equal(42L, Assert.Single(values));
        }
    }
}
