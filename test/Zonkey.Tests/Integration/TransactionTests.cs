#if !NETFRAMEWORK
using System.Threading.Tasks;
using Xunit;
using Zonkey.ObjectModel;
using Zonkey.Tests.Infrastructure;
using Zonkey.Tests.Models;

namespace Zonkey.Tests.Integration
{
    public abstract class TransactionTests<TFixture> : IClassFixture<TFixture>
        where TFixture : class, IDatabaseFixture
    {
        protected readonly TFixture Db;

        protected TransactionTests(TFixture db) => Db = db;

        [Fact]
        public async Task Transaction_Commit_PersistsData()
        {
            if (!Db.IsAvailable) Assert.Skip(Db.SkipReason);

            using var conn = Db.CreateConnection();
            var adapter = new DataClassAdapter<Species>(conn);

            int insertedId;
            using (var trx = conn.BeginTransaction())
            {
                adapter.Transaction = trx;
                var species = new Species { Name = "Committed Species", IsEndangered = false };
                await adapter.Save(species);
                insertedId = species.SpeciesId;
                trx.Commit();
            }

            adapter.Transaction = null;
            var count = await adapter.GetCount(s => s.SpeciesId == insertedId);
            Assert.True(count > 0);

            // Cleanup
            await adapter.Delete(s => s.SpeciesId == insertedId);
        }

        [Fact]
        public async Task Transaction_Rollback_DiscardsData()
        {
            if (!Db.IsAvailable) Assert.Skip(Db.SkipReason);

            using var conn = Db.CreateConnection();
            var adapter = new DataClassAdapter<Species>(conn);

            int insertedId;
            using (var trx = conn.BeginTransaction())
            {
                adapter.Transaction = trx;
                var species = new Species { Name = "Rolled Back Species", IsEndangered = false };
                await adapter.Save(species);
                insertedId = species.SpeciesId;
                trx.Rollback();
            }

            adapter.Transaction = null;
            var count = await adapter.GetCount(s => s.SpeciesId == insertedId);
            Assert.Equal(0L, count);
        }
    }
}
#endif
