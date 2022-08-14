using System.Data;

#if (NETSTANDARD2_0_OR_GREATER)
    using Microsoft.Data.SqlClient;
#elif (NET48)
    using System.Data.SqlClient;
#endif

namespace Zonkey
{
    public static class MsSqlExtension
    {
        public static void Initialize()
        {
#if (NETSTANDARD2_0)
            // ReSharper disable once RedundantNameQualifier
            Zonkey.DbProviderFactories.RegisterFactory("Microsoft.Data.SqlClient", SqlClientFactory.Instance);
#endif

            DbParameterExtensions.UseTypeSetter<SqlParameter>(DbType.Time, p =>
            {
                ((SqlParameter)p).SqlDbType = SqlDbType.Time;
            });
        }
    }
}
