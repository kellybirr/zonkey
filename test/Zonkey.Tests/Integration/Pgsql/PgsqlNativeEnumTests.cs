#if !NETFRAMEWORK
using System.Data;
using System.Threading.Tasks;
using Npgsql;
using Xunit;
using Zonkey;
using Zonkey.ObjectModel;
using Zonkey.Tests.Infrastructure;

namespace Zonkey.Tests.Integration.Pgsql
{
    /// <summary>
    /// Native PostgreSQL enum columns work with Zonkey when the enum is mapped at the
    /// Npgsql level (NpgsqlDataSourceBuilder.MapEnum) -- the same requirement EF Core
    /// has. The mapped provider surfaces the C# enum type directly; Zonkey fills it on
    /// both materialization paths and writes it with a plain DbType.Object declaration.
    /// Unmapped connections cannot even read native enum columns on modern Npgsql.
    /// </summary>
    public class PgsqlNativeEnumTests : IClassFixture<PgsqlFixture>
    {
        private readonly PgsqlFixture _db;

        public PgsqlNativeEnumTests(PgsqlFixture db) => _db = db;

        public enum Habitat { Forest = 1, Aquatic = 2, Desert = 3 }

        [DataItem("enum_zone")]
        public class EnumZone : DataClass
        {
            public EnumZone() : base(false) { }
            public EnumZone(bool addingNew) : base(addingNew) { }

            [DataField("id", DbType.Int32, IsKeyField = true, IsAutoIncrement = true)]
            public int Id { get => field; set => SetFieldValue(ref field, value); }

            [DataField("kind", DbType.Object, true)]
            public Habitat? Kind { get => field; set => SetFieldValue(ref field, value); }
        }

        /// <summary>Creates the enum type/table and returns a MapEnum'd connection.</summary>
        private async Task<NpgsqlConnection> OpenMappedConnection()
        {
            string connString;
            using (var setup = _db.CreateConnection())
            {
                connString = $"{TestConfiguration.PgsqlConnectionString};Database={setup.Database}";
                using var ddl = setup.CreateCommand();
                ddl.CommandText = @"
                    DO $$ BEGIN
                        IF NOT EXISTS (SELECT 1 FROM pg_type WHERE typname = 'habitat_kind') THEN
                            CREATE TYPE habitat_kind AS ENUM ('Forest', 'Aquatic', 'Desert');
                        END IF;
                    END $$;
                    CREATE TABLE IF NOT EXISTS enum_zone (id SERIAL PRIMARY KEY, kind habitat_kind);";
                await ddl.ExecuteNonQueryAsync();
            }

            var builder = new NpgsqlDataSourceBuilder(connString);
            // labels match the C# member names exactly, so no name translation;
            // MapEnum's default translator is snake_case ('Forest' -> 'forest')
            builder.MapEnum<Habitat>("habitat_kind", new Npgsql.NameTranslation.NpgsqlNullNameTranslator());
            var dataSource = builder.Build();
            return await dataSource.OpenConnectionAsync();
        }

        [Fact]
        public async Task MappedEnum_SaveAndReload_RoundTrips()
        {
            if (!_db.IsAvailable) Assert.Skip(_db.SkipReason);

            using var conn = await OpenMappedConnection();
            var adapter = new DataClassAdapter<EnumZone>(conn);

            var zone = new EnumZone(addingNew: true) { Kind = Habitat.Desert };
            Assert.True(await adapter.Save(zone));
            Assert.True(zone.Id > 0);

            var back = await adapter.GetOne(z => z.Id == zone.Id);
            Assert.Equal(Habitat.Desert, back.Kind);

            await adapter.DeleteItem(back);
        }

        [Fact]
        public async Task MappedEnum_FillsOnBothMaterializationPaths()
        {
            if (!_db.IsAvailable) Assert.Skip(_db.SkipReason);

            using var conn = await OpenMappedConnection();
            var adapter = new DataClassAdapter<EnumZone>(conn);

            var zone = new EnumZone(addingNew: true) { Kind = Habitat.Aquatic };
            await adapter.Save(zone);

            foreach (bool fast in new[] { true, false })
            {
                using var reader = await adapter.OpenReader(z => z.Id == zone.Id);
                reader.UseFastBuilder = fast;
                var read = await reader.ReadAsync();
                Assert.Equal(Habitat.Aquatic, read.Kind);
            }

            await adapter.DeleteItem(zone);
        }

        [Fact]
        public async Task MappedEnum_FiltersInWhereClause()
        {
            if (!_db.IsAvailable) Assert.Skip(_db.SkipReason);

            using var conn = await OpenMappedConnection();
            var adapter = new DataClassAdapter<EnumZone>(conn);

            var forest = new EnumZone(addingNew: true) { Kind = Habitat.Forest };
            var desert = new EnumZone(addingNew: true) { Kind = Habitat.Desert };
            await adapter.Save(forest);
            await adapter.Save(desert);

            var found = await adapter.GetOne(z => z.Kind == Habitat.Forest && z.Id >= forest.Id);
            Assert.NotNull(found);
            Assert.Equal(forest.Id, found.Id);

            await adapter.DeleteItem(forest);
            await adapter.DeleteItem(desert);
        }

        [Fact]
        public async Task MappedEnum_NullRoundTrips()
        {
            if (!_db.IsAvailable) Assert.Skip(_db.SkipReason);

            using var conn = await OpenMappedConnection();
            var adapter = new DataClassAdapter<EnumZone>(conn);

            var zone = new EnumZone(addingNew: true); // Kind null
            Assert.True(await adapter.Save(zone));

            var back = await adapter.GetOne(z => z.Id == zone.Id);
            Assert.Null(back.Kind);

            await adapter.DeleteItem(back);
        }
    }
}
#endif
