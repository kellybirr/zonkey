#if !NETFRAMEWORK
using Zonkey.Tests.Infrastructure;

namespace Zonkey.Tests.Integration.Sqlite
{
    public class SqliteCrudTests : CrudTests<SqliteFixture>
    {
        public SqliteCrudTests(SqliteFixture db) : base(db) { }
    }
}
#endif
