using System.Linq;
using Xunit;
using Zonkey.ObjectModel;
using Zonkey.Tests.Models;

namespace Zonkey.Tests.Unit
{
    public class DataMapTests
    {
        [Fact]
        public void GenerateNew_CreatesMapFromAttributedClass()
        {
            var map = DataMap.GenerateNew(typeof(Animal));
            Assert.NotNull(map);
            Assert.NotEmpty(map.DataFields);
        }

        [Fact]
        public void GenerateNew_DiscoversAllFields()
        {
            var map = DataMap.GenerateNew(typeof(Animal));
            Assert.Equal(8, map.DataFields.Count);
        }

        [Fact]
        public void GenerateNew_IdentifiesSingleKeyField()
        {
            var map = DataMap.GenerateNew(typeof(Animal));
            Assert.Single(map.KeyFields);
            Assert.Equal("AnimalId", map.KeyFields[0].FieldName);
        }

        [Fact]
        public void GenerateNew_IdentifiesCompositeKey()
        {
            var map = DataMap.GenerateNew(typeof(FeedingSchedule));
            Assert.Equal(3, map.KeyFields.Count);
            var keyNames = map.KeyFields.Select(k => k.FieldName).ToList();
            Assert.Contains("AnimalId", keyNames);
            Assert.Contains("DayOfWeek", keyNames);
            Assert.Contains("TimeSlot", keyNames);
        }

        [Fact]
        public void GenerateNew_IdentifiesGuidKey()
        {
            var map = DataMap.GenerateNew(typeof(Zookeeper));
            Assert.Single(map.KeyFields);
            Assert.Equal("ZookeeperId", map.KeyFields[0].FieldName);
            Assert.Equal(System.Data.DbType.Guid, map.KeyFields[0].DataType);
        }

        [Fact]
        public void GenerateNew_DetectsAutoIncrement()
        {
            var map = DataMap.GenerateNew(typeof(Animal));
            var keyField = map.KeyFields[0];
            Assert.True(keyField.IsAutoIncrement);
        }

        [Fact]
        public void GenerateNew_DetectsRowVersion()
        {
            var map = DataMap.GenerateNew(typeof(Exhibit));
            var rvField = map.DataFields.FirstOrDefault(f => f.FieldName == "RowVersion");
            Assert.NotNull(rvField);
            Assert.True(rvField.IsRowVersion);
        }

        [Fact]
        public void GenerateNew_DetectsNullableFields()
        {
            var map = DataMap.GenerateNew(typeof(Animal));
            var exhibitField = map.DataFields.First(f => f.FieldName == "ExhibitId");
            Assert.True(exhibitField.IsNullable);

            var nameField = map.DataFields.First(f => f.FieldName == "Name");
            Assert.False(nameField.IsNullable);
        }

        [Fact]
        public void GetReadableField_FindsByName()
        {
            var map = DataMap.GenerateNew(typeof(Animal));
            var field = map.GetReadableField("Name");
            Assert.NotNull(field);
            Assert.Equal("Name", field.FieldName);
        }

        [Fact]
        public void GetReadableField_ReturnsNull_ForUnknownField()
        {
            var map = DataMap.GenerateNew(typeof(Animal));
            var field = map.GetReadableField("NonExistent");
            Assert.Null(field);
        }

        [Fact]
        public void ContainsField_ReturnsTrueForExistingField()
        {
            var map = DataMap.GenerateNew(typeof(Animal));
            Assert.True(map.ContainsField("Name"));
        }

        [Fact]
        public void ContainsField_ReturnsFalseForMissingField()
        {
            var map = DataMap.GenerateNew(typeof(Animal));
            Assert.False(map.ContainsField("NonExistent"));
        }

        [Fact]
        public void GenerateCached_ReturnsSameInstance()
        {
            var map1 = DataMap.GenerateCached(typeof(Species));
            var map2 = DataMap.GenerateCached(typeof(Species));
            Assert.Same(map1, map2);
        }

        [Fact]
        public void ReadableFields_ExcludesWriteOnlyFields()
        {
            var map = DataMap.GenerateNew(typeof(Animal));
            Assert.Equal(map.DataFields.Count, map.ReadableFields.Count);
        }

        [Fact]
        public void IsComparable_False_ForNotesField()
        {
            var map = DataMap.GenerateNew(typeof(Animal));
            var notesField = map.DataFields.First(f => f.FieldName == "Notes");
            Assert.False(notesField.IsComparable);
        }
    }
}
