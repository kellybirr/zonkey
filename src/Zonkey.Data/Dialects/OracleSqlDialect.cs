using System;
using System.Data;

namespace Zonkey.Dialects
{
    /// <summary>
    /// Provides properties and methods specific to Oracle Database Server.
    /// </summary>
    public class OracleSqlDialect : AnsiSqlDialect
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
        /// Oracle requires a FROM clause on scalar selects.
        /// </summary>
        public override string FormatExistsQuery(string tableName, string whereText)
        {
            return base.FormatExistsQuery(tableName, whereText) + " FROM DUAL";
        }
    }
}