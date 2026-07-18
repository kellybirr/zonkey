#if !NETFRAMEWORK
using Zonkey.Tests.Infrastructure;

namespace Zonkey.Tests.Integration.Pgsql
{
    public class PgsqlCrudTests : CrudTests<PgsqlFixture>
    {
        public PgsqlCrudTests(PgsqlFixture db) : base(db) { }
    }
}
#endif
