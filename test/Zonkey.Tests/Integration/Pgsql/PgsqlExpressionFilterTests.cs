#if !NETFRAMEWORK
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Xunit;
using Zonkey.Extensions;
using Zonkey.Tests.Infrastructure;
using Zonkey.Tests.Models;

namespace Zonkey.Tests.Integration.Pgsql
{
    public class PgsqlExpressionFilterTests : ExpressionFilterTests<PgsqlFixture>
    {
        public PgsqlExpressionFilterTests(PgsqlFixture db) : base(db) { }

        [Fact]
        public async Task RegexMatch_CaseSensitive()
        {
            if (!Db.IsAvailable) Assert.Skip(Db.SkipReason);
            using var conn = Db.CreateConnection();
            var adapter = new DataClassAdapter<Animal>(conn);
            var animals = new List<Animal>();
            await adapter.Fill(animals, a => Regex.IsMatch(a.Name, "^Mei"));
            Assert.Single(animals);
        }

        [Fact]
        public async Task RegexMatch_IgnoreCase()
        {
            if (!Db.IsAvailable) Assert.Skip(Db.SkipReason);
            using var conn = Db.CreateConnection();
            var adapter = new DataClassAdapter<Animal>(conn);
            var animals = new List<Animal>();
            await adapter.Fill(animals, a => Regex.IsMatch(a.Name, "^mei", RegexOptions.IgnoreCase));
            Assert.Single(animals);
        }

        [Fact]
        public async Task SqlILike_UsesNativeIlike()
        {
            if (!Db.IsAvailable) Assert.Skip(Db.SkipReason);
            using var conn = Db.CreateConnection();
            var adapter = new DataClassAdapter<Animal>(conn);
            var animals = new List<Animal>();
            await adapter.Fill(animals, a => a.Name.SqlILike("mei%"));
            Assert.Single(animals);
        }

        [Fact]
        public async Task Contains_LargeList_ExecutesViaArrayParameter()
        {
            if (!Db.IsAvailable) Assert.Skip(Db.SkipReason);
            using var conn = Db.CreateConnection();
            var adapter = new DataClassAdapter<Animal>(conn);
            var animals = new List<Animal>();
            var ids = Enumerable.Range(1, 100).ToArray();   // > 64 => array path
            await adapter.Fill(animals, a => ids.Contains(a.SpeciesId));
            Assert.Equal(4, animals.Count);
        }

        [Fact]
        public async Task Contains_LargeGuidList_ExecutesViaUuidArray()
        {
            if (!Db.IsAvailable) Assert.Skip(Db.SkipReason);
            using var conn = Db.CreateConnection();
            var adapter = new DataClassAdapter<Animal>(conn);
            var all = new List<Animal>();
            await adapter.FillAll(all);
            var ids = all.Select(a => a.ZookeeperId).Concat(Enumerable.Range(1, 70).Select(_ => Guid.NewGuid())).ToArray();
            var matched = new List<Animal>();
            await adapter.Fill(matched, a => ids.Contains(a.ZookeeperId));
            Assert.Equal(all.Count, matched.Count);
        }
    }
}
#endif
