namespace Zonkey.Dialects
{
    /// <summary>
    /// Provides properties and methods specific to PostgrSQL Database Server.
    /// </summary>
    public class PostgrSqlDialect : AnsiSqlDialect
    {
        /// <summary>
        /// Gets the server-specific command to obtain the last inserted identity.
        /// </summary>
        public override string FormatAutoIncrementSelect(string sequenceName)
        {            
            return (string.IsNullOrEmpty(sequenceName))
                ? "lastval()"
                : string.Format("currval('{0}')", sequenceName);
        }

        /// <summary>
        /// Gets a value indicating whether database supports limit
        /// </summary>
        public override bool SupportsLimit
        {
            get { return true; }
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
            return string.Format("SELECT {0} FROM {1} WHERE {2} ORDER BY {3} LIMIT {4} OFFSET {5};", columnString, tableName, whereText, orderBy, length, start);
        }

        /// <summary>
        /// Optimizes the select single command.
        /// </summary>
        /// <param name="command">The command.</param>
        public override void OptimizeSelectSingleCommand(System.Data.Common.DbCommand command)
        {
            command.CommandText += " LIMIT 1";
        }
    }
}
