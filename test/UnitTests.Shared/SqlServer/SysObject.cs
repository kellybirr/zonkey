using System;
using System.Data;
using Zonkey.ObjectModel;

namespace Zonkey.UnitTests.SqlServer
{
    [DataItem("sysobjects", AccessType = AccessType.ReadOnly)]
    internal class SysObject : DataClass<int>
    {
        #region Data Columns (Properties)

        [DataField("name", DbType.String, false, Length = 128)]
        public virtual string name { get; set; }

        [DataField("id", DbType.Int32, false, IsKeyField = true)]
        public virtual Int32 id { get; set; }

        [DataField("xtype", DbType.StringFixedLength, false, Length = 2)]
        public virtual string xtype { get; set; }

        [DataField("uid", DbType.Int16, false)]
        public virtual Int16 uid { get; set; }

        [DataField("type", DbType.StringFixedLength, true, Length = 2)]
        public virtual string type { get; set; }

        #endregion

        public SysObject() : base(false) { }
    }

    #region Typed Collection

    internal class SysObjectCollection : DataClassCollection<SysObject>
    {

    }

    #endregion

}
