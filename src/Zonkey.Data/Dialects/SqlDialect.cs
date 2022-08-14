using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;

namespace Zonkey.Dialects
{
    /// <summary>
    /// Base class describing properties and methods for specific Database servers
    /// </summary>
    public abstract class SqlDialect
    {
        static SqlDialect()
        {
            Factories = new Dictionary<string, Func<DbConnection, SqlDialect>>(StringComparer.OrdinalIgnoreCase)
            {
                { "Microsoft.Data.SqlClient.SqlConnection", _ => new SqlServerDialect() },
                { "System.Data.SqlClient.SqlConnection", _ => new SqlServerDialect() },
                { "System.Data.SqlServerCe.SqlCeConnection", _ => new SqlServerDialect() },
                { "CoreLab.MySql.MySqlConnection", _ => new MySqlDialect() },
                { "Devart.Data.MySql.MySqlConnection", _ => new MySqlDialect() },
                { "MySql.Data.MySqlClient.MySqlConnection", _ => new MySqlDialect() },
                { "System.Data.OracleClient.OracleConnection", _ => new OracleSqlDialect() },
                { "IBM.Data.DB2.DB2Connection", _ => new DB2SqlDialect() },
                { "Npgsql.NpgsqlConnection", _ => new PostgreSqlDialect() },
                { "Mono.Data.Sqlite.SqliteConnection", _ => new SqliteDialect() }
            };
        }

        public static Dictionary<string, Func<DbConnection, SqlDialect>> Factories { get; }

        /// <summary>
        /// Creates the proper SqlDialect form the specified DbConnection.
        /// </summary>
        /// <param name="connection">The DbConnection.</param>
        /// <returns></returns>
        public static SqlDialect Create(DbConnection connection)
        {
            if (connection == null) return null;

            string typeName = connection.GetType().FullName;
            if (Factories.TryGetValue(typeName, out Func<DbConnection, SqlDialect> factory))
                return factory(connection);

            return new GenericSqlDialect();
        }

        /// <summary>
        /// Gets a value indicating whether [supports row version].
        /// </summary>
        /// <value><c>true</c> if [supports row version]; otherwise, <c>false</c>.</value>
        public virtual bool SupportsRowVersion
        {
            get { return false; }
        }

        /// <summary>
        /// Gets a value indicating whether [supports schema].
        /// </summary>
        /// <value><c>true</c> if [supports schema]; otherwise, <c>false</c>.</value>
        public virtual bool SupportsSchema
        {
            get { return false; }
        }

        /// <summary>
        /// Gets a value indicating whether [supports limit].
        /// </summary>
        /// <value><c>true</c> if [supports limit]; otherwise, <c>false</c>.</value>
        public virtual bool SupportsLimit
        {
            get { return false; }
        }

        /// <summary>
        /// Gets a value indicating whether [supports no lock].
        /// </summary>
        /// <value><c>true</c> if [supports no lock]; otherwise, <c>false</c>.</value>
        public virtual bool SupportsNoLock
        {
            get { return false; }
        }

        /// <summary>
        /// Gets a value indicating whether [supports S procs].
        /// </summary>
        /// <value><c>true</c> if [supports S procs]; otherwise, <c>false</c>.</value>
        public virtual bool SupportsStoredProcedures
        {
            get { return false; }
        }

        /// <summary>
        /// Gets a value indicating if change contexts are supported by the dialect
        /// </summary>
        public virtual bool SupportsChangeContext
        {
            get { return false; }
        }

        /// <summary>
        /// Gets a value indicating whether [use SQL batches].
        /// </summary>
        /// <value><c>true</c> if [use SQL batches]; otherwise, <c>false</c>.</value>
        public virtual bool UseSqlBatches
        {
            get { return false; }
        }

        /// <summary>
        /// Gets a value indicating whether [use named parameters].
        /// </summary>
        /// <value><c>true</c> if [use named parameters]; otherwise, <c>false</c>.</value>
        public virtual bool UseNamedParameters
        {
            get { return false; }
        }

