#if !NETFRAMEWORK
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Xunit;
using Zonkey.Extensions;
using Zonkey.Tests.Infrastructure;
using Zonkey.Tests.Models;

namespace Zonkey.Tests.Integration
{
    public abstract class ExpressionFilterTests<TFixture> : IClassFixture<TFixture>
        where TFixture : class, IDatabaseFixture
    {
        protected readonly TFixture Db;

        protected ExpressionFilterTests(TFixture db) => Db = db;

        private async Task<List<Animal>> Fill(System.Linq.Expressions.Expression<Func<Animal, bool>> filter)
        {
            using var conn = Db.CreateConnection();
            var adapter = new DataClassAdapter<Animal>(conn);
            var animals = new List<Animal>();
            await adapter.Fill(animals, filter);
            return animals;
        }

        [Fact]
        public async Task CapturedMethodResult_NoLocalVariableNeeded()
        {
            if (!Db.IsAvailable) Assert.Skip(Db.SkipReason);
            var animals = await Fill(a => a.SpeciesId == GetPandaSpeciesId());
            Assert.Equal(2, animals.Count);
        }

        private static int GetPandaSpeciesId() => 1;

        [Fact]
        public async Task Contains_OnIntList()
        {
            if (!Db.IsAvailable) Assert.Skip(Db.SkipReason);
            var ids = new[] { 1 };
            var animals = await Fill(a => ids.Contains(a.SpeciesId));
            Assert.Equal(2, animals.Count);
            Assert.Equal(new[] { "Bao Bao", "Mei Mei" }, animals.Select(a => a.Name).OrderBy(n => n));
        }

        [Fact]
        public async Task Contains_LargeInlinedList()
        {
            if (!Db.IsAvailable) Assert.Skip(Db.SkipReason);
            var ids = Enumerable.Range(1, 100).ToArray();   // > 64 => literal inlining path
            var animals = await Fill(a => ids.Contains(a.SpeciesId));
            Assert.Equal(4, animals.Count);
        }

        [Fact]
        public async Task Contains_EmptyList_ReturnsNoRows()
        {
            if (!Db.IsAvailable) Assert.Skip(Db.SkipReason);
            var ids = Array.Empty<int>();
            var animals = await Fill(a => ids.Contains(a.SpeciesId));
            Assert.Empty(animals);
        }

        [Fact]
        public async Task StringContains_WithPercentInValue_DoesNotWildcard()
        {
            if (!Db.IsAvailable) Assert.Skip(Db.SkipReason);
            var animals = await Fill(a => a.Name.Contains("%"));
            Assert.Empty(animals);   // no animal has a literal % in its name
        }

        [Fact]
        public async Task StartsWithIgnoreCase_MatchesRegardlessOfCase()
        {
            if (!Db.IsAvailable) Assert.Skip(Db.SkipReason);
            var animals = await Fill(a => a.Name.StartsWith("mei", StringComparison.OrdinalIgnoreCase));
            Assert.Single(animals);
            Assert.Equal("Mei Mei", animals[0].Name);
        }

        [Fact]
        public async Task SqlLike_RawPattern()
        {
            if (!Db.IsAvailable) Assert.Skip(Db.SkipReason);
            var animals = await Fill(a => a.Name.SqlLike("Mei%"));
            Assert.Single(animals);
            Assert.Equal("Mei Mei", animals[0].Name);
        }

        [Fact]
        public async Task SubquerySqlIn_ExecutesServerSide()
        {
            if (!Db.IsAvailable) Assert.Skip(Db.SkipReason);
            var animals = await Fill(a => a.ExhibitId.SqlIn((Exhibit e) => e.ExhibitId, e => e.IsOpen));
            Assert.Equal(3, animals.Count);
            Assert.All(animals, a => Assert.NotNull(a.ExhibitId));
        }

        [Fact]
        public async Task DateYear_Filter()
        {
            if (!Db.IsAvailable) Assert.Skip(Db.SkipReason);
            var all = await Fill(a => a.DateOfBirth.HasValue && a.DateOfBirth.Value.Year > 1900);
            Assert.Equal(3, all.Count);
            Assert.All(all, a => Assert.True(a.DateOfBirth.HasValue));
        }

        [Fact]
        public async Task Coalesce_Filter()
        {
            if (!Db.IsAvailable) Assert.Skip(Db.SkipReason);
            var animals = await Fill(a => (a.ExhibitId ?? -1) == -1);
            Assert.Single(animals);   // Bao Bao (null ExhibitId)
            Assert.Equal("Bao Bao", animals[0].Name);
        }
    }
}
#endif
