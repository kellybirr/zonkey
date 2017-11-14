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
    }
}