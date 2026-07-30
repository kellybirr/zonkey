#if !NETFRAMEWORK
using Zonkey.Tests.Infrastructure;

namespace Zonkey.Tests.Integration.Sqlite
{
    public class SqliteBulkOperationTests : BulkOperationTests<SqliteFixture>
    {
        public SqliteBulkOperationTests(SqliteFixture db) : base(db) { }
    }
}
#endif
