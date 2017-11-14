using System;
using System.Data;
using System.Data.Common;
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
	}
}