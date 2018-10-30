using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;

namespace Zonkey.ObjectModel
{
    public abstract class JoinDefinition
    {
        public LambdaExpression JoinExpression { get; set; }

        public List<Type> JoinTypes { get; } = new List<Type>();

        protected JoinDefinition()
        { }
    }

    [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1019:DefineAccessorsForAttributeArguments")]
    [AttributeUsage(AttributeTargets.Class, Inherited = true, AllowMultiple = false)]
    public sealed class DataJoinAttribute : Attribute
    {
        public Type JoinDefinition { get; }

        /// <inheritdoc />
        /// <summary>
        /// Initializes a new instance of the <see cref="DataJoinAttribute"/> class.
        /// </summary>
        /// <param name="joinDefType">Name of the table.</param>
        public DataJoinAttribute(Type joinDefType)
        {
            JoinDefinition = joinDefType;
        }

        public static DataJoinAttribute GetFromType(Type type)
        {
            return (DataJoinAttribute)type.GetTypeInfo().GetCustomAttribute(typeof(DataJoinAttribute));
        }

    }
}
