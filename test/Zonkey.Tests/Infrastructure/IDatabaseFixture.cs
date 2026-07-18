using System.Data.Common;
using Xunit;
using Zonkey.Dialects;

namespace Zonkey.Tests.Infrastructure
{
    public interface IDatabaseFixture : IAsyncLifetime
    {
        bool IsAvailable { get; }
        string SkipReason { get; }
        SqlDialect Dialect { get; }
        bool SupportsRowVersion { get; }
        DbConnection CreateConnection();
    }
}
