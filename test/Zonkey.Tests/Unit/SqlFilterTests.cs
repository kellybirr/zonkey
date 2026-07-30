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
    }
}
