using System;
using System.Collections.Generic;
using System.Data.Common;

namespace Zonkey.ObjectModel
{
    /// <summary>
    /// Provides a thread-safe implementation of a generic <see cref="System.Collections.Generic.Dictionary&lt;T, T&gt;"/>
    /// </summary>
    public class FieldValuesDictionary : Dictionary<string, object>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="FieldValuesDictionary"/> class.
        /// </summary>
        public FieldValuesDictionary()
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="FieldValuesDictionary"/> class.
        /// </summary>
        /// <param name="reader">The reader.</param>
        public FieldValuesDictionary(DbDataReader reader)
        {
            if (reader == null)
                throw new ArgumentNullException(nameof(reader));

            lock (this)
            {
                for (int i = 0; i < reader.VisibleFieldCount; i++)
                    Add(reader.GetName(i), reader[i]);
            }
        }
    }
}