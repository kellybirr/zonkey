#if !NETFRAMEWORK
using Zonkey.Tests.Infrastructure;

namespace Zonkey.Tests.Integration.Mssql
{
    public class MssqlBulkOperationTests : BulkOperationTests<MssqlFixture>
    {
        public MssqlBulkOperationTests(MssqlFixture db) : base(db) { }
    }
}
#endif
