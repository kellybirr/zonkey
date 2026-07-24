#if !NETFRAMEWORK
using Zonkey.Tests.Infrastructure;

namespace Zonkey.Tests.Integration.Mssql
{
    public class MssqlRecordsetUpdateTests : RecordsetUpdateTests<MssqlFixture>
    {
        public MssqlRecordsetUpdateTests(MssqlFixture db) : base(db) { }
    }
}
#endif
