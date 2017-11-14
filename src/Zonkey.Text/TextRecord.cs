using System;
using System.Reflection;

namespace Zonkey.Text
{
	/// <summary>
	/// The type of record to parse form the source
	/// </summary>
	public enum TextRecordType
	{
		/// <summary>
		/// Delimited file (i.e. CSV)
		/// </summary>
		Delimited,

		/// <summary>
		/// Fixed Record Length
		/// </summary>
		FixedLength
	}

	/// <summary>
	/// Interface describing a Text Record
	/// </summary>
	public interface ITextRecord
	{
		/// <summary>
		/// Gets or sets the type of the record.
		/// </summary>
		/// <value>The type of the record.</value>
		TextRecordType RecordType { get; set; }

		/// <summary>
		/// Gets or sets the delimiter.
		/// </summary>
		/// <value>The delimiter.</value>
		char Delimiter { get; set; }

		/// <summary>
		/// Gets or sets the text qualifier.
		/// </summary>
		/// <value>The text qualifier.</value>
		char TextQualifier { get; set; }

		/// <summary>
		/// Gets or sets a value indicating whether the properties of the class are sequential.
		/// </summary>
		/// <value><c>true</c> if [sequential properties]; otherwise, <c>false</c>.</value>
		bool SequentialProperties { get; set; }

		/// <summary>
		/// Gets or sets the string value to be used for line termination (default: "\r\n").
		/// </summary>
		/// <value>The new line value.</value>
		string NewLine { get; set; }
	}

	/// <summary>
	/// Attribute describing a Text Record
	/// </summary>
	[AttributeUsage(AttributeTargets.Class, Inherited = true, AllowMultiple = false)]
	public class TextRecordAttribute : Attribute, ITextRecord
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="TextRecordAttribute"/> class.
		/// </summary>
		/// <param name="recordType">Type of the record.</param>
		public TextRecordAttribute(TextRecordType recordType)
		{
			RecordType = recordType;
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="TextRecordAttribute"/> class.
		/// </summary>
		/// <param name="recordType">Type of the record.</param>
		/// <param name="sequential">Indicates if the properties are sequential</param>
		public TextRecordAttribute(TextRecordType recordType, bool sequential)
		{
			RecordType = recordType;
			SequentialProperties = sequential;
		}

		/// <summary>
		/// Gets or sets the type of the record.
		/// </summary>
		/// <value>The type of the record.</value>
		public TextRecordType RecordType { get; set;}

		/// <summary>
		/// Gets or sets the delimiter.
		/// </summary>
		/// <value>The delimiter.</value>
		public char Delimiter { get; set; }

		/// <summary>
		/// Gets or sets the text qualifier.
		/// </summary>
		/// <value>The text qualifier.</value>
		public char TextQualifier { get; set; }

		/// <summary>
		/// Gets or sets a value indicating whether the properties of the class are sequential.
		/// </summary>
		/// <value><c>true</c> if [sequential properties]; otherwise, <c>false</c>.</value>
		public bool SequentialProperties { get; set; }

		/// <summary>
		/// Gets or sets the string value to be used for line termination (default: "\r\n").
		/// </summary>
		/// <value>The new line value.</value>
		public string NewLine { get; set; }

		/// <summary>
		/// Gets from type or member.
		/// </summary>
		/// <param name="type">The Type.</param>
		/// <returns></returns>
		internal static TextRecordAttribute GetFromType(Type type)
		{
			return (TextRecordAttribute)type.GetTypeInfo().GetCustomAttribute(typeof(TextRecordAttribute));
		}

	}
}
