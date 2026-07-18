using System.Data;
using Xunit;
using Zonkey.Tests.Models;

namespace Zonkey.Tests.Unit
{
    public class DataClassTests
    {
        [Fact]
        public void NewObject_StartsAsAdded()
        {
            var animal = new Animal();
            Assert.Equal(DataRowState.Added, animal.DataRowState);
        }

        [Fact]
        public void ObjectCreatedAsNotNew_StartsAsDetached()
        {
            var animal = new Animal(false);
            Assert.Equal(DataRowState.Detached, animal.DataRowState);
        }

        [Fact]
        public void CommitValues_TransitionsAddedToUnchanged()
        {
            var animal = new Animal();
            animal.Name = "Test";
            animal.CommitValues();
            Assert.Equal(DataRowState.Unchanged, animal.DataRowState);
        }

        [Fact]
        public void SetField_OnUnchanged_TransitionsToModified()
        {
            var animal = new Animal { Name = "Original" };
            animal.CommitValues();

            animal.Name = "Updated";
            Assert.Equal(DataRowState.Modified, animal.DataRowState);
        }

        [Fact]
        public void SetField_TracksOriginalValue()
        {
            var animal = new Animal { Name = "Original" };
            animal.CommitValues();

            animal.Name = "Updated";
            Assert.True(animal.OriginalValues.ContainsKey("Name"));
        }

        [Fact]
        public void CommitValues_ResetsModifiedToUnchanged()
        {
            var animal = new Animal { Name = "Original" };
            animal.CommitValues();

            animal.Name = "Changed";
            Assert.Equal(DataRowState.Modified, animal.DataRowState);

            animal.CommitValues();
            Assert.Equal(DataRowState.Unchanged, animal.DataRowState);
        }

        [Fact]
        public void CommitValues_ClearsOriginalValues()
        {
            var animal = new Animal { Name = "Original" };
            animal.CommitValues();

            animal.Name = "Updated";
            Assert.NotEmpty(animal.OriginalValues);

            animal.CommitValues();
            Assert.Empty(animal.OriginalValues);
        }

        [Fact]
        public void MultipleFieldChanges_TrackedIndependently()
        {
            var animal = new Animal { Name = "Orig", SpeciesId = 1 };
            animal.CommitValues();

            animal.Name = "New Name";
            animal.SpeciesId = 2;

            Assert.True(animal.OriginalValues.ContainsKey("Name"));
            Assert.True(animal.OriginalValues.ContainsKey("SpeciesId"));
        }

        [Fact]
        public void SetField_OnAdded_StaysAdded()
        {
            var animal = new Animal();
            animal.Name = "Test";
            Assert.Equal(DataRowState.Added, animal.DataRowState);
        }
    }
}
