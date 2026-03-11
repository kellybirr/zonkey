using System.Data;

#if NET6_0_OR_GREATER
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
