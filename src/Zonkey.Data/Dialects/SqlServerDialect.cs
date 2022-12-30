using System;
using System.Data;
using System.Data.Common;
using System.Linq;
using System.Text;

namespace Zonkey.Dialects
{
    /// <summary>
    /// Provides properties and methods specific to Microsoft SQL Server.
    /// </summary>
    public class SqlServerDialect : SqlDialect
    {
        /// <summary>
        /// Gets a value indicating whether database supports row version.
        /// </summary>
        /// <value><c>true</c>.</value>
        public override bool SupportsRowVersion
        {
            get { return true; }
        }

        /// <summary>
        /// Gets a value indicating whether database supports schema.
        /// </summary>
        /// <value><c>true</c>.</value>
        public override bool SupportsSchema
        {
            get { return true; }
        }

        /// <summary>
        /// Gets a value indicating whether database supports limit
        /// </summary>
        public override bool SupportsLimit
        {
            get { return true; }
        }

        /// <summary>
        /// Gets a value indicating whether database supports no-lock
        /// </summary>
        public override bool SupportsNoLock
        {
            get { return true; }
        }
        /// <summary>
        /// Gets a value indicating whether database supports stored procedures.
        /// </summary>
        /// <value><c>true</c>.</value>
        public override bool SupportsStoredProcedures
        {
            get { return true; }
        }

        /// <summary>
        /// Gets a value indicating if change contexts are supported by the dialect
        /// </summary>
        public override bool SupportsChangeContext
        {
            get { return true; }
        }

        /// <summary>
        /// Gets the quoted identifiers mode settings.
        /// </summary>
        /// <value>The quoted identifiers setting.</value>
        public override QuotedIdentifiers QuotedIdentifiers
        {
            get { return QuotedIdentifiers.Preferred; }
        }

        /// <summary>
        /// Gets the server-specific command to obtain the last inserted identity.
        /// </summary>
        public override string FormatAutoIncrementSelect(string sequenceName)
        {
            return "SCOPE_IDENTITY()";
        }

        /// <summary>
        /// Gets a value indicating whether database supports using named parameters.
        /// </summary>
        /// <value><c>true</c>.</value>
        public override bool UseNamedParameters
        {
            get { return true; }
        }

        /// <summary>
        /// Gets a value indicating whether database supports using SQL batches.
        /// </summary>
        /// <value><c>true</c>.</value>
        public override bool UseSqlBatches
        {
            get { return true; }
        }

        /// <summary>
        /// Formats the name of the field.
        /// </summary>
        /// <param name="name">The name.</param>
        /// <param name="useQuotedIdentifier">if set to <c>true</c> then formats the field name with quoted identifiers.</param>
        /// <returns>The formatted field name.</returns>
        public override string FormatFieldName(string name, bool? useQuotedIdentifier)
        {
            return (useQuotedIdentifier != false) ? string.Concat("[", name, "]") : name;
        }

        /// <summary>
        /// Formats the name of the table.
        /// </summary>
        /// <param name="tableName">Name of the table.</param>
        /// <param name="schemaName">Name of the schema.</param>
        /// <param name="useQuotedIdentifier">if set to <c>true</c> then formats the table name with quoted identifiers.</param>
        /// <returns>The formatted table name.</returns>
        public override string FormatTableName(string tableName, string schemaName, bool? useQuotedIdentifier)
        {
            if (useQuotedIdentifier != false)
            {
                return (string.IsNullOrEmpty(schemaName)) 
                    ? string.Concat("[", tableName, "]") 
                    : string.Concat("[", schemaName, "].[", tableName, "]");
            }

            return (string.IsNullOrEmpty(schemaName)) 
                ? tableName 
                : string.Concat(schemaName, ".", tableName);
        }    

        /// <summary>
        /// Formats the name of the paramter.
        /// </summary>
        /// <param name="name">The name.</param>
        /// <param name="commandType">An instance of a <see cref="System.Data.CommandType"/>.</param>
        /// <returns>The formatted parameter name.</returns>
        public override string FormatParameterName(string name, CommandType commandType)
        {
            if (string.IsNullOrEmpty(name)) throw new ArgumentNullException(nameof(name));

            return ((name[0] == '@') ? name : "@" + name).Replace(' ', '_');
        }

        /// <summary>
        /// Formats the name of the parameter.
        /// </summary>
        /// <param name="index">The index.</param>
        /// <param name="commandType">An instance of a <see cref="System.Data.CommandType"/>.</param>
        /// <returns>The formatted parameter name.</returns>
        public override string FormatParameterName(int index, CommandType commandType)
        {
            return (commandType == CommandType.Text) ? string.Concat("@p", index) : "?";
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
        public override string FormatLimitQuery(string columnString, string tableName, string whereText, string orderBy, int start, int length)
        {
            return
                string.Format(
                    "SELECT {6} FROM ( SELECT {0}, ROW_NUMBER() OVER(ORDER BY {3}) AS [__ZONKEY_ROW_INDEX__] FROM {1} WHERE {2} ) [__ZONKEY_INNER_QUERY__] WHERE [__ZONKEY_ROW_INDEX__] BETWEEN {4} AND {5};",
                        columnString, tableName, whereText, orderBy, start + 1, start + length, columnString.Replace(tableName+".", ""));
        }

        /// <summary>
        /// Optimizes the select single command.
        /// </summary>
        /// <param name="command">The command.</param>
        public override void OptimizeSelectSingleCommand(DbCommand command)
        {
            if (! command.CommandText.StartsWith("SELECT"))
                return;

            string cmdWOselect = command.CommandText.Substring(6);
            command.CommandText = "SELECT TOP 1" + cmdWOselect;
        }

        public override void ApplyChangeTrackingContext(DbCommand command, object contextObj)
        {
            if (contextObj == null) return;

            byte[] bytes;
            if (contextObj is byte[])
                bytes = (byte[]) contextObj;
            if (contextObj is string)
                bytes = Encoding.UTF8.GetBytes((string) contextObj);
            else 
                throw new ArgumentException("Only 'String' and 'Byte[]' are supported types for SQL Server change tracking context.",nameof(contextObj));

            if (bytes.Length > 128)
                bytes = bytes.Take(128).ToArray();

            command.CommandText = "WITH CHANGE_TRACKING_CONTEXT (@ZONEKY_CHANGE_TRACKING_) " + command.CommandText;

            DbParameter cdcp = command.CreateParameter();
            cdcp.ParameterName = "@ZONEKY_CHANGE_TRACKING_";
            cdcp.DbType = DbType.Binary;
            cdcp.Size = 128;
            cdcp.Value = bytes;

            command.Parameters.Add(cdcp);
        }

        public override string ParseWhereFunction(string functionName, string left, string right)
        {
            switch (functionName)
            {
                case "StartsWith":
                    return $"({left} LIKE ({right}+'%'))";
                case "EndsWith":
                    return $"({left} LIKE ('%'+{right}))";
                case "Contains":
                    return $"({left} LIKE ('%'+{right}+'%'))";
                default:
                    throw new NotSupportedException();
            }
        }
    }
}