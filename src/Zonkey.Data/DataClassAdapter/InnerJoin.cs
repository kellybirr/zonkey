using System;
using System.Collections.Generic;
using System.Linq.Expressions;

namespace Zonkey
{
    public partial class DataClassAdapter<T>
    {
        private JoinCollection<T> _joinCollection;

        /// <summary>
        /// Gets the inner join collection.
        /// </summary>
        /// <value>The collection.</value>
        [Obsolete("This feature is still in development, it's not yet working", true)]
        private JoinCollection<T> InnerJoins
        {
            get
            {
                if (_joinCollection == null)
                    _joinCollection = new JoinCollection<T>(this);

                return _joinCollection;
            }
        }


#pragma warning disable 693
        /// <summary>
        /// The Join Collection
        /// </summary>
        /// <typeparam name="T"></typeparam>
        private class JoinCollection<T> : IEnumerable<KeyValuePair<Type, LambdaExpression>> where T: class 
#pragma warning restore 693
        {
            private readonly Dictionary<Type, LambdaExpression> _joins;
            private readonly DataClassAdapter<T> _adapter;

            /// <summary>
            /// Initializes a new instance of the <see cref="DataClassAdapter&lt;T&gt;.JoinCollection&lt;T&gt;"/> class.
            /// </summary>
            internal JoinCollection(DataClassAdapter<T> adapter)
            {
                _adapter = adapter;
                _joins = new Dictionary<Type, LambdaExpression>();
            }

            /// <summary>
            /// Gets the <see cref="System.Linq.Expressions.LambdaExpression"/> with the specified key.
            /// </summary>
            /// <value></value>
            public LambdaExpression this[Type key]
            {
                get { return _joins[key];}
            }

            /// <summary>
            /// Adds the specified join expression.
            /// </summary>
            /// <typeparam name="Tc">The type of the c.</typeparam>
            /// <param name="joinExpression">The join expression.</param>
            public void Add<Tc>(Expression<Func<T, Tc, bool>> joinExpression) where Tc : class, new()
            {
                _joins.Add(typeof(Tc), joinExpression);

                if (_joins.Count == 1)
                    _adapter.CommandBuilder.UseTableWithFieldNames = true;
            }

            /// <summary>
            /// Removes this instance.
            /// </summary>
            /// <typeparam name="Tc">The type of the c.</typeparam>
            public void Remove<Tc>()
            {
                _joins.Remove(typeof(Tc));

                if (_joins.Count == 0)
                    _adapter.CommandBuilder.UseTableWithFieldNames = false;
            }

            /// <summary>
            /// Removes the specified key.
            /// </summary>
            /// <param name="key">The key.</param>
            public void Remove(Type key)
            {
                _joins.Remove(key);

                if (_joins.Count == 0)
                    _adapter.CommandBuilder.UseTableWithFieldNames = false;
            }

            /// <summary>
            /// Clears this instance.
            /// </summary>
            public void Clear()
            {
                _joins.Clear();
                
                _adapter.CommandBuilder.UseTableWithFieldNames = false;
            }

            /// <summary>
            /// Gets the count.
            /// </summary>
            /// <value>The count.</value>
            public int Count
            {
                get { return _joins.Count; }
            }

            public IEnumerator<KeyValuePair<Type, LambdaExpression>> GetEnumerator()
            {
                return _joins.GetEnumerator();
            }

            System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
            {
                return _joins.GetEnumerator();
            }
        }
    }
}
