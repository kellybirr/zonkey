#if !NETFRAMEWORK
using Zonkey.Tests.Infrastructure;

namespace Zonkey.Tests.Integration.Sqlite
{
    public class SqliteExpressionFilterTests : ExpressionFilterTests<SqliteFixture>
    {
        public SqliteExpressionFilterTests(SqliteFixture db) : base(db) { }
    }
}
#endif
