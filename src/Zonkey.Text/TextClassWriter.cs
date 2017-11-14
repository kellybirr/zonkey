using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Zonkey.Text
{
	/// <summary>
	/// Writes classes as text records to a file or stream
	/// </summary>
	/// <typeparam name="T"></typeparam>
	public class TextClassWriter<T> : TextClassRWBase<T>
		where T: class
	{
		protected TextWriter Output;
		
		/// <summary>
		/// Initializes a new instance of the <see cref="TextClassWriter&lt;T&gt;"/> class.
		/// </summary>
		/// <param name="path">The path.</param>
		public TextClassWriter(string path)
            : this(File.CreateText(path))
		{ }

		/// <summary>
		/// Initializes a new instance of the <see cref="TextClassWriter&lt;T&gt;"/> class.
		/// </summary>
		/// <param name="path">The path.</param>
		/// <param name="append">if set to <c>true</c> [append].</param>
		public TextClassWriter(string path, bool append)
            : this((append) ? File.AppendText(path) : File.CreateText(path))
		{ }

		/// <summary>
		/// Initializes a new instance of the <see cref="TextClassWriter&lt;T&gt;"/> class.
		/// </summary>
		/// <param name="stream">The stream.</param>
		public TextClassWriter(Stream stream)
            : this (new StreamWriter(stream))
		{ }

		/// <summary>
		/// Initializes a new instance of the <see cref="TextClassWriter&lt;T&gt;"/> class.
		/// </summary>
		/// <param name="stream">The stream.</param>
		/// <param name="encoding">The encoding.</param>
        public TextClassWriter(Stream stream, Encoding encoding)
            : this(new StreamWriter(stream, encoding))
		{ } 

        /// <summary>
        /// Initializes a new instance of the <see cref="TextClassWriter&lt;T&gt;"/> class.
        /// </summary>
        /// <param name="output">The output.</param>
        public TextClassWriter(TextWriter output)
        {
            Output = output;
            TextQualifyStrings = true;
        }

		/// <summary>
		/// Initializes a new instance of the <see cref="TextClassWriter&lt;T&gt;"/> class.
		/// </summary>
		protected TextClassWriter()
		{
			// for overrides
            TextQualifyStrings = true;
		}

		/// <summary>
		/// Releases unmanaged and - optionally - managed resources
		/// </summary>
		/// <param name="disposing"><c>true</c> to release both managed and unmanaged resources; <c>false</c> to release only unmanaged resources.</param>
		protected override void Dispose(bool disposing)
		{
			if (Disposed) return;

			try
			{
				if (Output != null)
					Output.Dispose();
			}
			catch (ObjectDisposedException) { }
			finally
			{
				// Call Dispose on your base class.
				base.Dispose(disposing);
			}
		}

		/// <summary>
		/// Flushes the data to the underlying stream or disk.
		/// </summary>
		public void Flush()
		{
			Output.Flush();
		}

		/// <summary>
		/// Gets or sets a value indicating whether to text qualify (quote) all fields.
		/// </summary>
		/// <value>
		/// 	<c>true</c> if [text qualify all fields]; otherwise, <c>false</c>.
		/// </value>
		public bool TextQualifyAllFields { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether to text qualify (quote) string fields.
        /// </summary>
        /// <value>
        /// 	<c>true</c> if [text qualify strings]; otherwise, <c>false</c>.
        /// </value>
        public bool TextQualifyStrings { get; set; }

		/// <summary>
		/// Gets or sets the missing delimited field behavior.
		/// </summary>
		/// <value>The missing delimited field behavior.</value>
		public MissingDelimitedFieldBehavior MissingDelimitedFieldBehavior { get; set; }

		/// <summary>
		/// Writes the specified obj.
		/// </summary>
		/// <param name="obj">The obj.</param>
		public void Write(T obj)
		{
			// init if necessary
			if (! Initialized) Initialize();

			// write record
			if (RecordType == TextRecordType.Delimited)
				WriteInternal_Delimited(obj);
			else
				WriteInternal_Fixed(obj);
		}

		/// <summary>
		/// Writes the specified obj.
		/// </summary>
		/// <param name="collection">The collection.</param>
		public void Write(IEnumerable<T> collection)
		{
			foreach (var obj in collection)
				Write(obj);
		}

		private void WriteInternal_Delimited(T obj)
		{
			var sb = new StringBuilder();
			int lastFieldPosition = -1;
			foreach (var field in FieldArray)
			{
				if (sb.Length > 0) sb.Append(Delimiter);

				// deal with missing fields
				if ( (field.Position > (lastFieldPosition + 1)) && (MissingDelimitedFieldBehavior > MissingDelimitedFieldBehavior.Ignore) )
				{
					for (int n = (lastFieldPosition + 1); n < field.Position; n++)
					{
                        if ((MissingDelimitedFieldBehavior == MissingDelimitedFieldBehavior.WriteAsEmptyString && TextQualifyStrings) || TextQualifyAllFields)
						{
							sb.Append(TextQualifier);
							sb.Append(TextQualifier);
						}

						sb.Append(Delimiter);
					}
				}
				lastFieldPosition = field.Position;

				string sValue = GetFieldValue(field, obj);
				Type pType = field.Property.PropertyType;
                if (TextQualifyAllFields || (TextQualifyStrings && (pType == typeof(string) || pType == typeof(char))))
				{
					sb.Append(TextQualifier);
					sb.Append(sValue);
					sb.Append(TextQualifier);
				}
				else
					sb.Append(sValue);
			}

			Output.WriteLine(sb.ToString());
		}

		private void WriteInternal_Fixed(T obj)
		{
			var buffer = (new String(' ', RecordLength)).ToCharArray();
			foreach (var field in FieldArray)
			{
				var sValue = GetFieldValue(field, obj).ToCharArray();
				Array.Copy(sValue, 0, buffer, field.Position, Math.Min(sValue.Length, field.Length));
			}

			Output.WriteLine(buffer, 0, RecordLength);
		}

		private static string GetFieldValue(ITextField field, T obj)
		{
			var pi = field.Property;

			object oValue = pi.GetValue(obj, null);
			if (oValue == null) return string.Empty;

			if (oValue is Boolean) return field.FormatBoolean((bool)oValue);

			if ( (! string.IsNullOrEmpty(field.OutputFormat)) && (oValue is IFormattable) )
				return ((IFormattable)oValue).ToString(field.OutputFormat, null);

			return oValue.ToString();
		}

		protected override void PostInitialize()
		{
			if (NewLine != null)
				Output.NewLine = NewLine;

			base.PostInitialize();
		}
	}

	/// <summary>
	/// How should missing fields be treated in a delimited record
	/// </summary>
	public enum MissingDelimitedFieldBehavior
	{
		/// <summary>
		/// Ignore missing fields and collapse record
		/// </summary>
		Ignore = 0,
		/// <summary>
		/// Write Missing Fields as Null (,,)
		/// </summary>
		WriteAsNull = 1,
		/// <summary>
		/// Write Missing Fields as Empty Strings (,"",)
		/// </summary>
		WriteAsEmptyString = 2
	}
}
