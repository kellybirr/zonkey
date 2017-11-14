using System;
using System.Collections.Generic;
using System.Dynamic;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;

namespace Zonkey.Text
{
	public class DynamicCsvReader : IDisposable
	{
		private CsvReader _csv;
		private string[] _columns;

        #region Public Properties

        /// <summary>
        /// Gets or sets the text delimiter.
        /// </summary>
        /// <value>The text delimiter.</value>
        public char Delimiter
        {
            get { return _csv.Delimiter; }
            set { _csv.Delimiter = value; }
        }

		/// <summary>
        /// Gets or sets the text qualifier.
        /// </summary>
        /// <value>The text qualifier.</value>
        public char TextQualifier
        {
            get { return _csv.TextQualifier; }
            set { _csv.TextQualifier = value; }
        }

        /// <summary>
        /// Gets or sets the current line length.
        /// </summary>
        /// <value>The length of the current line.</value>
        public int CurrentLineLength
        {
            get { return _csv.CurrentLineLength; }
        }

        /// <summary>
        /// Gets or sets the current encoding.
        /// </summary>
        /// <value>The current encoding.</value>
        public Encoding CurrentEncoding
        {
            get { return _csv.CurrentEncoding; }
            set { _csv.CurrentEncoding = value; }
        }

        /// <summary>
        /// Gets or sets the base reader.
        /// </summary>
        /// <value>The base reader.</value>
        public CsvReader BaseReader
        {
            get { return _csv; }
            set { _csv = value; }
        }

		/// <summary>
		/// Gets a value indicating whether the data reader is closed.
		/// </summary>
		/// <value></value>
		/// <returns>true if the data reader is closed; otherwise, false.</returns>
		public bool IsClosed
		{
			get { return _csv.IsClosed; }
		}

		/// <summary>
		/// Gets or sets the line filter function.
		/// </summary>
		/// <value>The line delegate function.</value>
		public Func<int, string, bool> LineFilter 
		{ 
			get { return _csv.LineFilter;  }
			set { _csv.LineFilter = value; }
		}

		/// <summary>
		/// Gets the line number.
		/// </summary>
		/// <value>The line number.</value>
		public int LineNumber
		{
			get { return _csv.LineNumber; }
		}

		public bool ForceLowerCaseNames { get; set; }

        #endregion

        #region Constructors

        /// <summary>
        /// Initializes a new instance of the <see cref="Zonkey.Text.CsvReader"/> class.
        /// </summary>
        /// <param name="reader">The reader.</param>
        /// <param name="delimiter">The delimiter.</param>
        /// <param name="textQualifier">The text qualifier.</param>
        public DynamicCsvReader(TextReader reader, char delimiter, char textQualifier)
        {
            _csv = new CsvReader(reader, delimiter, textQualifier);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Zonkey.Text.CsvReader"/> class.
        /// </summary>
        /// <param name="reader">The reader.</param>
        /// <param name="delimiter">The delimiter.</param>
        public DynamicCsvReader(TextReader reader, char delimiter)
            : this(reader, delimiter, '"')
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Zonkey.Text.CsvReader"/> class.
        /// </summary>
        /// <param name="reader">The reader.</param>
        public DynamicCsvReader(TextReader reader)
            : this(reader, ',', '"')
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Zonkey.Text.CsvReader"/> class.
        /// </summary>
        /// <param name="stream">The stream.</param>
        /// <param name="delimiter">The delimiter.</param>
        /// <param name="textQualifier">The text qualifier.</param>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Reliability", "CA2000:DisposeObjectsBeforeLosingScope")]
        public DynamicCsvReader(Stream stream, char delimiter, char textQualifier)
        {
			_csv = new CsvReader(stream, delimiter, textQualifier);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Zonkey.Text.CsvReader"/> class.
        /// </summary>
        /// <param name="stream">The stream.</param>
        /// <param name="delimiter">The delimiter.</param>
        public DynamicCsvReader(Stream stream, char delimiter)
            : this(stream, delimiter, '"')
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Zonkey.Text.CsvReader"/> class.
        /// </summary>
        /// <param name="stream">The stream.</param>
        public DynamicCsvReader(Stream stream)
            : this(stream, ',', '"')
        {
        }

        #endregion

		#region Public Methods

		/// <summary>
		/// Closes the <see cref="T:System.Data.IDataReader"/> Object.
		/// </summary>
		public void Close()
		{
			_csv.Close();
		}

		/// <summary>
		/// Advances the <see cref="T:System.Data.IDataReader"/> to the next record.
		/// </summary>
		/// <returns>
		/// a dynamic object from the text data.
		/// </returns>
		public dynamic Read()
		{
			if (_columns == null)
			{
				if (! ReadColumns())
					return null;

				if (_columns == null)
					return null;
			}

			if (!_csv.Read())
				return null;

			IDictionary<string, object> obj = new ExpandoObject();
			for (int c = 0; c < _csv.FieldCount; c++)
			{
				obj[_columns[c]] = _csv[c];
			}

			return obj;
		}

		private bool ReadColumns()
		{
			if (!_csv.Read())
				return false;

			_columns = new string[_csv.FieldCount];
			if (_columns.Length < 1) return false;

			for (int c = 0; c < _csv.FieldCount; c++)
			{
				_columns[c] = Regex.Replace(_csv.GetString(c), @"[^\w]", "_");
				if (ForceLowerCaseNames)
					_columns[c] = _columns[c].ToLowerInvariant();
			}
				
			return true;
		}

		#endregion

		#region IDisposable Members

		/// <summary>
		/// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
		/// </summary>
		public void Dispose()
		{
			if (! IsClosed) Close();
		}

		#endregion
	}
}
