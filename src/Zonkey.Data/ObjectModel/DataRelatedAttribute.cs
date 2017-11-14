using System;

namespace Zonkey.ObjectModel
{
    /// <summary>
    /// Provides properties and methods to parse attributes of a DataField on a <see cref="Zonkey.ObjectModel.DataClass"/> instance.
    /// </summary>
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1019:DefineAccessorsForAttributeArguments")]
    [AttributeUsage(AttributeTargets.Property, Inherited = true, AllowMultiple = false)]
    public sealed class DataRelatedAttribute : Attribute
    {
        public DataRelatedAttribute(string keyName) : this (keyName, keyName)
        { }

        public DataRelatedAttribute(string parentKeyName, string childKeyName)
        {
            ParentKeyName = parentKeyName;
            ChildKeyName = childKeyName;
        }

        public string ParentKeyName { get; set; }

        public string ChildKeyName { get; set; }

        public string Where { get; set; }

    }
}
