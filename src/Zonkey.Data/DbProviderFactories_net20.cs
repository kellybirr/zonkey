#if (NETSTANDARD2_0)
using System;
using System.Collections.Generic;
using System.Data.Common;

namespace Zonkey
{
    /// <summary>
    /// DbProviderFactories static class does not exist in .Net Standard 2.0, so we're faking it
    /// </summary>
    public static class DbProviderFactories
    {
        internal static DbProviderFactory GetFactory(DbConnection connection)
        {
            string providerName = connection.GetType().Namespace;
            if (_providerFactories.TryGetValue(providerName, out DbProviderFactory factory))
                return factory;

            throw new NotSupportedException($"Provider '{providerName}' is not registered");
        }

        public static void RegisterFactory(string providerName, DbProviderFactory factory)
            => _providerFactories[providerName.ToLowerInvariant()] = factory;

        private static readonly Dictionary<string, DbProviderFactory> _providerFactories
            = new Dictionary<string, DbProviderFactory>(StringComparer.InvariantCultureIgnoreCase);
    }
}
#endif
