#if !NETFRAMEWORK
using Zonkey.Tests.Infrastructure;

namespace Zonkey.Tests.Integration.Pgsql
{
    public class PgsqlFillTests : FillTests<PgsqlFixture>
    {
        public PgsqlFillTests(PgsqlFixture db) : base(db) { }
    }
}
#endif
