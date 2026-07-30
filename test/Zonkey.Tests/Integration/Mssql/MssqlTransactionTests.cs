#if !NETFRAMEWORK
using Zonkey.Tests.Infrastructure;

namespace Zonkey.Tests.Integration.Mssql
{
    public class MssqlTransactionTests : TransactionTests<MssqlFixture>
    {
        public MssqlTransactionTests(MssqlFixture db) : base(db) { }
    }
}
#endif
