using System;
using System.Threading.Tasks;
using Zonkey.ObjectModel;

namespace Zonkey.UnitTests.AdventureWorks
{
    class AdventureDb : DatabaseWrapper
    {
        public const string Name = "AdventureWorks";

        public static string ConnectionString
        {
            get
            {
                switch (Environment.MachineName.ToUpperInvariant())
                {
                    case "DILBERT7":
                        return "Server=(local);Database=AdventureWorks2014;Trusted_Connection=Yes;";
                    case "LKEC1799":
                        return "Server=(local)\\SqlExpress;Database=AdventureWorks2014;Trusted_Connection=Yes;";
                    default:
                        throw new Exception("Unknown Dev/Test Machine");
                }
            }
        }

        private AdventureDb() : base(Name)
        { }

        public static async Task<AdventureDb> Open()
        {
            var db = new AdventureDb();
            await db.Connection.OpenAsync();

            return db;
        }
    }
}
