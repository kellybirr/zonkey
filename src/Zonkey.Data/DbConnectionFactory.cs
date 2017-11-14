using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Reflection;
using System.Threading.Tasks;

namespace Zonkey
{
    /// <summary>
    /// Provides methods for managing SQL database connections
    /// </summary>
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1706:ShortAcronymsShouldBeUppercase")]
    public static partial class DbConnectionFactory
    {
        private static readonly Dictionary<string, DbConnectionType> _connections = 
            new Dictionary<string, DbConnectionType>(StringComparer.OrdinalIgnoreCase);

        /// <summary>
        /// Registers a new connection Type with the provided Key, Type and ConnectionString
        /// </summary>
        /// <param name="name">The name of the connection to use when retrieving</param>
        /// <param name="connectionString">The connectionString to use when connecting</param>
        public static void Register<TConnection>(string name, string connectionString) where TConnection : DbConnection
        {
            Register<TConnection>(name, connectionString, true);
        }

        /// <summary>
        /// Registers a new connection Type with the provided Key, Type and ConnectionString
        /// </summary>
        /// <param name="name">The name of the connection to use when retrieving</param>
        /// <param name="connectionString">The connectionString to use when connecting</param>
        /// <param name="useSystemEnvironment">Should the system environment values be used ont he ConnectionString</param>
        public static void Register<TConnection>(string name, string connectionString, bool useSystemEnvironment) where TConnection : DbConnection
        {
            _connections[name] = new DbConnectionType
            {
                Name = name,
                Type = typeof(TConnection),
                ConnecitonString = connectionString,
                UseSystemEnvironment = useSystemEnvironment
            };
        }

        /// <summary>
        /// Registers a new connection Type with the provided Key, Type and ConnectionString
        /// </summary>
        /// <param name="name">The name of the connection to use when retrieving</param>
        /// <param name="type">Type of connection that is sub-classed from DbConnection</param>
        /// <param name="connectionString">The connectionString to use when connecting</param>
        public static void Register(string name, Type type, string connectionString)
        {
            Register(name, type, connectionString, true);
        }

        /// <summary>
        /// Registers a new connection Type with the provided Key, Type and ConnectionString
        /// </summary>
        /// <param name="name">The name of the connection to use when retrieving</param>
        /// <param name="type">Type of connection that is sub-classed from DbConnection</param>
        /// <param name="connectionString">The connectionString to use when connecting</param>
        /// <param name="useSystemEnvironment">Should the system environment values be used on the ConnectionString</param>
        public static void Register(string name, Type type, string connectionString, bool useSystemEnvironment)
        {
            TypeInfo typeInfo = type.GetTypeInfo();
            if (!typeInfo.IsSubclassOf(typeof(DbConnection)))
                throw new ArgumentException("Connection Type must inherit System.Data.Common.DbConnection", nameof(type));

            _connections[name] = new DbConnectionType
            {
                Name = name,
                Type = type,
                ConnecitonString = connectionString,
                UseSystemEnvironment = useSystemEnvironment
            };
        }


        /// <summary>
        /// Creates and Opens a new connection configured based on registered connection types
        /// </summary>
        /// <param name="name">the name of the registered connection</param>
        /// <returns>an open DbConnection</returns>
        public static async Task<DbConnection> OpenConnection(string name)
        {
            var cnxn = CreateConnection(name);
            await cnxn.OpenAsync();

            return cnxn;
        }

        /// <summary>
        /// Creates, but does not open a new connection configured based on registered connection types
        /// </summary>
        /// <param name="name">the name of the registered connection</param>
        /// <returns>a new DbConnection</returns>
        public static DbConnection CreateConnection(string name)
        {
            if (string.IsNullOrEmpty(name))
                throw new ArgumentNullException(nameof(name));

            if (! _connections.TryGetValue(name, out DbConnectionType connType))
                throw new ArgumentOutOfRangeException(nameof(connType));

            return connType.Create();
        }
    }

    internal class DbConnectionType
    {
        internal string Name { get; set; }
        internal string ConnecitonString { get; set; }
        internal Type Type { get; set; }

        internal bool UseSystemEnvironment { get; set; }

        public DbConnection Create()
        {
            var cnxn = (DbConnection) Activator.CreateInstance(Type);
            cnxn.ConnectionString = (UseSystemEnvironment)
                ? EnvironmentHelper.ProcessString(ConnecitonString)
                : ConnecitonString;

            return cnxn;
        }
    }
}