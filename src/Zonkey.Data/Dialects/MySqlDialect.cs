using System;
using System.Data;

namespace Zonkey.Dialects
{
    /// <summary>
    /// Provides properties and methods specific to MySQL Database Server.
    /// </summary>
    public class MySqlDialect : SqlDialect
    {
        /// <summary>
        /// Gets or sets the default text parameter prefix character.
        /// </summary>
        /// <value>The default text parameter prefix character.</value>
        public static char DefaultTextParameterPrefixChar
        {
            get { return _defaultTextParameterPrefixChar; }
            set { _defaultTextParameterPrefixChar = value; }
        }

        private static char _defaultTextParameterPrefixChar = '@';

        /// <summary>
        /// Gets a value indicating whether database supports schema.
        /// </summary>
        /// <value><c>true</c>.</value>
        public override bool SupportsSchema
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
        /// Gets a value indicating whether database supports limit
        /// </summary>
        public override bool SupportsLimit
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
        /// Gets the server-specific command to obtain the last inserted identity.
        /// </summary>
        public override string FormatAutoIncrementSelect(string sequenceName)
        {
            return "LAST_INSERT_ID()";
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
        /// Gets or sets the text parameter prefix character.
        /// </summary>
        /// <value>The text parameter prefix character.</value>
        public char TextParameterPrefixChar
        {
            get { return _textParameterPrefixChar; }
            set { _textParameterPrefixChar = value; }
        }

        private char _textParameterPrefixChar = _defaultTextParameterPrefixChar;

        /// <summary>
        /// Formats the name of the field.
        /// </summary>
        /// <param name="name">The name.</param>
        /// <param name="useQuotedIdentifier">if set to <c>true</c> then formats the field name with quoted identifiers.</param>
        /// <returns>The formatted field name.</returns>
        public override string FormatFieldName(string name, bool? useQuotedIdentifier)
        {
            return (useQuotedIdentifier == true) ? string.Concat("`", name, "`") : name;
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
                if (string.IsNullOrEmpty(schemaName))
                    return string.Concat("`", tableName, "`");
                else
                    return string.Concat("`", schemaName, "`.`", tableName, "`");
            }
            else
            {
                if (string.IsNullOrEmpty(schemaName))
                    return tableName;
                else
                    return string.Concat(schemaName, ".", tableName);
            }
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

            if (commandType == CommandType.StoredProcedure)
                return name.TrimStart(':');
            else if ((name[0] == '@') || (name[0] == ':'))
                return name;
            else
                return string.Concat(_textParameterPrefixChar, name);
        }

        /// <summary>
        /// Formats the name of the paramter.
        /// </summary>
        /// <param name="index">The index.</param>
        /// <param name="commandType">An instance of a <see cref="System.Data.CommandType"/>.</param>
        /// <returns>The formatted parameter name.</returns>
        public override string FormatParameterName(int index, CommandType commandType)
        {
            return (commandType == CommandType.Text) ? string.Concat(_textParameterPrefixChar, "p", index) : "?";
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
            return string.Format( "SELECT {0} FROM {1} WHERE {2} ORDER BY {3} LIMIT {4},{5};", columnString, tableName, whereText, orderBy, start, length);
        }

        /// <summary>
        /// Optimizes the select single command.
        /// </summary>
        /// <param name="command">The command.</param>
        public override void OptimizeSelectSingleCommand(System.Data.Common.DbCommand command)
        {
            command.CommandText += " LIMIT 0,1";
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