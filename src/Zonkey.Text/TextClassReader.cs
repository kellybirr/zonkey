using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;

namespace Zonkey.Text
{
	/// <summary>
	/// Reads text sources such as files or streams and parses them into strongly typed objects.
	/// </summary>
	/// <typeparam name="T">The type of the class to parse the input data into</typeparam>
	public class TextClassReader<T> : TextClassRWBase<T>, IEnumerable<T> 
		where T : class, new()
	{
		protected TextReader Input;
		
		private CsvReader _csv;
		private int _lineNumber;

		/// <summary>
		/// Initializes a new instance of the <see cref="TextClassReader&lt;T&gt;"/> class.
		/// </summary>
		/// <param name="path">The path.</param>
		public TextClassReader(string path)
		{
			Input = new StreamReader(File.OpenRead(path));            
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="TextClassReader&lt;T&gt;"/> class.
		/// </summary>
		/// <param name="path">The path.</param>
		/// <param name="encoding">The encoding.</param>
		public TextClassReader(string path, Encoding encoding)
		{
			Input = new StreamReader(File.OpenRead(path), encoding);
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="TextClassReader&lt;T&gt;"/> class.
		/// </summary>
		/// <param name="source">The source.</param>
		public TextClassReader(TextReader source)
		{
			Input = source;
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="TextClassReader&lt;T&gt;"/> class.
		/// </summary>
		/// <param name="stream">The stream.</param>
		public TextClassReader(Stream stream)
		{
			Input = new StreamReader(stream);
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="TextClassReader&lt;T&gt;"/> class.
		/// </summary>
		/// <param name="stream">The stream.</param>
		/// <param name="encoding">The encoding.</param>
		public TextClassReader(Stream stream, Encoding encoding)
		{
			Input = new StreamReader(stream, encoding);
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="TextClassReader&lt;T&gt;"/> class.
		/// </summary>
		protected TextClassReader()
		{
			// for overrides
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
				if (_csv != null) 
					_csv.Dispose();

				if (Input != null) 
					Input.Dispose();
			}
			catch (ObjectDisposedException) { }
			finally
			{
				// Call Dispose on your base class.
				base.Dispose(disposing);
			}
		}

		/// <summary>
		/// Returns an enumerator that iterates through the collection.
		/// </summary>
		/// <returns>
		/// A <see cref="T:System.Collections.Generic.IEnumerator`1"/> that can be used to iterate through the collection.
		/// </returns>
		public IEnumerator<T> GetEnumerator()
		{
			T obj;
			while ( (obj = Read()) != null )
				yield return obj;
		}

		/// <summary>
		/// Returns an enumerator that iterates through a collection.
		/// </summary>
		/// <returns>
		/// An <see cref="T:System.Collections.IEnumerator"/> object that can be used to iterate through the collection.
		/// </returns>
		System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
		{
			return GetEnumerator();
		}

		/// <summary>
		/// Reads the next record from the file
		/// </summary>
		/// <returns></returns>
		public T Read()
		{
			// init if necessary
			if (! Initialized) Initialize();

			string sFixedLine = null;			
			if (_csv != null)
			{
				if (!_csv.Read()) return null;
			}
			else
			{
				sFixedLine = ReadLine();
				if (sFixedLine == null) return null;
			}

			var obj = new T();
			foreach (ITextField field in FieldArray)
			{
				string sValue;
				if (_csv != null)
					sValue = (field.Length > 0)
								? _csv.GetString(field.Position, field.Length).Trim()
								: _csv.GetString(field.Position).Trim();
				else if (sFixedLine != null)
				{
					if (sFixedLine.Length <= field.Position)
						continue;

					sValue = (sFixedLine.Length > (field.Position + field.Length)) 
						? sFixedLine.Substring(field.Position, field.Length).Trim()
						: sFixedLine.Substring(field.Position).Trim();						
				}
				else
					continue;

				if (string.IsNullOrEmpty(sValue)) continue;

				PropertyInfo pi = field.Property;
				TypeInfo propType = pi.PropertyType.GetTypeInfo();
				if (propType.IsEnum) propType = Enum.GetUnderlyingType(pi.PropertyType).GetTypeInfo();

				try
				{
					if (propType.IsAssignableFrom(typeof(String)))
						pi.SetValue(obj, sValue, null);
					else if (propType.IsAssignableFrom(typeof(Char)))
						pi.SetValue(obj, Convert.ToChar(sValue), null);
					else if (propType.IsAssignableFrom(typeof(Guid)))
						pi.SetValue(obj, new Guid(sValue), null);
					else if (propType.IsAssignableFrom(typeof(DateTime)))
						pi.SetValue(obj, DateTime.Parse(sValue, null, field.DateTimeStyle), null);
					else if (propType.IsAssignableFrom(typeof(Decimal)))
						pi.SetValue(obj, Decimal.Parse(sValue, field.NumberStyle), null);
					else if (propType.IsAssignableFrom(typeof(Double)))
						pi.SetValue(obj, Double.Parse(sValue, field.NumberStyle), null);
					else if (propType.IsAssignableFrom(typeof(Single)))
						pi.SetValue(obj, Single.Parse(sValue, field.NumberStyle), null);
					else if (propType.IsAssignableFrom(typeof(Int64)))
						pi.SetValue(obj, Int64.Parse(sValue, field.NumberStyle), null);
					else if (propType.IsAssignableFrom(typeof(Int32)))
						pi.SetValue(obj, Int32.Parse(sValue, field.NumberStyle), null);
					else if (propType.IsAssignableFrom(typeof(Int16)))
						pi.SetValue(obj, Int16.Parse(sValue, field.NumberStyle), null);
					else if (propType.IsAssignableFrom(typeof(Byte)))
						pi.SetValue(obj, Byte.Parse(sValue, field.NumberStyle), null);
					else if (propType.IsAssignableFrom(typeof(Boolean)))
						pi.SetValue(obj, GetBoolean(sValue, field.BooleanTrue), null);
					else
						pi.SetValue(obj, Convert.ChangeType(sValue, pi.PropertyType), null);
				}
				catch (Exception ex)
				{
					var s = string.Format("Unable to set Property `{0}` of Type `{1}` to value `{2}`", pi.Name, pi.PropertyType.Name, sValue);
					throw new InvalidDataException(s, ex);
				}
			}

			return obj;
		}

		private static bool GetBoolean(string s, string trueValues)
		{
			if (string.IsNullOrEmpty(s))
				return false;

			return (! string.IsNullOrEmpty(trueValues))
				? trueValues.Contains(s) 
				: TextField.DefaultBooleanTrue.Contains(s.Substring(0,1));
		}

		/// <summary>
		/// Reads a line of raw text and advances the stream position.
		/// </summary>
		/// <returns>string</returns>
		public string ReadLine()
		{
			_lineNumber++;
			string sLine = Input.ReadLine();

			// end of file
			if (sLine == null) return null;

			// check for short lines
			if (sLine.Length < RecordLength)
			{
				switch (ShortRecordBehaviour)
				{
					case ShortRecordBehaviour.Exception:
						throw new Exception(string.Format("Line {0} was only {1} chars, expected {2} chars.", _lineNumber, sLine.Length, RecordLength));
					case ShortRecordBehaviour.Skip:
						return ReadLine();
				}
			}

			// apply line filter
			if ((LineFilter != null) && (! LineFilter(_lineNumber, sLine)))
				return ReadLine();

			return sLine;
		}

		/// <summary>
		/// Gets or sets the line filter function.
		/// </summary>
		/// <value>The line delegate function.</value>
		public Func<int, string, bool> LineFilter 
		{
			get { return _lineFilter; } 
			set
			{
				_lineFilter = value;

				if (_csv != null)
					_csv.LineFilter = value;
			}
		}

		private Func<int, string, bool> _lineFilter;


		/// <summary>
		/// Fills the specified collection.
		/// </summary>
		/// <param name="collection">The collection.</param>
		/// <returns></returns>
		public int Fill(ICollection<T> collection)
		{
			return Fill(collection, null);
		}

		/// <summary>
		/// Fills the specified collection.
		/// </summary>
		/// <param name="collection">The collection.</param>
		/// <param name="filter">The filter function.</param>
		/// <returns></returns>
		public int Fill(ICollection<T> collection, Predicate<T> filter)
		{
			int count = 0;
			T obj;

			if (filter != null)
			{
				while ((obj = Read()) != null)
				{
					if (! filter(obj)) continue;

					collection.Add(obj);
					count++;
				}
			}
			else
			{
				while ((obj = Read()) != null)
				{
					collection.Add(obj);
					count++;
				}
			}

			return count;
		}

		/// <summary>
		/// Initializes the CSV reader.
		/// </summary>
		protected override void PostInitialize()
		{
			if (RecordType == TextRecordType.Delimited)
			{
				_csv = new CsvReader(Input, Delimiter, TextQualifier)
						{
							LineFilter = _lineFilter
						};
			}

			base.PostInitialize();
		}

		/// <summary>
		/// Defined the behavior of the reader when the line is shorter than the record length.
		/// </summary>
		/// <value>The short record behavior.</value>
		public ShortRecordBehaviour ShortRecordBehaviour { get; set; }

		/// <summary>
		/// Gets the line number last read.
		/// </summary>
		/// <value>The line number.</value>
		public int LineNumber => _csv?.LineNumber ?? _lineNumber;
	}

	/// <summary>
	/// Defined the behavior of the reader when the line is shorter than the record length.
	/// </summary>
	public enum ShortRecordBehaviour
	{
		/// <summary>
		/// Accept short records and leave unread values null
		/// </summary>
		Accept,

		/// <summary>
		/// Silently skip short records
		/// </summary>
		Skip,

		/// <summary>
		/// Throw an exception on a short record
		/// </summary>
		Exception
	}
}
