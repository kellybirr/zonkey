#if !NETFRAMEWORK
using System.Collections.Generic;
using System.Threading.Tasks;
using Xunit;
using Zonkey.ObjectModel;
using Zonkey.Tests.Infrastructure;
using Zonkey.Tests.Models;

namespace Zonkey.Tests.Integration
{
    public abstract class FillTests<TFixture> : IClassFixture<TFixture>
        where TFixture : class, IDatabaseFixture
    {
        protected readonly TFixture Db;

        protected FillTests(TFixture db) => Db = db;

        [Fact]
        public async Task FillAll_ReturnsAllSeededAnimals()
        {
            if (!Db.IsAvailable) Assert.Skip(Db.SkipReason);

            using var conn = Db.CreateConnection();
            var adapter = new DataClassAdapter<Animal>(conn);
            var animals = new List<Animal>();

            var count = await adapter.FillAll(animals);
            Assert.Equal(4, count);
        }

        [Fact]
        public async Task Fill_WithLinqExpression()
        {
            if (!Db.IsAvailable) Assert.Skip(Db.SkipReason);

            using var conn = Db.CreateConnection();
            var adapter = new DataClassAdapter<Animal>(conn);
            var animals = new List<Animal>();

            await adapter.Fill(animals, a => a.SpeciesId == 1);
            Assert.Equal(2, animals.Count); // Mei Mei and Bao Bao
        }

        [Fact]
        public async Task Fill_WithSqlFilter()
        {
            if (!Db.IsAvailable) Assert.Skip(Db.SkipReason);

            using var conn = Db.CreateConnection();
            var adapter = new DataClassAdapter<Animal>(conn);
            var animals = new List<Animal>();

            await adapter.Fill(animals, SqlFilter.EQ("SpeciesId", 1));
            Assert.Equal(2, animals.Count);
        }

        [Fact]
        public async Task Fill_WithStringFilter()
        {
            if (!Db.IsAvailable) Assert.Skip(Db.SkipReason);

            using var conn = Db.CreateConnection();
            var adapter = new DataClassAdapter<Animal>(conn);
            var animals = new List<Animal>();

            await adapter.Fill(animals, "SpeciesId = $0", 1);
            Assert.Equal(2, animals.Count);
        }

        [Fact]
        public async Task Fill_WithNullFilter()
        {
            if (!Db.IsAvailable) Assert.Skip(Db.SkipReason);

            using var conn = Db.CreateConnection();
            var adapter = new DataClassAdapter<Animal>(conn);
            var animals = new List<Animal>();

            await adapter.Fill(animals, a => a.ExhibitId == null);
            Assert.Single(animals); // Bao Bao
            Assert.Equal("Bao Bao", animals[0].Name);
        }

        [Fact]
        public async Task Fill_WithBooleanFilter()
        {
            if (!Db.IsAvailable) Assert.Skip(Db.SkipReason);

            using var conn = Db.CreateConnection();
            var adapter = new DataClassAdapter<Species>(conn);
            var species = new List<Species>();

            await adapter.Fill(species, s => s.IsEndangered);
            Assert.Equal(2, species.Count); // Red Panda, Axolotl
        }

        [Fact]
        public async Task Fill_WithCompoundFilter()
        {
            if (!Db.IsAvailable) Assert.Skip(Db.SkipReason);

            using var conn = Db.CreateConnection();
            var adapter = new DataClassAdapter<Animal>(conn);
            var animals = new List<Animal>();

            await adapter.Fill(animals, a => a.SpeciesId == 1 && a.Weight > 5.0m);
            Assert.Single(animals); // Mei Mei (5.50)
        }

        [Fact]
        public async Task GetCount_WithFilter()
        {
            if (!Db.IsAvailable) Assert.Skip(Db.SkipReason);

            using var conn = Db.CreateConnection();
            var adapter = new DataClassAdapter<Animal>(conn);

            var count = await adapter.GetCount(a => a.SpeciesId == 1);
            Assert.Equal(2L, count);
        }

        [Fact]
        public async Task GetCount_Matching_ReturnsNonZero()
        {
            if (!Db.IsAvailable) Assert.Skip(Db.SkipReason);

            using var conn = Db.CreateConnection();
            var adapter = new DataClassAdapter<Animal>(conn);

            var count = await adapter.GetCount(a => a.Name == "Mei Mei");
            Assert.True(count > 0);
        }

        [Fact]
        public async Task GetCount_NonMatching_ReturnsZero()
        {
            if (!Db.IsAvailable) Assert.Skip(Db.SkipReason);

            using var conn = Db.CreateConnection();
            var adapter = new DataClassAdapter<Animal>(conn);

            var count = await adapter.GetCount(a => a.Name == "NonExistent");
            Assert.Equal(0L, count);
        }
    }
}
#endif
