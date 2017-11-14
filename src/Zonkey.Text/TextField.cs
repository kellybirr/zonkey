using System;
using System.Globalization;
using System.Reflection;

namespace Zonkey.Text
{
	/// <summary>
	/// 
	/// </summary>
	public interface ITextField
	{
		/// <summary>
		/// Gets or sets the position of the field.
		/// </summary>
		/// <value>The position.</value>
		int Position { get; set; }

		/// <summary>
		/// Gets or sets the length of the field.
		/// </summary>
		/// <value>The length.</value>
		int Length { get; set; }

		/// <summary>
		/// Gets or sets the property.
		/// </summary>
		/// <value>The property.</value>
		PropertyInfo Property { get; set; }

		/// <summary>
		/// Gets or sets the number style used when parsing numbers.
		/// </summary>
		/// <value>The number style.</value>
		NumberStyles NumberStyle { get; set; }

		/// <summary>
		/// Gets or sets the date time style used when parsing dates.
		/// </summary>
		/// <value>The date time style.</value>
		DateTimeStyles DateTimeStyle { get; set; }

		/// <summary>
		/// Gets or sets the output format.
		/// </summary>
		/// <value>The output format.</value>
		string OutputFormat { get; set; }

		/// <summary>
		/// Gets or sets the boolean true values (comma-separated).
		/// </summary>
		/// <value>The boolean true values.</value>
		string BooleanTrue { get; set; }

		/// <summary>
		/// Formats the boolean.
		/// </summary>
		/// <param name="value">if set to <c>true</c> [value].</param>
		/// <returns></returns>
		string FormatBoolean(bool value);
	}

	/// <summary>
	/// 
	/// </summary>
	public class TextField : ITextField
	{
		/// <summary>
		/// Defines and undefined position to be calculated
		/// </summary>
		public const int AutoPosition = -1;

		/// <summary>
		/// The default Boolean true values
		/// </summary>
		public const string DefaultBooleanTrue = "T,t,Y,y,1";

		/// <summary>
		/// Initializes a new instance of the <see cref="TextField"/> class.
		/// </summary>
		/// <param name="pi">The property.</param>
		public TextField(PropertyInfo pi)
		{
			Property = pi;
			Position = AutoPosition;
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="TextField"/> class.
		/// </summary>
		/// <param name="pi">The property.</param>
		/// <param name="position">The position.</param>
		public TextField(PropertyInfo pi, int position)
		{
			Property = pi;
			Position = position;
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="TextField"/> class.
		/// </summary>
		/// <param name="pi">The property.</param>
		/// <param name="position">The position.</param>
		/// <param name="length">The length.</param>
		public TextField(PropertyInfo pi, int position, int length)
		{
			Property = pi;
			Position = position;
			Length = length;
		}

		/// <summary>
		/// Gets or sets the position of the field.
		/// </summary>
		/// <value>The position.</value>
		public int Position { get; set; }

		/// <summary>
		/// Gets or sets the length of the field.
		/// </summary>
		/// <value>The length.</value>
		public int Length { get; set; }

		/// <summary>
		/// Gets or sets the property.
		/// </summary>
		/// <value>The property.</value>
		public PropertyInfo Property { get; set; }

		/// <summary>
		/// Gets or sets the number style used when parsing numbers.
		/// </summary>
		/// <value>The number style.</value>
		public NumberStyles NumberStyle { get; set; }

		/// <summary>
		/// Gets or sets the date time style used when parsing dates.
		/// </summary>
		/// <value>The date time style.</value>
		public DateTimeStyles DateTimeStyle { get; set; }

		/// <summary>
		/// Gets or sets the output format.
		/// </summary>
		/// <value>The output format.</value>
		public string OutputFormat { get; set; }

		/// <summary>
		/// Gets or sets the Boolean true values (comma-separated).
		/// </summary>
		/// <value>The Boolean true values.</value>
		public string BooleanTrue { get; set; }

		/// <summary>
		/// Formats the Boolean.
		/// </summary>
		/// <param name="value">if set to <c>true</c> [value].</param>
		/// <returns></returns>
		public string FormatBoolean(bool value)
		{
			if (string.IsNullOrEmpty(OutputFormat))
				return value.ToString();

			if (_boolFormats == null)
			{
				_boolFormats = OutputFormat.Split(new[] {'|'});
				if (_boolFormats.Length != 2)
					throw new InvalidOperationException("Invalid Boolean Output Format, missing '|'");
			}

			return (value) ? _boolFormats[0] : _boolFormats[1];
		}
		private string[] _boolFormats;
	}

	/// <summary>
	/// Text Field Attribute
	/// </summary>
	[AttributeUsage(AttributeTargets.Property, Inherited = true, AllowMultiple = false)]
	public class TextFieldAttribute : Attribute, ITextField
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="TextFieldAttribute"/> class.
		/// </summary>
		public TextFieldAttribute()
		{
			Position = TextField.AutoPosition;
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="TextFieldAttribute"/> class.
		/// </summary>
		/// <param name="position">The position.</param>
		public TextFieldAttribute(int position)
		{
			Position = position;
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="TextFieldAttribute"/> class.
		/// </summary>
		/// <param name="position">The position.</param>
		/// <param name="length">The length.</param>
		public TextFieldAttribute(int position, int length)
		{
			Position = position;
			Length = length;
		}

		/// <summary>
		/// Gets or sets the position of the field.
		/// </summary>
		/// <value>The position.</value>
		public int Position { get; set; }

		/// <summary>
		/// Gets or sets the length of the field.
		/// </summary>
		/// <value>The length.</value>
		public int Length { get; set; }

		/// <summary>
		/// Gets or sets the property.
		/// </summary>
		/// <value>The property.</value>
		public PropertyInfo Property { get; set; }

		/// <summary>
		/// Gets or sets the number style used when parsing numbers.
		/// </summary>
		/// <value>The number style.</value>
		public NumberStyles NumberStyle { get; set; }

		/// <summary>
		/// Gets or sets the date time style used when parsing dates.
		/// </summary>
		/// <value>The date time style.</value>
		public DateTimeStyles DateTimeStyle { get; set; }

		/// <summary>
		/// Gets or sets the output format.
		/// </summary>
		/// <value>The output format.</value>
		public string OutputFormat { get; set; }

		/// <summary>
		/// Gets or sets the Boolean true values (comma-separated).
		/// </summary>
		/// <value>The Boolean true values.</value>
		public string BooleanTrue { get; set; }

		/// <summary>
		/// Formats the Boolean.
		/// </summary>
		/// <param name="value">if set to <c>true</c> [value].</param>
		/// <returns></returns>
		public string FormatBoolean(bool value)
		{
			if (string.IsNullOrEmpty(OutputFormat))
				return value.ToString();

			if (_boolFormats == null)
			{
				_boolFormats = OutputFormat.Split(new[] { '|' });
				if (_boolFormats.Length != 2)
					throw new InvalidOperationException("Invalid Boolean Output Format, missing '|'");
			}

			return (value) ? _boolFormats[0] : _boolFormats[1];
		}
		private string[] _boolFormats;

		/// <summary>
		/// Gets from property.
		/// </summary>
		/// <param name="pi">The pi.</param>
		/// <returns></returns>
		public static TextFieldAttribute GetFromProperty(PropertyInfo pi)
		{
			var attr = (TextFieldAttribute)pi.GetCustomAttribute(typeof(TextFieldAttribute));
			if (attr != null) attr.Property = pi;

			return attr;
		}
	}
}
