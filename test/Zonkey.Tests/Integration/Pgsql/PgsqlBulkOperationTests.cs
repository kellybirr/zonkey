#if !NETFRAMEWORK
using Zonkey.Tests.Infrastructure;

namespace Zonkey.Tests.Integration.Pgsql
{
    public class PgsqlBulkOperationTests : BulkOperationTests<PgsqlFixture>
    {
        public PgsqlBulkOperationTests(PgsqlFixture db) : base(db) { }
    }
}
#endif
