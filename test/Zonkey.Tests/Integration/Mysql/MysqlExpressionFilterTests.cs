#if !NETFRAMEWORK
using Zonkey.Tests.Infrastructure;

namespace Zonkey.Tests.Integration.Mysql
{
    public class MysqlExpressionFilterTests : ExpressionFilterTests<MysqlFixture>
    {
        public MysqlExpressionFilterTests(MysqlFixture db) : base(db) { }
    }
}
#endif
