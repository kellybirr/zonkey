#if !NETFRAMEWORK
using Zonkey.Tests.Infrastructure;

namespace Zonkey.Tests.Integration.Mysql
{
    public class MysqlTransactionTests : TransactionTests<MysqlFixture>
    {
        public MysqlTransactionTests(MysqlFixture db) : base(db) { }
    }
}
#endif
