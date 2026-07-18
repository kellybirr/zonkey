#if !NETFRAMEWORK
using Zonkey.Tests.Infrastructure;

namespace Zonkey.Tests.Integration.Pgsql
{
    public class PgsqlTransactionTests : TransactionTests<PgsqlFixture>
    {
        public PgsqlTransactionTests(PgsqlFixture db) : base(db) { }
    }
}
#endif
