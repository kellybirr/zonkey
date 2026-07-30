using System;
using System.Linq.Expressions;

namespace Zonkey
{
    /// <summary>
    /// Thrown when a LINQ expression cannot be translated to SQL.
    /// Derives from NotSupportedException so existing catch blocks keep working.
    /// </summary>
    public class SqlExpressionException : NotSupportedException
    {
        public SqlExpressionException(string message) : base(message)
        { }

        internal static SqlExpressionException ForNode(Expression node, string reason, string hint = null)
        {
            string msg = $"Cannot translate expression '{node}': {reason}.";
            if (hint != null) msg += " Hint: " + hint;
            return new SqlExpressionException(msg);
        }
    }
}