        /// <summary>
        /// Gets the quoted identifiers mode settings.
        /// </summary>
        /// <value>The quoted identifiers setting.</value>
        public virtual QuotedIdentifiers QuotedIdentifiers
        {
            get { return QuotedIdentifiers.NotSupported; }
        }

        /// <summary>
        /// Gets the last identity var.
        /// </summary>
        /// <value>The last identity var.</value>
        public virtual string FormatAutoIncrementSelect(string sequenceName)
        {
            throw new NotSupportedException("This SQL dialect does not support the LastIdentity feature.");
        }

        /// <summary>
        /// Formats the limit query.
        /// </summary>
        /// <param name="columnString">The column string.</param>
        /// <param name="tableName">Name of the table.</param>
        /// <param name="whereText">The where text.</param>
        /// <param name="orderBy">The order by.</param>
        /// <param name="start">The start.</param>
        /// <param name="length">The length.</param>
        /// <returns></returns>
        public virtual string FormatLimitQuery(string columnString, string tableName, string whereText, string orderBy, int start, int length)
        {
            throw new NotSupportedException("This SQL dialect does not support the FormatLimitQuery feature.");
        }

        /// <summary>
        /// Optimizes the select single command.
        /// </summary>
        /// <param name="command">The command.</param>
        public virtual void OptimizeSelectSingleCommand(DbCommand command)
        {
            
        }

        /// <summary>
        /// Applies the change tacking context to a command
        /// </summary>
        /// <param name="command">the command to affect</param>
        /// <param name="contextObj">the context object for change tracking</param>
        public virtual void ApplyChangeTrackingContext(DbCommand command, object contextObj)
        {
            
        }

        /// <summary>
        /// Formats the GUID literal.
        /// </summary>
        /// <param name="guid">The GUID.</param>
        /// <returns></returns>
        public virtual string FormatGuidLiteral(Guid? guid)
        {
            return (guid.HasValue) ? string.Format("'{0}'", guid) : "NULL";
        }

        /// <summary>
        /// Override to have the dialect fix parameters before they're added to the command
        /// </summary>
        /// <param name="parameter"></param>
        public virtual void FixParameter(DbParameter parameter)
        {

        }

        /// <summary>
        /// Formats the name of the field.
        /// </summary>
        /// <param name="name">The name.</param>
        /// <returns></returns>
        public virtual string FormatFieldName(string name)
        {
            return FormatFieldName(name, null);
        }

        /// <summary>
        /// Formats the name of the field.
        /// </summary>
        /// <param name="name">The name.</param>
        /// <param name="useQuotedIdentifier">if set to <c>true</c> [use quoted identifier].</param>
        /// <returns></returns>
        public abstract string FormatFieldName(string name, bool? useQuotedIdentifier);

        /// <summary>
        /// Formats the name of the table.
        /// </summary>
        /// <param name="tableName">Name of the table.</param>
        /// <param name="schemaName">Name of the schema.</param>
        /// <returns></returns>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1702:CompoundWordsShouldBeCasedCorrectly", MessageId = "FormatTable")]
        public virtual string FormatTableName(string tableName, string schemaName)
        {
            return FormatTableName(tableName, schemaName, null);
        }

        /// <summary>
        /// Formats the name of the table.
        /// </summary>
        /// <param name="tableName">Name of the table.</param>
        /// <param name="schemaName">Name of the schema.</param>
        /// <param name="useQuotedIdentifier">if set to <c>true</c> [use quoted identifier].</param>
        /// <returns></returns>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1702:CompoundWordsShouldBeCasedCorrectly", MessageId = "FormatTable")]
        public abstract string FormatTableName(string tableName, string schemaName, bool? useQuotedIdentifier);

        /// <summary>
        /// Formats the name of the paramter.
        /// </summary>
        /// <param name="name">The name.</param>
        /// <param name="commandType">Type of the command.</param>
        /// <returns></returns>
        public abstract string FormatParameterName(string name, CommandType commandType);

        /// <summary>
        /// Formats the name of the paramter.
        /// </summary>
        /// <param name="index">The index.</param>
        /// <param name="commandType">Type of the command.</param>
        /// <returns></returns>
        public abstract string FormatParameterName(int index, CommandType commandType);
    }
}
