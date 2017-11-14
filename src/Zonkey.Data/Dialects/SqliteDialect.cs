using System;
using System.Data;
using System.Data.Common;

namespace Zonkey.Dialects
{
    /// <summary>
    /// Provides properties and methods specific to Sqlite.
    /// </summary>
    public class SqliteDialect : SqlDialect
    {
        /// <summary>
        /// Gets a value indicating whether database supports limit
        /// </summary>
        public override bool SupportsLimit => true;

        /// <summary>
        /// Gets the quoted identifiers mode settings.
        /// </summary>
        /// <value>The quoted identifiers setting.</value>
        public override QuotedIdentifiers QuotedIdentifiers => QuotedIdentifiers.Preferred;

        /// <summary>
        /// Gets the server-specific command to obtain the last inserted identity.
        /// </summary>
        public override string FormatAutoIncrementSelect(string sequenceName)
        {
            return "last_insert_rowid()";
        }

        /// <summary>
        /// Gets a value indicating whether database supports using named parameters.
        /// </summary>
        /// <value><c>true</c>.</value>
        public override bool UseNamedParameters => true;

        /// <summary>
        /// Fixes parameter types
        /// </summary>
        /// <param name="parameter"></param>
        public override void FixParameter(DbParameter parameter)
        {
            if (parameter.DbType == DbType.Guid)
                parameter.DbType = DbType.String;
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
            return (useQuotedIdentifier != false) ? string.Concat("[", tableName, "]") : tableName;
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
            return $"SELECT {columnString} FROM {tableName} WHERE {whereText} ORDER BY {orderBy} LIMIT {start} OFFSET {length};";
        }
    }
}