#if !NETFRAMEWORK
using Zonkey.Tests.Infrastructure;

namespace Zonkey.Tests.Integration.Mysql
{
    public class MysqlCrudTests : CrudTests<MysqlFixture>
    {
        public MysqlCrudTests(MysqlFixture db) : base(db) { }
    }
}
#endif
