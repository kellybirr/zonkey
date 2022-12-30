using System;
using System.Data;
using System.Data.Common;

namespace Zonkey.Dialects
{
    /// <summary>
    /// Provides properties and methods specific to Microsoft Access.
    /// </summary>
    public class AccessSqlDialect : SqlDialect
    {
        /// <summary>
        /// Gets the server-specific command to obtain the last inserted identity.
        /// </summary>
        public override string FormatAutoIncrementSelect(string sequenceName)
        {
            return "@@IDENTITY";
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
            return "?";
        }

        /// <summary>
        /// Formats the name of the paramter.
        /// </summary>
        /// <param name="index">The index.</param>
        /// <param name="commandType">An instance of a <see cref="System.Data.CommandType"/>.</param>
        /// <returns>The formatted parameter name.</returns>
        public override string FormatParameterName(int index, CommandType commandType)
        {
            return "?";
        }

        /// <summary>
        /// Optimizes the select single command.
        /// </summary>
        /// <param name="command">The command.</param>
        public override void OptimizeSelectSingleCommand(DbCommand command)
        {
            if (!command.CommandText.StartsWith("SELECT"))
                return;

            string cmdWOselect = command.CommandText.Substring(6);
            command.CommandText = "SELECT TOP 1" + cmdWOselect;
        }

        public override string ParseWhereFunction(string functionName, string left, string right)
        {
            switch (functionName)
            {
                case "StartsWith":
                    return $"({left} LIKE ({right} & '%'))";
                case "EndsWith":
                    return $"({left} LIKE ('%' & {right}))";
                case "Contains":
                    return $"({left} LIKE ('%' & {right} & '%'))";
                default:
                    throw new NotSupportedException();
            }
        }
    }
}