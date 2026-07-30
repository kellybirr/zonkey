#if !NETFRAMEWORK
using Zonkey.Tests.Infrastructure;

namespace Zonkey.Tests.Integration.Mssql
{
    public class MssqlExpressionFilterTests : ExpressionFilterTests<MssqlFixture>
    {
        public MssqlExpressionFilterTests(MssqlFixture db) : base(db) { }
    }
}
#endif
