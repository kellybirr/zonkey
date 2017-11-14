using System.Configuration;
using System.Data.Common;

namespace Zonkey
{
    public static partial class DbConnectionFactory
    {
        private static readonly object _loadLocker = new object();

        /// <summary>
        /// Loads the connectionStrings from the web.config/app.config 
        /// </summary>
        /// <param name="useEnvironment">Should environment variables be processed on connection strings</param>
        public static void LoadConnectionStrings(bool useEnvironment=true)
        {
            lock (_loadLocker)
            {
                foreach (ConnectionStringSettings connectionString in ConfigurationManager.ConnectionStrings)
                {
                    if (string.IsNullOrWhiteSpace(connectionString.ProviderName))
                        throw new ConfigurationErrorsException($"ConnectionString `{connectionString.Name}` does not specify a providerName");

                    // Use DbProviderFactory to get actual connection
                    DbProviderFactory providerFactory = DbProviderFactories.GetFactory(connectionString.ProviderName);
                    DbConnection cnxn = providerFactory.CreateConnection();
                    if (cnxn == null) throw new ConfigurationErrorsException($"Unable to get connection for provider `{connectionString.ProviderName}`");

                    Register(
                        name: connectionString.Name, 
                        type: cnxn.GetType(), 
                        connectionString: connectionString.ConnectionString, 
                        useSystemEnvironment: useEnvironment
                        );
                }
            }
        }
    }
}
