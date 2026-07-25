using System;
using System.Data;
using System.Data.Common;

namespace Zonkey.Dialects
{
    /// <summary>
    /// Provides properties and methods specific to PostgrSQL Database Server.
    /// </summary>
    public class PostgreSqlDialect : AnsiSqlDialect
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

        /// <summary>
        /// Formats the unary boolean.
        /// </summary>
        /// <param name="fieldName">Name of the field.</param>
        /// <returns>System.String.</returns>
        public override string FormatUnaryBoolean(string fieldName) => $"({fieldName})";

        public override string RenderFunction(string name, params string[] args)
        {
            switch (name)
            {
                // Postgres has no round(double precision, int) overload, only round(numeric, int);
                // casting to numeric is a no-op for arguments that are already numeric/decimal.
                case "ROUND2": return $"ROUND(CAST({args[0]} AS numeric), {args[1]})";
                default: return base.RenderFunction(name, args);
            }
        }

        public override string RenderLike(string left, string right, bool ignoreCase, char? escapeChar)
        {
            string escape = escapeChar.HasValue ? $" ESCAPE '{escapeChar}'" : string.Empty;
            return ignoreCase
                ? $"({left} ILIKE {right}{escape})"
                : $"({left} LIKE {right}{escape})";
        }

        public override string RenderRegexMatch(string left, string right, bool ignoreCase)
        {
            return ignoreCase ? $"({left} ~* {right})" : $"({left} ~ {right})";
        }

        /// <summary>Gets the maximum number of parameters allowed per text command (PG wire-protocol Bind limit).</summary>
        public override int MaxParameters
        {
            get { return 65535; }
        }

        public override bool SupportsInCollectionParameter(Type elementType)
        {
            // Npgsql binds typed CLR arrays of scalar-mappable types as PG arrays
            // (int[] -> integer[], long[] -> bigint[], string[] -> text[], Guid[] -> uuid[], ...).
            // Exclusions: byte (byte[] binds as bytea, not smallint[]); sbyte/ushort/uint/ulong
            // (no PG mapping - these stay on the literal-inline path); enums (scalar enum params are
            // converted by FixParameter, but enum arrays bind only for explicitly mapped native enums,
            // which the dialect cannot detect - enum lists stay individually parameterized).
            // DateTime arrays are allowed but require a consistent DateTimeKind across elements
            // (same Npgsql rule as scalars, enforced array-wide).
            if (elementType.IsEnum) return false;
            if (elementType == typeof(byte) || elementType == typeof(sbyte)
                || elementType == typeof(ushort) || elementType == typeof(uint) || elementType == typeof(ulong))
                return false;
            return true;
        }

        public override string RenderInCollectionParameter(string operand, string placeholder)
        {
            return $"({operand} = ANY({placeholder}))";
        }

        public override void FixParameter(DbParameter parameter)
        {
            if (parameter.Value?.GetType() is Type vt && vt.IsEnum)
            { 
                switch (parameter.DbType)
                {
                    case DbType.Byte:
                        parameter.Value = Convert.ChangeType(parameter.Value, typeof(Byte));
                        break;
                    case DbType.Int16:
                        parameter.Value = Convert.ChangeType(parameter.Value, typeof(Int16));
                        break;
                    case DbType.Int32:
                        parameter.Value = Convert.ChangeType(parameter.Value, typeof(Int32));
                        break;
                    case DbType.Int64:
                        parameter.Value = Convert.ChangeType(parameter.Value, typeof(Int64));
                        break;
                }
            }

            base.FixParameter(parameter);
        }
    }
}
