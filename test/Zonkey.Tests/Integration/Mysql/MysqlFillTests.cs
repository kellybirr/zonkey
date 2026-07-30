#if !NETFRAMEWORK
using Zonkey.Tests.Infrastructure;

namespace Zonkey.Tests.Integration.Mysql
{
    public class MysqlFillTests : FillTests<MysqlFixture>
    {
        public MysqlFillTests(MysqlFixture db) : base(db) { }
    }
}
#endif
