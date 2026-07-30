#if !NETFRAMEWORK
using Zonkey.Tests.Infrastructure;

namespace Zonkey.Tests.Integration.Mysql
{
    public class MysqlBulkOperationTests : BulkOperationTests<MysqlFixture>
    {
        public MysqlBulkOperationTests(MysqlFixture db) : base(db) { }
    }
}
#endif
