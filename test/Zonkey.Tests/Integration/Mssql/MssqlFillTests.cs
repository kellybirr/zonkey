#if !NETFRAMEWORK
using Zonkey.Tests.Infrastructure;

namespace Zonkey.Tests.Integration.Mssql
{
    public class MssqlFillTests : FillTests<MssqlFixture>
    {
        public MssqlFillTests(MssqlFixture db) : base(db) { }
    }
}
#endif
