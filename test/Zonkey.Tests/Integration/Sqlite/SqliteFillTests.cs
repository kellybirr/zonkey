#if !NETFRAMEWORK
using Zonkey.Tests.Infrastructure;

namespace Zonkey.Tests.Integration.Sqlite
{
    public class SqliteFillTests : FillTests<SqliteFixture>
    {
        public SqliteFillTests(SqliteFixture db) : base(db) { }
    }
}
#endif
