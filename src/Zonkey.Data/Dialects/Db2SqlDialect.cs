using System;
using System.Data;

namespace Zonkey.Dialects
{
    /// <summary>
    /// Provides properties and methods specific to IBM DB2 Database Server.
    /// </summary>
    public class DB2SqlDialect : AnsiSqlDialect
    {
        /// <summary>
        /// Gets a value indicating whether database supports limit.
        /// DB2 supports the ANSI OFFSET/FETCH syntax inherited from the base dialect.
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
            return "SYSIBM.IDENTITY_VAL_LOCAL()";
        }

        /// <summary>
        /// DB2 requires a FROM clause on scalar selects.
        /// </summary>
        public override string FormatExistsQuery(string tableName, string whereText)
        {
            return base.FormatExistsQuery(tableName, whereText) + " FROM SYSIBM.SYSDUMMY1";
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
            return (commandType == CommandType.StoredProcedure) ? name.TrimStart(':') : "?";
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

        public override string RenderFunction(string name, params string[] args)
        {
            switch (name)
            {
                case "SUBSTRING": return $"SUBSTR({args[0]}, {args[1]}, {args[2]})";
                case "SUBSTRING2": return $"SUBSTR({args[0]}, {args[1]})";
                case "INDEXOF": return $"(LOCATE({args[1]}, {args[0]}) - 1)";
                default: return base.RenderFunction(name, args);
            }
        }
    }
}