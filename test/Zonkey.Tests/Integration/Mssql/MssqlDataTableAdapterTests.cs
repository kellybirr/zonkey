#if !NETFRAMEWORK
using Zonkey.Tests.Infrastructure;

namespace Zonkey.Tests.Integration.Mssql
{
    public class MssqlDataTableAdapterTests : DataTableAdapterTests<MssqlFixture>
    {
        public MssqlDataTableAdapterTests(MssqlFixture db) : base(db) { }
    }
}
#endif
