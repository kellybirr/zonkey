#if !NETFRAMEWORK
using Zonkey.Tests.Infrastructure;

namespace Zonkey.Tests.Integration.Mysql
{
    public class MysqlRecordsetUpdateTests : RecordsetUpdateTests<MysqlFixture>
    {
        public MysqlRecordsetUpdateTests(MysqlFixture db) : base(db) { }
    }
}
#endif
