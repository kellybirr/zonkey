#if !NETFRAMEWORK
using System.Collections.Generic;
using System.Threading.Tasks;
using Xunit;
using Zonkey.ObjectModel;
using Zonkey.Tests.Infrastructure;
using Zonkey.Tests.Models;

namespace Zonkey.Tests.Integration
{
    public abstract class BulkOperationTests<TFixture> : IClassFixture<TFixture>
        where TFixture : class, IDatabaseFixture
    {
        protected readonly TFixture Db;

        protected BulkOperationTests(TFixture db) => Db = db;

        [Fact]
        public async Task BulkInsert_InsertsMultipleRecords()
        {
            if (!Db.IsAvailable) Assert.Skip(Db.SkipReason);

            using var conn = Db.CreateConnection();
            var adapter = new DataClassAdapter<Species>(conn);

            var newSpecies = new List<Species>
            {
                new Species { Name = "Bulk Species 1", IsEndangered = false },
                new Species { Name = "Bulk Species 2", IsEndangered = true }
            };

            var inserted = await adapter.BulkInsert(newSpecies);
            Assert.Equal(2, inserted);

            // Cleanup
            foreach (var s in newSpecies)
                await adapter.Delete(x => x.Name == s.Name);
        }
    }
}
#endif
