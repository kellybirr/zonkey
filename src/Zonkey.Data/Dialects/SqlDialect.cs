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
                { "MySqlConnector.MySqlConnection", _ => new MySqlDialect() },
                { "MariaDB.Data.MariaDbConnection", _ => new MySqlDialect() },
                { "System.Data.OracleClient.OracleConnection", _ => new OracleSqlDialect() },
                { "Oracle.ManagedDataAccess.Client.OracleConnection", _ => new OracleSqlDialect() },
                { "IBM.Data.DB2.DB2Connection", _ => new DB2SqlDialect() },
                { "Npgsql.NpgsqlConnection", _ => new PostgreSqlDialect() },
                { "Mono.Data.Sqlite.SqliteConnection", _ => new SqliteDialect() },
                { "System.Data.SQLite.SQLiteConnection", _ => new SqliteDialect() },
                { "Microsoft.Data.Sqlite.SqliteConnection", _ => new SqliteDialect() }
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
        /// Formats the limit query using the ANSI SQL:2008 <c>OFFSET ... FETCH NEXT ... ROWS ONLY</c> form.
        /// This is the default implementation inherited by dialects that support standard offset-fetch
        /// paging (SQL Server 2012+, Oracle, DB2, and any dialect that doesn't override it). Dialects
        /// with their own paging syntax (SQLite/PostgreSQL/MySQL use LIMIT/OFFSET) or that cannot page
        /// at all (Access) override this method.
        /// </summary>
        /// <param name="columnString">The column string.</param>
        /// <param name="tableName">Name of the table.</param>
        /// <param name="whereText">The where text.</param>
        /// <param name="orderBy">The order by.</param>
        /// <param name="start">The start (0-based row offset).</param>
        /// <param name="length">The length (page size).</param>
        /// <returns></returns>
        public virtual string FormatLimitQuery(string columnString, string tableName, string whereText, string orderBy, int start, int length)
        {
            return $"SELECT {columnString} FROM {tableName} WHERE {whereText} ORDER BY {orderBy} OFFSET {start} ROWS FETCH NEXT {length} ROWS ONLY;";
        }

        /// <summary>
        /// Formats a scalar existence query that returns 1 if any row matches, else 0.
        /// The ANSI CASE WHEN EXISTS form works on SQL Server, SQLite, PostgreSQL, and MySQL;
        /// dialects that require a FROM clause on scalar selects (Oracle, DB2) override this.
        /// </summary>
        /// <param name="tableName">The formatted table name (may include hints).</param>
        /// <param name="whereText">The WHERE clause text, without the WHERE keyword; may be empty.</param>
        /// <returns>The full command text for the existence check.</returns>
        public virtual string FormatExistsQuery(string tableName, string whereText)
        {
            return (string.IsNullOrEmpty(whereText))
                ? $"SELECT CASE WHEN EXISTS(SELECT 1 FROM {tableName}) THEN 1 ELSE 0 END AS ZONKEY_EXISTS"
                : $"SELECT CASE WHEN EXISTS(SELECT 1 FROM {tableName} WHERE {whereText}) THEN 1 ELSE 0 END AS ZONKEY_EXISTS";
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

        /// <summary>
        /// Parses the where function.
        /// </summary>
        /// <param name="functionName">Name of the function.</param>
        /// <param name="left">The left argument.</param>
        /// <param name="right">The right argument.</param>
        /// <returns>System.String.</returns>
        /// <exception cref="System.NotImplementedException"></exception>
        [Obsolete("No longer called by Zonkey. The expression translator uses RenderFunction/RenderLike/RenderRegexMatch instead.")]
        public virtual string ParseWhereFunction(string functionName, string left, string right)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Formats the unary boolean.
        /// </summary>
        /// <param name="fieldName">Name of the field.</param>
        /// <returns>System.String.</returns>
        public virtual string FormatUnaryBoolean(string fieldName) => $"({fieldName} = 1)";

        /// <summary>Renders a logical SQL function (UPPER, SUBSTRING, DATE_YEAR, ...) with pre-rendered arguments.</summary>
        public virtual string RenderFunction(string name, params string[] args)
        {
            switch (name)
            {
                case "UPPER": case "LOWER": case "TRIM":
                case "ABS": case "FLOOR": case "CEILING":
                    return $"{name}({args[0]})";
                case "ROUND1": return $"ROUND({args[0]})";
                case "ROUND2": return $"ROUND({args[0]}, {args[1]})";
                case "LENGTH": return $"LENGTH({args[0]})";
                case "SUBSTRING": return $"SUBSTRING({args[0]} FROM {args[1]} FOR {args[2]})";
                case "SUBSTRING2": return $"SUBSTRING({args[0]} FROM {args[1]})";
                case "INDEXOF": return $"(POSITION({args[1]} IN {args[0]}) - 1)";
                case "REPLACE": return $"REPLACE({args[0]}, {args[1]}, {args[2]})";
                case "CONCAT": return $"({args[0]} || {args[1]})";
                case "COALESCE": case "COALESCE_BOOL": return $"COALESCE({args[0]}, {args[1]})";
                case "CASE_WHEN": return $"CASE WHEN {args[0]} THEN {args[1]} ELSE {args[2]} END";
                case "ISNULLOREMPTY": return $"({args[0]} IS NULL OR {args[0]} = '')";
                case "DATE_YEAR": return $"EXTRACT(YEAR FROM {args[0]})";
                case "DATE_MONTH": return $"EXTRACT(MONTH FROM {args[0]})";
                case "DATE_DAY": return $"EXTRACT(DAY FROM {args[0]})";
                case "DATE_HOUR": return $"EXTRACT(HOUR FROM {args[0]})";
                case "DATE_MINUTE": return $"EXTRACT(MINUTE FROM {args[0]})";
                case "DATE_SECOND": return $"EXTRACT(SECOND FROM {args[0]})";
                case "DATE_DATE": return $"CAST({args[0]} AS DATE)";
                default:
                    throw new SqlExpressionException($"SQL function '{name}' is not supported by dialect {GetType().Name}");
            }
        }

        /// <summary>Renders a LIKE predicate; ignoreCase renders UPPER(x) LIKE UPPER(y) unless overridden (PostgreSql: ILIKE).</summary>
        public virtual string RenderLike(string left, string right, bool ignoreCase, char? escapeChar)
        {
            string escape = escapeChar.HasValue ? $" ESCAPE '{escapeChar}'" : string.Empty;
            return ignoreCase
                ? $"(UPPER({left}) LIKE UPPER({right}){escape})"
                : $"({left} LIKE {right}{escape})";
        }

        /// <summary>Renders a regex-match predicate. Only PostgreSql supports this.</summary>
        public virtual string RenderRegexMatch(string left, string right, bool ignoreCase)
        {
            throw new SqlExpressionException("Regex matching in WHERE expressions is only supported on PostgreSql.");
        }

        /// <summary>Gets the maximum number of parameters allowed per text command; conservative legacy default.</summary>
        public virtual int MaxParameters
        {
            get { return 2100; }
        }

        /// <summary>True if the dialect can bind a whole IN-list as a single collection parameter for this element type.</summary>
        public virtual bool SupportsInCollectionParameter(Type elementType)
        {
            return false;
        }

        /// <summary>Renders the IN-collection predicate (e.g. PostgreSql: (operand = ANY($n))).</summary>
        public virtual string RenderInCollectionParameter(string operand, string placeholder)
        {
            throw new SqlExpressionException($"Dialect {GetType().Name} does not support collection parameters");
        }
    }
}
