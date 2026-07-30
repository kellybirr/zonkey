using System;
using System.Data;
using System.Data.Common;
using Xunit;
using Zonkey.Dialects;

// Fake connection types living in "telling" namespaces, standing in for real driver assemblies
// that aren't referenced by this test project. Abstract DbConnection members that Create() never
// touches are left throwing NotImplementedException.
namespace MariaDB.Data
{
    public class MariaDbConnection : DbConnection
    {
        public override string ConnectionString { get; set; }
        public override string Database => throw new NotImplementedException();
        public override string DataSource => throw new NotImplementedException();
        public override string ServerVersion => throw new NotImplementedException();
        public override ConnectionState State => throw new NotImplementedException();
        public override void ChangeDatabase(string databaseName) => throw new NotImplementedException();
        public override void Close() => throw new NotImplementedException();
        public override void Open() => throw new NotImplementedException();
        protected override DbTransaction BeginDbTransaction(IsolationLevel isolationLevel) => throw new NotImplementedException();
        protected override DbCommand CreateDbCommand() => throw new NotImplementedException();
    }
}

namespace Some.Future.Provider
{
    public class MySqlFlavorConnection : DbConnection
    {
        public override string ConnectionString { get; set; }
        public override string Database => throw new NotImplementedException();
        public override string DataSource => throw new NotImplementedException();
        public override string ServerVersion => throw new NotImplementedException();
        public override ConnectionState State => throw new NotImplementedException();
        public override void ChangeDatabase(string databaseName) => throw new NotImplementedException();
        public override void Close() => throw new NotImplementedException();
        public override void Open() => throw new NotImplementedException();
        protected override DbTransaction BeginDbTransaction(IsolationLevel isolationLevel) => throw new NotImplementedException();
        protected override DbCommand CreateDbCommand() => throw new NotImplementedException();
    }
}

namespace Acme
{
    public class FooConnection : DbConnection
    {
        public override string ConnectionString { get; set; }
        public override string Database => throw new NotImplementedException();
        public override string DataSource => throw new NotImplementedException();
        public override string ServerVersion => throw new NotImplementedException();
        public override ConnectionState State => throw new NotImplementedException();
        public override void ChangeDatabase(string databaseName) => throw new NotImplementedException();
        public override void Close() => throw new NotImplementedException();
        public override void Open() => throw new NotImplementedException();
        protected override DbTransaction BeginDbTransaction(IsolationLevel isolationLevel) => throw new NotImplementedException();
        protected override DbCommand CreateDbCommand() => throw new NotImplementedException();
    }
}

namespace Zonkey.Tests.Unit
{
    /// <summary>
    /// SqlDialect.Create() maps connection types to dialects by EXACT FullName match against the
    /// Factories registry (populated in the static constructor), then falls through to
    /// GenericSqlDialect for anything unrecognized — deliberately no partial/pattern matching.
    /// MariaDB is covered by exact registry entries: its mainstream drivers are the MySql
    /// connector types, plus a dedicated "MariaDB.Data.MariaDbConnection" entry. Unregistered
    /// drivers are the consumer's job via the public Factories dictionary.
    /// </summary>
    public class DialectFactoryTests
    {
        [Fact]
        public void MariaDbConnection_ResolvesToMySqlDialect_ViaExactRegistryEntry()
        {
            var conn = new MariaDB.Data.MariaDbConnection();
            Assert.IsType<MySqlDialect>(SqlDialect.Create(conn));
        }

        [Fact]
        public void UnregisteredMySqlFlavoredConnection_FallsThroughToGeneric()
        {
            // exact-match only: a name merely CONTAINING "MySql" is not recognized
            var conn = new Some.Future.Provider.MySqlFlavorConnection();
            Assert.IsType<GenericSqlDialect>(SqlDialect.Create(conn));
        }

        [Fact]
        public void ConsumerCanRegisterExactTypeName_ViaPublicFactories()
        {
            string typeName = typeof(Some.Future.Provider.MySqlFlavorConnection).FullName;
            SqlDialect.Factories[typeName] = _ => new MySqlDialect();
            try
            {
                var conn = new Some.Future.Provider.MySqlFlavorConnection();
                Assert.IsType<MySqlDialect>(SqlDialect.Create(conn));
            }
            finally
            {
                SqlDialect.Factories.Remove(typeName);
            }
        }

        [Fact]
        public void UnrelatedConnection_StillResolvesToGenericSqlDialect()
        {
            var conn = new Acme.FooConnection();
            Assert.IsType<GenericSqlDialect>(SqlDialect.Create(conn));
        }

        [Fact]
        public void NullConnection_ReturnsNull()
        {
            Assert.Null(SqlDialect.Create(null));
        }
    }
}
