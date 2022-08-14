#if (NET48)
using System.Data.Common;
using Zonkey.Dialects;

namespace Zonkey.Ado
{
    public class OleDbHelper
    {
        public static void RegisterDialects()
        {
            const string typeName = "System.Data.OleDb.OleDbConnection";
            if (! SqlDialect.Factories.ContainsKey(typeName))
            {
                SqlDialect.Factories.Add(typeName, OleDbDialectFactory);
            }
        }

        private static SqlDialect OleDbDialectFactory(DbConnection connection)
        {
            switch (((System.Data.OleDb.OleDbConnection)connection).Provider)
            {
                case "Microsoft.Jet.OLEDB.3.5.1":
                case "Microsoft.Jet.OLEDB.4.0":
                    return new AccessSqlDialect();
                default:
                    return new GenericSqlDialect();
            }
        }
    }
}
#endif
