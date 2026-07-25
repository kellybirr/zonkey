using System;
using System.Collections.Generic;

namespace Zonkey.Extensions
{
    public static class SqlFilterExtensions
    {
        public static bool SqlIn<TField, TList>(this TField field, Func<TList, bool> filterExpression) where TList : class
        {
            throw new NotSupportedException();
        }

        public static bool SqlIn<TField, TList>(this TField field, Func<TList, TField> fieldExpression, Func<TList, bool> filterExpression) where TList : class
        {
            throw new NotSupportedException();
        }

        [Obsolete("Use list.Contains(field) in the where-expression; the translator parameterizes or inlines automatically.")]
        public static bool SqlIn<TField>(this TField field, IEnumerable<TField> options)
        {
            throw new NotSupportedException();
        }

        [Obsolete("Use list.Contains(field) in the where-expression; the translator parameterizes or inlines automatically.")]
        public static bool SqlInInt(this Int32 field, IEnumerable<Int32> options)
        {
            throw new NotSupportedException();
        }

        [Obsolete("Use list.Contains(field) in the where-expression; the translator parameterizes or inlines automatically.")]
        public static bool SqlInInt(this Int32? field, IEnumerable<Int32?> options)
        {
            throw new NotSupportedException();
        }

        [Obsolete("Use list.Contains(field) in the where-expression; the translator parameterizes or inlines automatically.")]
        public static bool SqlInInt(this Int64 field, IEnumerable<Int64> options)
        {
            throw new NotSupportedException();
        }

        [Obsolete("Use list.Contains(field) in the where-expression; the translator parameterizes or inlines automatically.")]
        public static bool SqlInInt(this Int64? field, IEnumerable<Int64?> options)
        {
            throw new NotSupportedException();
        }

        [Obsolete("Use list.Contains(field) in the where-expression; the translator parameterizes or inlines automatically.")]
        public static bool SqlInInt(this Int16 field, IEnumerable<Int16> options)
        {
            throw new NotSupportedException();
        }

        [Obsolete("Use list.Contains(field) in the where-expression; the translator parameterizes or inlines automatically.")]
        public static bool SqlInInt(this Int16? field, IEnumerable<Int16?> options)
        {
            throw new NotSupportedException();
        }

        [Obsolete("Use list.Contains(field) in the where-expression; the translator parameterizes or inlines automatically.")]
        public static bool SqlInGuid(this Guid field, IEnumerable<Guid> options)
        {
            throw new NotSupportedException();
        }

        [Obsolete("Use list.Contains(field) in the where-expression; the translator parameterizes or inlines automatically.")]
        public static bool SqlInGuid(this Guid? field, IEnumerable<Guid?> options)
        {
            throw new NotSupportedException();
        }

        /// <summary>Marker for LIKE with a caller-supplied pattern ('%'/'_' wildcards). Only valid inside Zonkey where-expressions.</summary>
        public static bool SqlLike(this string field, string pattern)
        {
            throw new NotSupportedException();
        }

        /// <summary>Marker for case-insensitive LIKE (PostgreSql: ILIKE) with a caller-supplied pattern. Only valid inside Zonkey where-expressions.</summary>
        public static bool SqlILike(this string field, string pattern)
        {
            throw new NotSupportedException();
        }

    }
}
