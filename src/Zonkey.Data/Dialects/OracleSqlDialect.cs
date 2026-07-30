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
        /// Gets a value indicating whether database supports limit.
        /// Oracle 12c+ supports the ANSI OFFSET/FETCH syntax inherited from the base dialect.
        /// </summary>
        public override bool SupportsLimit
        {
            get { return true; }
        }

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

        public override string RenderFunction(string name, params string[] args)
        {
            switch (name)
            {
                case "SUBSTRING": return $"SUBSTR({args[0]}, {args[1]}, {args[2]})";
                case "SUBSTRING2": return $"SUBSTR({args[0]}, {args[1]})";
                case "INDEXOF": return $"(INSTR({args[0]}, {args[1]}) - 1)";
                case "CEILING": return $"CEIL({args[0]})";
                case "DATE_DATE": return $"TRUNC({args[0]})";
                default: return base.RenderFunction(name, args);
            }
        }
    }
}