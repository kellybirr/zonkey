using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Reflection;

namespace Zonkey.ObjectModel
{
    public class JoinDefinition
    {
        public virtual LambdaExpression JoinExpression { get; set; }

        public IList<Type> JoinTypes { get; } = new List<Type>();
    }

    public class JoinDefinition<T1, T2> : JoinDefinition
    {
        public virtual Expression<Func<T1, T2, bool>> JoinFunc { get; }

        public override LambdaExpression JoinExpression
        {
            get => JoinFunc;
            set { }
        }

        protected JoinDefinition()
        {
            JoinTypes.Add(typeof(T1));
            JoinTypes.Add(typeof(T2));
        }

        public JoinDefinition(Expression<Func<T1, T2, bool>> joinFunc) : this()
        {
            JoinFunc = joinFunc;
        }
    }

    public class JoinDefinition<T1, T2, T3> : JoinDefinition
    {
        public virtual Expression<Func<T1, T2, T3, bool>> JoinFunc { get; }

        public override LambdaExpression JoinExpression
        {
            get => JoinFunc;
            set { }
        }

        protected JoinDefinition()
        {
            JoinTypes.Add(typeof(T1));
            JoinTypes.Add(typeof(T2));
            JoinTypes.Add(typeof(T3));
        }

        public JoinDefinition(Expression<Func<T1, T2, T3, bool>> joinFunc) : this()
        {
            JoinFunc = joinFunc;
        }
    }

    public class JoinDefinition<T1, T2, T3, T4> : JoinDefinition
    {
        public virtual Expression<Func<T1, T2, T3, T4, bool>> JoinFunc { get; }

        public override LambdaExpression JoinExpression
        {
            get => JoinFunc;
            set { }
        }

        protected JoinDefinition()
        {
            JoinTypes.Add(typeof(T1));
            JoinTypes.Add(typeof(T2));
            JoinTypes.Add(typeof(T3));
            JoinTypes.Add(typeof(T4));
        }

        public JoinDefinition(Expression<Func<T1, T2, T3, T4, bool>> joinFunc) : this()
        {
            JoinFunc = joinFunc;
        }
    }

    public class JoinDefinition<T1, T2, T3, T4, T5> : JoinDefinition
    {
       public virtual Expression<Func<T1, T2, T3, T4, T5, bool>> JoinFunc { get; }

        public override LambdaExpression JoinExpression
        {
            get => JoinFunc;
            set { }
        }

        protected JoinDefinition()
        {
            JoinTypes.Add(typeof(T1));
            JoinTypes.Add(typeof(T2));
            JoinTypes.Add(typeof(T3));
            JoinTypes.Add(typeof(T4));
            JoinTypes.Add(typeof(T5));
        }

        public JoinDefinition(Expression<Func<T1, T2, T3, T4, T5, bool>> joinFunc) : this()
        {
            JoinFunc = joinFunc;
        }
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
