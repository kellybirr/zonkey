#if !NETFRAMEWORK
using Zonkey.Tests.Infrastructure;

namespace Zonkey.Tests.Integration.Pgsql
{
    public class PgsqlDataTableAdapterTests : DataTableAdapterTests<PgsqlFixture>
    {
        public PgsqlDataTableAdapterTests(PgsqlFixture db) : base(db) { }

        // PostgreSQL accepts a SELECT statement with an empty projection
        protected override bool EmptyProjectionThrows => false;
    }
}
#endif
