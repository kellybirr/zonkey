using ZonkeyCodeGen.CodeGen;

namespace ZonkeyCodeGen.Utilities
{
    internal static class SupportData
    {
        public static ListItem[] GetCollectionTypes()
        {
            return new[] {
                             new ListItem(GenerateCollectionMode.None, "None"),
                             new ListItem(GenerateCollectionMode.BindableCollection, "BindableCollection"),
                             new ListItem(GenerateCollectionMode.DataClassCollection, "DataClassCollection"),
                             new ListItem(GenerateCollectionMode.GenericCollection, "GenericCollection")
                         };
        }
    }

    internal class ListItem
    {
        public ListItem(GenerateCollectionMode id, string label)
        {
            ID = id;
            Label = label;
        }

        public GenerateCollectionMode ID { get; set; }

        public string Label { get; set; }
    }
}