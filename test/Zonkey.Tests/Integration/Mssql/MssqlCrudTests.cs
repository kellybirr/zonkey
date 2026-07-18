#if !NETFRAMEWORK
using Zonkey.Tests.Infrastructure;

namespace Zonkey.Tests.Integration.Mssql
{
    public class MssqlCrudTests : CrudTests<MssqlFixture>
    {
        public MssqlCrudTests(MssqlFixture db) : base(db) { }
    }
}
#endif
