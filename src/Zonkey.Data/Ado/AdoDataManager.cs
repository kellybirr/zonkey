using System;
using System.Data.Common;

namespace Zonkey.Ado
{
    [Obsolete("Class is obsolete as of .Net Standard 2.0, use Zonkey.DataManager instead")]
    public class AdoDataManager : DataManager
    {
        public AdoDataManager(DbConnection connection) : base(connection)
        { }

        /// <summary>
        /// Default Constructor
        /// </summary>
        public AdoDataManager()
        { }
    }
}
