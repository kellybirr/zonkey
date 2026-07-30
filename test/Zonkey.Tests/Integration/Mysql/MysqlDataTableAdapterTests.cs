#if !NETFRAMEWORK
using Zonkey.Tests.Infrastructure;

namespace Zonkey.Tests.Integration.Mysql
{
    public class MysqlDataTableAdapterTests : DataTableAdapterTests<MysqlFixture>
    {
        public MysqlDataTableAdapterTests(MysqlFixture db) : base(db) { }
    }
}
#endif
