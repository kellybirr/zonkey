#if !NETFRAMEWORK
using Zonkey.Tests.Infrastructure;

namespace Zonkey.Tests.Integration.Pgsql
{
    public class PgsqlRecordsetUpdateTests : RecordsetUpdateTests<PgsqlFixture>
    {
        public PgsqlRecordsetUpdateTests(PgsqlFixture db) : base(db) { }
    }
}
#endif
