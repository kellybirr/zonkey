using System.Data;

#if !NETFRAMEWORK
using Microsoft.Data.SqlClient;
#else
using System.Data.SqlClient;
#endif

namespace Zonkey
{
    public static class MsSqlExtension
    {
        public static void Initialize()
        {
            DbParameterExtensions.UseTypeSetter<SqlParameter>(DbType.Time, (p,f) =>
            {
                ((SqlParameter)p).SqlDbType = SqlDbType.Time;
            });
        }
    }
}
