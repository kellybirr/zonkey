#if !NETFRAMEWORK
using System.Data;
using System.Threading.Tasks;
using Xunit;
using Zonkey.Mocks;
using Zonkey.ObjectModel;

namespace Zonkey.Tests.Unit
{
    /// <summary>
    /// On .NET 8+ targets DatabaseWrapper implements IAsyncDisposable itself
    /// (like DataClassReader), so subclasses get 'await using' without boilerplate.
    /// </summary>
    public class DatabaseWrapperDisposeTests
    {
        private class TestDb : DatabaseWrapper
        {
            public TestDb(MockDbConnection conn) : base(conn) { }
        }

        [Fact]
        public async Task AwaitUsing_DisposesConnection()
        {
            var conn = new MockDbConnection();
            conn.Open();

            await using (var db = new TestDb(conn))
            {
                Assert.Equal(ConnectionState.Open, db.Connection.State);
            }

            Assert.Equal(ConnectionState.Closed, conn.State);
        }

        [Fact]
        public async Task DisposeAsync_IsIdempotent()
        {
            var conn = new MockDbConnection();
            conn.Open();

            var db = new TestDb(conn);
            await db.DisposeAsync();
            await db.DisposeAsync(); // second call must not throw

            Assert.Equal(ConnectionState.Closed, conn.State);
        }

        private class DisposeTrackingDb : DatabaseWrapper
        {
            public bool DisposeBoolCalled { get; private set; }

            public DisposeTrackingDb(MockDbConnection conn) : base(conn) { }

            protected override void Dispose(bool disposing)
            {
                DisposeBoolCalled = true;
                base.Dispose(disposing);
            }
        }

        [Fact]
        public async Task DisposeAsync_InvokesVirtualDisposeBool()
        {
            // Regression for C2: DisposeAsync used to inline its own cleanup without ever
            // calling the virtual Dispose(bool), so subclasses overriding Dispose(bool) to
            // release their own resources would leak on the `await using` path.
            var conn = new MockDbConnection();
            conn.Open();

            var db = new DisposeTrackingDb(conn);
            await using (db)
            {
                Assert.False(db.DisposeBoolCalled);
            }

            Assert.True(db.DisposeBoolCalled);
            Assert.Equal(ConnectionState.Closed, conn.State);
        }
    }
}
#endif
