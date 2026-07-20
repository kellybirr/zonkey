#if !NETFRAMEWORK
using System;
using System.Threading.Tasks;
using Xunit;
using Zonkey.ObjectModel;
using Zonkey.Tests.Infrastructure;
using Zonkey.Tests.Models;

namespace Zonkey.Tests.Integration
{
    public abstract class CrudTests<TFixture> : IClassFixture<TFixture>
        where TFixture : class, IDatabaseFixture
    {
        protected readonly TFixture Db;

        protected CrudTests(TFixture db) => Db = db;

        [Fact]
        public async Task InsertAnimal_AssignsAutoIncrementId()
        {
            if (!Db.IsAvailable) Assert.Skip(Db.SkipReason);

            using var conn = Db.CreateConnection();
            var adapter = new DataClassAdapter<Animal>(conn);

            var animal = new Animal
            {
                Name = "Test Insert",
                SpeciesId = 1,
                ExhibitId = 1,
                ZookeeperId = Guid.Parse("A1B2C3D4-E5F6-7890-ABCD-EF1234567890"),
                Weight = 1.5m
            };

            var saved = await adapter.Save(animal);
            Assert.True(saved);
            Assert.True(animal.AnimalId > 0);

            // Cleanup
            await adapter.DeleteItem(animal);
        }

        [Fact]
        public async Task InsertAnimal_WithDateOfBirth_RoundTrips()
        {
            if (!Db.IsAvailable) Assert.Skip(Db.SkipReason);

            using var conn = Db.CreateConnection();
            var adapter = new DataClassAdapter<Animal>(conn);

            // plain timestamp column: DbType.DateTime2, Kind-Unspecified value
            var born = new DateTime(2022, 3, 4, 10, 30, 0);
            var animal = new Animal
            {
                Name = "Dated",
                SpeciesId = 1,
                ZookeeperId = Guid.Parse("A1B2C3D4-E5F6-7890-ABCD-EF1234567890"),
                DateOfBirth = born
            };

            Assert.True(await adapter.Save(animal));

            var fetched = await adapter.GetOne(a => a.AnimalId == animal.AnimalId);
            Assert.Equal(born, fetched.DateOfBirth);

            await adapter.DeleteItem(fetched);
        }

        [Fact]
        public async Task StubUpdate_Update2_UpdatesOnlyAssignedColumn_WithoutSelect()
        {
            if (!Db.IsAvailable) Assert.Skip(Db.SkipReason);

            using var conn = Db.CreateConnection();
            var adapter = new DataClassAdapter<Animal>(conn);

            var seeded = new Animal
            {
                Name = "Stub Target",
                SpeciesId = 1,
                ZookeeperId = Guid.Parse("A1B2C3D4-E5F6-7890-ABCD-EF1234567890"),
                Weight = 1.00m
            };
            await adapter.Save(seeded);

            // the stub pattern: key while Detached, CommitValues to arm tracking,
            // then assign only the column being changed
            var stub = new Animal(false) { AnimalId = seeded.AnimalId };
            stub.CommitValues();
            stub.Weight = 9.75m;

            Assert.True(await adapter.Update2(stub, UpdateCriteria.KeyOnly, false));

            var reloaded = await adapter.GetOne(a => a.AnimalId == seeded.AnimalId);
            Assert.Equal(9.75m, reloaded.Weight);
            Assert.Equal("Stub Target", reloaded.Name); // untouched columns preserved

            await adapter.DeleteItem(reloaded);
        }

        [Fact]
        public async Task StubUpdate_WithSelectBack_HydratesStub()
        {
            if (!Db.IsAvailable) Assert.Skip(Db.SkipReason);

            using var conn = Db.CreateConnection();
            var adapter = new DataClassAdapter<Animal>(conn);

            var seeded = new Animal
            {
                Name = "Hydrate Target",
                SpeciesId = 2,
                ZookeeperId = Guid.Parse("E5F6A7B8-C9D0-1234-5678-9ABCDEF01234"),
                Weight = 2.00m
            };
            await adapter.Save(seeded);

            var stub = new Animal(false) { AnimalId = seeded.AnimalId };
            stub.CommitValues();
            stub.Weight = 3.50m;

            Assert.True(await adapter.Update2(stub, UpdateCriteria.KeyOnly, true));

            // select-back populated the rest of the stub from the database
            Assert.Equal("Hydrate Target", stub.Name);
            Assert.Equal(2, stub.SpeciesId);

            await adapter.DeleteItem(stub);
        }

        [Fact]
        public async Task InsertZookeeper_WithExplicitGuid()
        {
            if (!Db.IsAvailable) Assert.Skip(Db.SkipReason);

            using var conn = Db.CreateConnection();
            var adapter = new DataClassAdapter<Zookeeper>(conn);

            var keeper = new Zookeeper
            {
                ZookeeperId = Guid.NewGuid(),
                FirstName = "Test",
                LastName = "Keeper",
                HireDate = DateTime.Today
            };

            var saved = await adapter.Save(keeper);
            Assert.True(saved);

            await adapter.DeleteItem(keeper);
        }

        [Fact]
        public async Task GetSingleItem_ByIntKey()
        {
            if (!Db.IsAvailable) Assert.Skip(Db.SkipReason);

            using var conn = Db.CreateConnection();
            var adapter = new DataClassAdapter<Animal>(conn);

            var animal = await adapter.GetOne(a => a.AnimalId == 1);
            Assert.NotNull(animal);
            Assert.Equal("Mei Mei", animal.Name);
        }

        [Fact]
        public async Task GetSingleItem_ByGuidKey()
        {
            if (!Db.IsAvailable) Assert.Skip(Db.SkipReason);

            using var conn = Db.CreateConnection();
            var adapter = new DataClassAdapter<Zookeeper>(conn);

            var id = Guid.Parse("A1B2C3D4-E5F6-7890-ABCD-EF1234567890");
            var keeper = await adapter.GetOne(k => k.ZookeeperId == id);
            Assert.NotNull(keeper);
            Assert.Equal("Jane", keeper.FirstName);
        }

        [Fact]
        public async Task GetSingleItem_ByCompositeKey()
        {
            if (!Db.IsAvailable) Assert.Skip(Db.SkipReason);

            using var conn = Db.CreateConnection();
            var adapter = new DataClassAdapter<FeedingSchedule>(conn);

            var schedule = await adapter.GetOne(s => s.AnimalId == 1 && s.DayOfWeek == 1 && s.TimeSlot == "morning");
            Assert.NotNull(schedule);
            Assert.Equal("Bamboo", schedule.FoodType);
        }

        [Fact]
        public async Task UpdateSingleField_SavesOnlyChangedField()
        {
            if (!Db.IsAvailable) Assert.Skip(Db.SkipReason);

            using var conn = Db.CreateConnection();
            var adapter = new DataClassAdapter<Species>(conn);

            var species = await adapter.GetOne(s => s.SpeciesId == 1);
            var originalName = species.Name;

            species.Name = "Updated Red Panda";
            await adapter.Save(species, UpdateCriteria.ChangedFields);

            // Verify
            var reloaded = await adapter.GetOne(s => s.SpeciesId == 1);
            Assert.Equal("Updated Red Panda", reloaded.Name);

            // Restore
            reloaded.Name = originalName;
            await adapter.Save(reloaded, UpdateCriteria.ChangedFields);
        }

        [Fact]
        public async Task SaveNew_ThenUpdate()
        {
            if (!Db.IsAvailable) Assert.Skip(Db.SkipReason);

            using var conn = Db.CreateConnection();
            var adapter = new DataClassAdapter<Species>(conn);

            // Insert
            var species = new Species { Name = "Test Species", IsEndangered = false };
            await adapter.Save(species);
            Assert.True(species.SpeciesId > 0);

            // Update
            species.IsEndangered = true;
            await adapter.Save(species, UpdateCriteria.ChangedFields);

            // Verify
            var reloaded = await adapter.GetOne(s => s.SpeciesId == species.SpeciesId);
            Assert.True(reloaded.IsEndangered);

            // Cleanup
            await adapter.DeleteItem(species);
        }

        [Fact]
        public async Task DeleteByKey_RemovesRecord()
        {
            if (!Db.IsAvailable) Assert.Skip(Db.SkipReason);

            using var conn = Db.CreateConnection();
            var adapter = new DataClassAdapter<Species>(conn);

            var species = new Species { Name = "To Delete", IsEndangered = false };
            await adapter.Save(species);
            var id = species.SpeciesId;

            await adapter.DeleteItem(species);

            var count = await adapter.GetCount(s => s.SpeciesId == id);
            Assert.Equal(0L, count);
        }

        [Fact]
        public async Task NullField_InsertAndUpdate()
        {
            if (!Db.IsAvailable) Assert.Skip(Db.SkipReason);

            using var conn = Db.CreateConnection();
            var adapter = new DataClassAdapter<Animal>(conn);

            // Insert with null ExhibitId
            var animal = new Animal
            {
                Name = "Null Test",
                SpeciesId = 1,
                ZookeeperId = Guid.Parse("A1B2C3D4-E5F6-7890-ABCD-EF1234567890")
            };
            await adapter.Save(animal);
            Assert.Null(animal.ExhibitId);

            // Update to non-null
            animal.ExhibitId = 1;
            await adapter.Save(animal, UpdateCriteria.ChangedFields);

            var reloaded = await adapter.GetOne(a => a.AnimalId == animal.AnimalId);
            Assert.Equal(1, reloaded.ExhibitId);

            // Update back to null
            reloaded.ExhibitId = null;
            await adapter.Save(reloaded, UpdateCriteria.ChangedFields);

            var reloaded2 = await adapter.GetOne(a => a.AnimalId == animal.AnimalId);
            Assert.Null(reloaded2.ExhibitId);

            // Cleanup
            await adapter.DeleteItem(animal);
        }

        [Fact]
        public async Task RowVersion_ConcurrencyConflict()
        {
            if (!Db.IsAvailable) Assert.Skip(Db.SkipReason);
            if (!Db.SupportsRowVersion) Assert.Skip("Row version not supported by this provider");

            using var conn = Db.CreateConnection();
            var adapter = new DataClassAdapter<Exhibit>(conn);

            // Get two copies of the same exhibit
            var exhibit1 = await adapter.GetOne(e => e.ExhibitId == 1);
            var exhibit2 = await adapter.GetOne(e => e.ExhibitId == 1);

            // Update first copy
            exhibit1.Capacity = 99;
            await adapter.Save(exhibit1, UpdateCriteria.KeyAndVersion);

            // Try to update second copy (stale row version) — should conflict
            exhibit2.Capacity = 50;
            var result = await adapter.TrySave(exhibit2, UpdateCriteria.KeyAndVersion);
            Assert.Equal(SaveResultStatus.Conflict, result.Status);

            // Restore
            exhibit1.Capacity = 5;
            await adapter.Save(exhibit1, UpdateCriteria.ChangedFields);
        }
    }
}
#endif
