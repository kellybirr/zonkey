#if !NETFRAMEWORK
using Zonkey.Tests.Infrastructure;

namespace Zonkey.Tests.Integration.Sqlite
{
    public class SqliteTransactionTests : TransactionTests<SqliteFixture>
    {
        public SqliteTransactionTests(SqliteFixture db) : base(db) { }
    }
}
#endif
