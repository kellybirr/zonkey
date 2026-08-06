using Xunit;

namespace Zonkey.Scaffold.Tests.Infrastructure;

public interface IScaffoldFixture : IAsyncLifetime
{
    bool IsAvailable { get; }
    string SkipReason { get; }
    string ConnectionString { get; }
    string Provider { get; }
    string DefaultSchema { get; }
}
