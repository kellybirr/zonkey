using System;
using System.Data;

namespace Zonkey.Dialects
{
    /// <summary>
    /// Provides properties and methods specific to an ANSI Standards Based RDBMS.
    /// </summary>
    public class AnsiSqlDialect : SqlDialect
    {
        /// <summary>
        /// Gets a value indicating whether database supports stored procedures.
        /// </summary>
        /// <value><c>true</c>.</value>
        public override bool SupportsStoredProcedures
        {
            get { return true; }
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
        /// Gets the quoted identifiers mode settings.
        /// </summary>
        /// <value>The quoted identifiers setting.</value>
        public override QuotedIdentifiers QuotedIdentifiers
        {
            get { return QuotedIdentifiers.Optional; }
        }

        /// <summary>
        /// Formats the name of the field.
        /// </summary>
        /// <param name="name">The name.</param>
        /// <param name="useQuotedIdentifier">if set to <c>true</c> then formats the field name with quoted identifiers.</param>
        /// <returns>The formatted field name.</returns>
        public override string FormatFieldName(string name, bool? useQuotedIdentifier)
        {
            return (useQuotedIdentifier == true) ? string.Concat("\"", name, "\"") : name;
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
            if (useQuotedIdentifier == true)
            {
                return (string.IsNullOrEmpty(schemaName))
                    ? string.Concat("\"", tableName, "\"")
                    : string.Concat("\"", schemaName, "\".\"", tableName, "\"");
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
            if (commandType == CommandType.StoredProcedure)
                return (string.IsNullOrEmpty(name)) ? null : name.TrimStart(':');

            if (string.IsNullOrEmpty(name))
                throw new ArgumentNullException(nameof(name));

            return (name[0] == ':') ? name : string.Concat(":", name);
        }

        /// <summary>
        /// Formats the name of the paramter.
        /// </summary>
        /// <param name="index">The index.</param>
        /// <param name="commandType">An instance of a <see cref="System.Data.CommandType"/>.</param>
        /// <returns>The formatted parameter name.</returns>
        public override string FormatParameterName(int index, CommandType commandType)
        {
            return (commandType == CommandType.Text) ? string.Concat(":p", index) : "?";
        }

        public override string ParseWhereFunction(string functionName, string left, string right)
        {
            switch (functionName)
            {
                case "StartsWith":
                    return $"({left} LIKE CONCAT({right},'%'))";
                case "EndsWith":
                    return $"({left} LIKE CONCAT('%',{right}))";
                case "Contains":
                    return $"({left} LIKE CONCAT('%',{right},'%'))";
                default:
                    throw new NotSupportedException();
            }
        }
    }
}
