using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Linq;
using System.Text;
using Zonkey.Dialects;

namespace Zonkey
{
	/// <summary>
	/// Provides methods for creating WHERE clauses using filters
	/// </summary>
	public abstract class SqlFilter
	{
		/// <summary>
		/// Creates a SQL 'is equal to' WHERE clause.
		/// </summary>
		/// <param name="fieldName">Name of the database field.</param>
		/// <param name="value">The value to compare.</param>
		/// <returns>A <see cref="Zonkey.SqlFilter"/> object (WHERE fieldName = value).</returns>
		public static SqlFilter EQ(string fieldName, object value)
		{
			return new SimpleSqlFilter(fieldName, "=", value);
		}

		/// <summary>
		/// Creates a SQL 'is not equal to' WHERE clause.
		/// </summary>
		/// <param name="fieldName">Name of the database field.</param>
		/// <param name="value">The value to compare.</param>
		/// <returns>A <see cref="Zonkey.SqlFilter"/> object (WHERE fieldName != value).</returns>
		[System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1705:LongAcronymsShouldBePascalCased", MessageId = "Member")]
		public static SqlFilter NEQ(string fieldName, object value)
		{
			return new SimpleSqlFilter(fieldName, "!=", value);
		}

		/// <summary>
		/// Creates a SQL 'is greater than' WHERE clause.
		/// </summary>
		/// <param name="fieldName">Name of the database field.</param>
		/// <param name="value">The value to compare.</param>
		/// <returns>A <see cref="Zonkey.SqlFilter"/> object (WHERE fieldName &gt; value).</returns>
		public static SqlFilter GT(string fieldName, object value)
		{
			return new SimpleSqlFilter(fieldName, ">", value);
		}

		/// <summary>
		/// Creates a SQL 'is greater than or equal to' WHERE clause.
		/// </summary>
		/// <param name="fieldName">Name of the database field.</param>
		/// <param name="value">The value to compare.</param>
		/// <returns>A <see cref="Zonkey.SqlFilter"/> object (WHERE fieldName &gt;= value).</returns>
		[System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1705:LongAcronymsShouldBePascalCased", MessageId = "Member")]
		public static SqlFilter GTE(string fieldName, object value)
		{
			return new SimpleSqlFilter(fieldName, ">=", value);
		}

		/// <summary>
		/// Creates a SQL 'is not greater than' WHERE clause.
		/// </summary>
		/// <param name="fieldName">Name of the database field.</param>
		/// <param name="value">The value to compare.</param>
		/// <returns>A <see cref="Zonkey.SqlFilter"/> object (WHERE fieldName !&gt; value).</returns>
		[System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1705:LongAcronymsShouldBePascalCased", MessageId = "Member")]
		public static SqlFilter NGT(string fieldName, object value)
		{
			return new SimpleSqlFilter(fieldName, "!>", value);
		}

		/// <summary>
		/// Creates a SQL 'is less than' WHERE clause.
		/// </summary>
		/// <param name="fieldName">Name of the database field.</param>
		/// <param name="value">The value to compare.</param>
		/// <returns>A <see cref="Zonkey.SqlFilter"/> object (WHERE fieldName &lt; value).</returns>
		public static SqlFilter LT(string fieldName, object value)
		{
			return new SimpleSqlFilter(fieldName, "<", value);
		}

		/// <summary>
		/// Creates a SQL 'is less than or equal to' WHERE clause.
		/// </summary>
		/// <param name="fieldName">Name of the database field.</param>
		/// <param name="value">The value to compare.</param>
		/// <returns>A <see cref="Zonkey.SqlFilter"/> object (WHERE fieldName &lt;= value).</returns>
		[System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1705:LongAcronymsShouldBePascalCased", MessageId = "Member")]
		public static SqlFilter LTE(string fieldName, object value)
		{
			return new SimpleSqlFilter(fieldName, "<=", value);
		}

		/// <summary>
		/// Creates a SQL 'is not less than' WHERE clause.
		/// </summary>
		/// <param name="fieldName">Name of the database field.</param>
		/// <param name="value">The value to compare.</param>
		/// <returns>A <see cref="Zonkey.SqlFilter"/> object (WHERE fieldName !&lt; value).</returns>
		[System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1705:LongAcronymsShouldBePascalCased", MessageId = "Member")]
		public static SqlFilter NLT(string fieldName, object value)
		{
			return new SimpleSqlFilter(fieldName, "!<", value);
		}

		/// <summary>
		/// Creates a SQL 'is null' WHERE clause.
		/// </summary>
		/// <param name="fieldName">Name of the database field.</param>
		/// <returns>A <see cref="Zonkey.SqlFilter"/> object (WHERE fieldName IS NULL).</returns>
		[System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1705:LongAcronymsShouldBePascalCased", MessageId = "Member")]
		public static SqlFilter NULL(string fieldName)
		{
			return new SqlNullFilter(fieldName, true);
		}

		/// <summary>
		/// Creates a SQL 'is not null' WHERE clause.
		/// </summary>
		/// <param name="fieldName">Name of the database field.</param>
		/// <returns>A <see cref="Zonkey.SqlFilter"/> object (WHERE fieldName IS NOT NULL).</returns>
		[System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1705:LongAcronymsShouldBePascalCased", MessageId = "Member")]
		public static SqlFilter NOTNULL(string fieldName)
		{
			return new SqlNullFilter(fieldName, false);
		}

		/// <summary>
		/// Creates a SQL 'is like' WHERE clause.
		/// </summary>
		/// <param name="fieldName">Name of the database field.</param>
		/// <param name="value">The value to compare.</param>
		/// <returns>A <see cref="Zonkey.SqlFilter"/> object (WHERE fieldName !&lt; value).</returns>
		public static SqlFilter LIKE(string fieldName, string value)
		{
			return new SimpleSqlFilter(fieldName, "LIKE", value);
		}

		/// <summary>
		/// Creates a SQL 'is not like' WHERE clause.
		/// </summary>
		/// <param name="fieldName">Name of the database field.</param>
		/// <param name="value">The value to compare.</param>
		/// <returns>A <see cref="Zonkey.SqlFilter"/> object (WHERE fieldName !&lt; value).</returns>
		public static SqlFilter NOTLIKE(string fieldName, string value)
		{
			return new SimpleSqlFilter(fieldName, "NOT LIKE", value);
		}

		/// <summary>
		/// Creates a SQL 'is ilike' WHERE clause.
		/// </summary>
		/// <param name="fieldName">Name of the database field.</param>
		/// <param name="value">The value to compare.</param>
		/// <returns>A <see cref="Zonkey.SqlFilter"/> object (WHERE fieldName !&lt; value).</returns>
		public static SqlFilter ILIKE(string fieldName, string value)
		{
			return new SimpleSqlFilter(fieldName, "ILIKE", value);
		}

		/// <summary>
		/// Creates a SQL 'is not ilike' WHERE clause.
		/// </summary>
		/// <param name="fieldName">Name of the database field.</param>
		/// <param name="value">The value to compare.</param>
		/// <returns>A <see cref="Zonkey.SqlFilter"/> object (WHERE fieldName !&lt; value).</returns>
		public static SqlFilter NOTILIKE(string fieldName, string value)
		{
			return new SimpleSqlFilter(fieldName, "NOT ILIKE", value);
		}

		/// <summary>
		/// Creates a SQL '~' WHERE clause.
		/// </summary>
		/// <param name="fieldName">Name of the database field.</param>
		/// <param name="value">The value to compare.</param>
		/// <returns>A <see cref="Zonkey.SqlFilter"/> object (WHERE fieldName !&lt; value).</returns>
		public static SqlFilter MATCH(string fieldName, string regex)
		{
			return new SimpleSqlFilter(fieldName, "~", regex);
		}

		/// <summary>
		/// Creates a SQL '!~' WHERE clause.
		/// </summary>
		/// <param name="fieldName">Name of the database field.</param>
		/// <param name="value">The value to compare.</param>
		/// <returns>A <see cref="Zonkey.SqlFilter"/> object (WHERE fieldName !&lt; value).</returns>
		public static SqlFilter NOTMATCH(string fieldName, string value)
		{
			return new SimpleSqlFilter(fieldName, "!~", value);
		}

		/// <summary>
		/// Creates a SQL '~*' WHERE clause.
		/// </summary>
		/// <param name="fieldName">Name of the database field.</param>
		/// <param name="value">The value to compare.</param>
		/// <returns>A <see cref="Zonkey.SqlFilter"/> object (WHERE fieldName !&lt; value).</returns>
		public static SqlFilter IMATCH(string fieldName, string regex)
		{
			return new SimpleSqlFilter(fieldName, "~*", regex);
		}

		/// <summary>
		/// Creates a SQL '!~*' WHERE clause.
		/// </summary>
		/// <param name="fieldName">Name of the database field.</param>
		/// <param name="value">The value to compare.</param>
		/// <returns>A <see cref="Zonkey.SqlFilter"/> object (WHERE fieldName !&lt; value).</returns>
		public static SqlFilter NOTIMATCH(string fieldName, string regex)
		{
			return new SimpleSqlFilter(fieldName, "!~*", regex);
		}

		/// <summary>
		/// Creates a SQL 'is in list' WHERE clause.
		/// Behavior matches the where-expression Contains() translation: an empty (or all-null)
		/// list renders an always-false predicate, a single value collapses to an equality, and a
		/// multi-value list binds as one typed-array collection parameter on dialects that support
		/// it (PostgreSql: field = ANY(:pN)) or as individual parameters (field IN (:pN_0, :pN_1, ...))
		/// everywhere else. Null and DBNull elements are ignored (IN (NULL) never matches).
		/// </summary>
		/// <typeparam name="T">The element type of the list.</typeparam>
		/// <param name="fieldName">Name of the database field.</param>
		/// <param name="values">The values to compare.</param>
		/// <returns>A <see cref="Zonkey.SqlFilter"/> object (WHERE fieldName IN (values)).</returns>
		[System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1705:LongAcronymsShouldBePascalCased", MessageId = "Member")]
		public static SqlFilter IN<T>(string fieldName, IEnumerable<T> values)
		{
			return new SqlInFilter(fieldName, values?.Cast<object>() ?? throw new ArgumentNullException(nameof(values)));
		}

		private SqlFilter(string fieldName)
		{
			FieldName = fieldName;
		}

		/// <summary>
		/// Gets or sets the name of the database field.
		/// </summary>
		/// <value>The name of the database field.</value>
		public string FieldName { get; set; }

		/// <summary>
		/// Gets or sets the value to compare.
		/// </summary>
		/// <value>The value to compare.</value>
		public virtual object Value
		{
			get { return _value; }
			set { _value = value; }
		}
		private object _value;

		/// <summary>
		/// Formats the SqlFilter as a string.
		/// </summary>
		/// <param name="dialect">The SQL dialect.</param>
		/// <param name="paramIndex">Index of the param.</param>
		/// <returns>A string representation of the SqlFilter</returns>
		public abstract string ToString(SqlDialect dialect, int paramIndex);

		/// <summary>
		/// Adds to command params.
		/// </summary>
		/// <param name="command">The command.</param>
		/// <param name="dialect">The dialect.</param>
		/// <param name="index">The index.</param>
		public abstract void AddToCommandParams(DbCommand command, SqlDialect dialect, int index);

		private class SimpleSqlFilter : SqlFilter
		{
			private readonly string _operator;

			protected internal SimpleSqlFilter(string fieldName, string op, object value)
				: base(fieldName)
			{
				_operator = op;
				_value = value;
			}

			public override string ToString(SqlDialect dialect, int paramIndex)
			{
				if (dialect == null) throw new ArgumentNullException(nameof(dialect));

				var sb = new StringBuilder();
				sb.Append(dialect.FormatFieldName(FieldName));
				sb.Append(' ');
				sb.Append(_operator);
				sb.Append(' ');
				sb.Append(dialect.FormatParameterName(paramIndex, CommandType.Text));

				return sb.ToString();
			}

			public override void AddToCommandParams(DbCommand command, SqlDialect dialect, int index)
			{
				if (command == null) throw new ArgumentNullException(nameof(command));
				if (dialect == null) throw new ArgumentNullException(nameof(dialect));

				DbParameter newParm = command.CreateParameter();
				newParm.ParameterName = dialect.FormatParameterName(index, command.CommandType);
				newParm.Value = (_value ?? DBNull.Value);

				command.Parameters.Add(newParm);
			}
		} ;

		private class SqlNullFilter : SqlFilter
		{
			private readonly bool _isNull;

			protected internal SqlNullFilter(string fieldName, bool isNull)
				: base(fieldName)
			{
				_isNull = isNull;
			}

			public override object Value
			{
				get { return DBNull.Value; }
				set { return; }
			}

			public override string ToString(SqlDialect dialect, int paramIndex)
			{
				if (dialect == null) throw new ArgumentNullException(nameof(dialect));

				var sb = new StringBuilder();
				sb.Append(dialect.FormatFieldName(FieldName));
				sb.Append((_isNull) ? " IS NULL" : " IS NOT NULL");
				return sb.ToString();
			}

			public override void AddToCommandParams(DbCommand command, SqlDialect dialect, int index)
			{
				return;
			}
		} ;

		private class SqlInFilter : SqlFilter
		{
			private readonly object[] _values;

			protected internal SqlInFilter(string fieldName, IEnumerable<object> values)
				: base(fieldName)
			{
				_values = values.Where(v => v != null && v != DBNull.Value).ToArray();
				_value = _values;
			}

			public override string ToString(SqlDialect dialect, int paramIndex)
			{
				if (dialect == null) throw new ArgumentNullException(nameof(dialect));

				// deliberately not `field IN (NULL)`: that is UNKNOWN, not FALSE (see ExpressionTranslator)
				if (_values.Length == 0)
					return "1 = 0 /* IN (): empty or all-null list */";

				string field = dialect.FormatFieldName(FieldName);

				// a one-value IN is an equality: shares a cached plan with hand-written equality
				// filters and keeps the value visible to the PostgreSql planner (same collapse as
				// the where-expression surface)
				if (_values.Length == 1)
					return string.Concat(field, " = ", dialect.FormatParameterName(paramIndex, CommandType.Text));

				if (UseCollectionParameter(dialect))
					return dialect.RenderInCollectionParameter(field, dialect.FormatParameterName(paramIndex, CommandType.Text));

				var sb = new StringBuilder();
				sb.Append(field).Append(" IN (");
				for (int i = 0; i < _values.Length; i++)
				{
					if (i > 0) sb.Append(',');
					sb.Append(dialect.FormatParameterName(SubName(paramIndex, i), CommandType.Text));
				}
				sb.Append(')');
				return sb.ToString();
			}

			public override void AddToCommandParams(DbCommand command, SqlDialect dialect, int index)
			{
				if (command == null) throw new ArgumentNullException(nameof(command));
				if (dialect == null) throw new ArgumentNullException(nameof(dialect));

				if (_values.Length == 0) return;

				if (_values.Length == 1)
				{
					AddParameter(command, dialect.FormatParameterName(index, command.CommandType), _values[0]);
					return;
				}

				if (UseCollectionParameter(dialect))
				{
					TryBuildTypedArray(out Array typedArray);
					AddParameter(command, dialect.FormatParameterName(index, command.CommandType), typedArray);
					return;
				}

				for (int i = 0; i < _values.Length; i++)
					AddParameter(command, dialect.FormatParameterName(SubName(index, i), command.CommandType), _values[i]);
			}

			private static void AddParameter(DbCommand command, string name, object value)
			{
				DbParameter newParm = command.CreateParameter();
				newParm.ParameterName = name;
				newParm.Value = value;
				command.Parameters.Add(newParm);
			}

			// one typed-array parameter (e.g. PostgreSql = ANY): one stable plan, no parameter-count limit
			private bool UseCollectionParameter(SqlDialect dialect)
				=> dialect.SupportsInCollectionParameter(_values[0].GetType())
					&& TryBuildTypedArray(out _);

			// mixed-runtime-type lists fall through to individual parameters instead of throwing
			private bool TryBuildTypedArray(out Array array)
			{
				Type elementType = _values[0].GetType();
				var candidate = Array.CreateInstance(elementType, _values.Length);
				for (int i = 0; i < _values.Length; i++)
				{
					if (_values[i].GetType() != elementType)
					{
						array = null;
						return false;
					}

					candidate.SetValue(_values[i], i);
				}

				array = candidate;
				return true;
			}

			private static string SubName(int index, int sub)
				=> string.Concat("p", index.ToString(System.Globalization.CultureInfo.InvariantCulture), "_", sub.ToString(System.Globalization.CultureInfo.InvariantCulture));
		} ;
	}
}