using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.IO;
using System.Text;

namespace Zonkey.Text
{
    /// <summary>
    /// Provides methods and properties for working with CSV (comma-separated values) files.
    /// </summary>
    public sealed class CsvReader : IDataReader
    {
        private const NumberStyles INT_STYLE = NumberStyles.Integer | NumberStyles.AllowTrailingSign | NumberStyles.AllowThousands;
        private readonly List<string> _record = new List<string>();
        private string _sourceLine;

        #region Public Properties

        /// <summary>
        /// Gets or sets the text delimiter.
        /// </summary>
        /// <value>The text delimiter.</value>
        public char Delimiter
        {
            get { return _delimiter; }
            set { _delimiter = value; }
        }

        private char _delimiter;

        /// <summary>
        /// Gets or sets the text qualifier.
        /// </summary>
        /// <value>The text qualifier.</value>
        public char TextQualifier
        {
            get { return _textQualifier; }
            set { _textQualifier = value; }
        }

        private char _textQualifier;

        /// <summary>
        /// Gets or sets the current line length.
        /// </summary>
        /// <value>The length of the current line.</value>
        public int CurrentLineLength
        {
            get { return _currentLineLength; }
            set { _currentLineLength = value; }
        }

        private int _currentLineLength;

        /// <summary>
        /// Gets or sets the current encoding.
        /// </summary>
        /// <value>The current encoding.</value>
        public Encoding CurrentEncoding
        {
            get { return _currentEncoding; }
            set { _currentEncoding = value; }
        }
        private Encoding _currentEncoding;

        /// <summary>
        /// Gets or sets the base reader.
        /// </summary>
        /// <value>The base reader.</value>
        public TextReader BaseReader
        {
            get { return _baseReader; }
            set { _baseReader = value; }
        }

        private TextReader _baseReader;

        #endregion

        #region Constructors

        /// <summary>
        /// Initializes a new instance of the <see cref="Zonkey.Text.CsvReader"/> class.
        /// </summary>
        /// <param name="reader">The reader.</param>
        /// <param name="delimiter">The delimiter.</param>
        /// <param name="textQualifier">The text qualifier.</param>
        public CsvReader(TextReader reader, char delimiter, char textQualifier)
        {
            _baseReader = reader;
            _delimiter = delimiter;
            _textQualifier = textQualifier;

            ReadLine = r => r.ReadLine();
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Zonkey.Text.CsvReader"/> class.
        /// </summary>
        /// <param name="reader">The reader.</param>
        /// <param name="delimiter">The delimiter.</param>
        public CsvReader(TextReader reader, char delimiter)
            : this(reader, delimiter, '"')
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Zonkey.Text.CsvReader"/> class.
        /// </summary>
        /// <param name="reader">The reader.</param>
        public CsvReader(TextReader reader)
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
        public CsvReader(Stream stream, char delimiter, char textQualifier)
        {
            _delimiter = delimiter;
            _textQualifier = textQualifier;

            var sr = new StreamReader(stream, true);
            _currentEncoding = sr.CurrentEncoding;
            _baseReader = sr;

            ReadLine = r => r.ReadLine();
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Zonkey.Text.CsvReader"/> class.
        /// </summary>
        /// <param name="stream">The stream.</param>
        /// <param name="delimiter">The delimiter.</param>
        public CsvReader(Stream stream, char delimiter)
            : this(stream, delimiter, '"')
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Zonkey.Text.CsvReader"/> class.
        /// </summary>
        /// <param name="stream">The stream.</param>
        public CsvReader(Stream stream)
            : this(stream, ',', '"')
        {
        }

        #endregion

        #region IDataReader Members

        /// <summary>
        /// Gets the number of rows changed, inserted, or deleted by execution of the SQL statement.
        /// </summary>
        /// <value></value>
        /// <returns>The number of rows changed, inserted, or deleted; 0 if no rows were affected or the statement failed; and -1 for SELECT statements.</returns>
        public int RecordsAffected
        {
            get { return -1; }
        }

        /// <summary>
        /// Gets a value indicating whether the data reader is closed.
        /// </summary>
        /// <value></value>
        /// <returns>true if the data reader is closed; otherwise, false.</returns>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1031:DoNotCatchGeneralExceptionTypes")]
        public bool IsClosed
        {
            get 
            { 
                try
                {
                    _baseReader.Peek();
                    return false;
                }
                catch
                {
                    return true;
                }
            }
        }

        /// <summary>
        /// Advances the data reader to the next result, when reading the results of batch SQL statements.
        /// </summary>
        /// <returns>
        /// true if there are more rows; otherwise, false.
        /// </returns>
        public bool NextResult()
        {
            return false;
        }

        /// <summary>
        /// Closes the <see cref="T:System.Data.IDataReader"/> Object.
        /// </summary>
        public void Close()
        {
            _baseReader.Dispose();
        }

        /// <summary>
        /// Advances the <see cref="T:System.Data.IDataReader"/> to the next record.
        /// </summary>
        /// <returns>
        /// true if there are more rows; otherwise, false.
        /// </returns>
        public bool Read()
        {
            _record.Clear();
            _currentLineLength = 0;

            do
            {
                _lineNum++;
                _sourceLine = (ReadLine != null) 
                    ? ReadLine(BaseReader) 
                    : BaseReader.ReadLine();
                
                if (_sourceLine == null)
                    return false;

            } while ( (LineFilter != null) && (! LineFilter(_lineNum, _sourceLine)) );

            _currentLineLength = _sourceLine.Trim().Length;
            if (_currentLineLength == 0) return false;

            bool InQuote = false;
            var sbItem = new StringBuilder();

            for (int n = 0; n < _sourceLine.Length; n++)
            {
                char c = _sourceLine[n];

                if (c == _textQualifier)
                {
                    int j = n + 1, k = n + 2;
                    if ((_sourceLine.Length > k) && (_sourceLine[j] == _textQualifier) && (_sourceLine[k] != _delimiter))
                    {
                        sbItem.Append(c);
                        n++;
                    }
                    else
                    {
                        if (!InQuote) sbItem = new StringBuilder();
                        InQuote = (!InQuote);
                    }
                }
                else if (c == _delimiter && (!InQuote))
                {
                    _record.Add(sbItem.ToString());
                    sbItem = new StringBuilder();
                }
                else
                {
                    sbItem.Append(c);
                }
            }
            _record.Add(sbItem.ToString());

            return true;
        }

        /// <summary>
        /// Gets or sets the line filter function.
        /// </summary>
        /// <value>The line delegate function.</value>
        public Func<int, string, bool> LineFilter { get; set; }

        public Func<TextReader, string > ReadLine { get; set; }

        /// <summary>
        /// Gets a value indicating the depth of nesting for the current row.
        /// </summary>
        /// <value></value>
        /// <returns>The level of nesting.</returns>
        public int Depth
        {
            get { return 0; }
        }

        /// <summary>
        /// Returns a <see cref="T:System.Data.DataTable"/> that describes the column metadata of the <see cref="T:System.Data.IDataReader"/>.
        /// </summary>
        /// <returns>
        /// A <see cref="T:System.Data.DataTable"/> that describes the column metadata.
        /// </returns>
        /// <exception cref="T:System.InvalidOperationException">The <see cref="T:System.Data.IDataReader"/> is closed. </exception>
        public DataTable GetSchemaTable()
        {
            return null;
        }

        /// <summary>
        /// Gets the line number.
        /// </summary>
        /// <value>The line number.</value>
        public int LineNumber
        {
            get { return _lineNum; }
        }

        /// <summary>
        /// Gets the source text from the last line read
        /// </summary>
        public string SourceLine
        {
            get { return _sourceLine;  }
        }

        private int _lineNum;

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

        #region IDataRecord Members

        /// <summary>
        /// Gets the 32-bit signed integer value of the specified field.
        /// </summary>
        /// <param name="i">The index of the field to find.</param>
        /// <returns>
        /// The 32-bit signed integer value of the specified field.
        /// </returns>
        /// <exception cref="T:System.IndexOutOfRangeException">The index passed was outside the range of 0 through <see cref="P:System.Data.IDataRecord.FieldCount"/>. </exception>
        public int GetInt32(int i)
        {
            return Int32.Parse(_record[i], INT_STYLE);
        }

        /// <summary>
        /// Gets the <see cref="System.String"/> at the specified index.
        /// </summary>
        /// <value></value>
        public string this[int index]
        {
            get { return _record[index]; }
        }

        /// <summary>
        /// Gets the <see cref="System.String"/> at the specified index.
        /// </summary>
        /// <value></value>
        public string this[int index, int maxLength]
        {
            get
            {
                return (maxLength < _record[index].Length) 
                    ? _record[index].Substring(0, maxLength) 
                    : _record[index];
            }
        }

        /// <summary>
        /// Gets the <see cref="System.Object"/> with the specified name.
        /// </summary>
        /// <value></value>
        object IDataRecord.this[string name]
        {
            get { return null; }
        }

        /// <summary>
        /// Gets the <see cref="System.Object"/> at the specified index.
        /// </summary>
        /// <value></value>
        object IDataRecord.this[int index]
        {
            get { return _record[index]; }
        }

        /// <summary>
        /// Return the value of the specified field.
        /// </summary>
        /// <param name="i">The index of the field to find.</param>
        /// <returns>
        /// The <see cref="T:System.Object"/> which will contain the field value upon return.
        /// </returns>
        /// <exception cref="T:System.IndexOutOfRangeException">The index passed was outside the range of 0 through <see cref="P:System.Data.IDataRecord.FieldCount"/>. </exception>
        public object GetValue(int i)
        {
            return _record[i];
        }

        /// <summary>
        /// Return whether the specified field is set to null.
        /// </summary>
        /// <param name="i">The index of the field to find.</param>
        /// <returns>
        /// true if the specified field is set to null; otherwise, false.
        /// </returns>
        /// <exception cref="T:System.IndexOutOfRangeException">The index passed was outside the range of 0 through <see cref="P:System.Data.IDataRecord.FieldCount"/>. </exception>
        public bool IsDBNull(int i)
        {
            return string.IsNullOrEmpty(_record[i]);
        }

        /// <summary>
        /// Reads a stream of bytes from the specified column offset into the buffer as an array, starting at the given buffer offset.
        /// </summary>
        /// <param name="i">The zero-based column ordinal.</param>
        /// <param name="fieldOffset">The index within the field from which to start the read operation.</param>
        /// <param name="buffer">The buffer into which to read the stream of bytes.</param>
        /// <param name="bufferoffset">The index for <paramref name="buffer"/> to start the read operation.</param>
        /// <param name="length">The number of bytes to read.</param>
        /// <returns>The actual number of bytes read.</returns>
        /// <exception cref="T:System.IndexOutOfRangeException">The index passed was outside the range of 0 through <see cref="P:System.Data.IDataRecord.FieldCount"/>. </exception>
        public long GetBytes(int i, long fieldOffset, byte[] buffer, int bufferoffset, int length)
        {
            if (_currentEncoding == null)
                throw new InvalidOperationException("Must set CurrentEncoding before calling GetBytes()");

            return _currentEncoding.GetBytes(_record[i], (int)fieldOffset, length, buffer, bufferoffset);
        }

        /// <summary>
        /// Gets the 8-bit unsigned integer value of the specified column.
        /// </summary>
        /// <param name="i">The zero-based column ordinal.</param>
        /// <returns>
        /// The 8-bit unsigned integer value of the specified column.
        /// </returns>
        /// <exception cref="T:System.IndexOutOfRangeException">The index passed was outside the range of 0 through <see cref="P:System.Data.IDataRecord.FieldCount"/>. </exception>
        public byte GetByte(int i)
        {
            return Byte.Parse(_record[i], NumberStyles.AllowLeadingWhite | NumberStyles.AllowTrailingWhite);
        }

        /// <summary>
        /// Gets the <see cref="T:System.Type"/> information corresponding to the type of <see cref="T:System.Object"/> that would be returned from <see cref="M:System.Data.IDataRecord.GetValue(System.Int32)"/>.
        /// </summary>
        /// <param name="i">The index of the field to find.</param>
        /// <returns>
        /// The <see cref="T:System.Type"/> information corresponding to the type of <see cref="T:System.Object"/> that would be returned from <see cref="M:System.Data.IDataRecord.GetValue(System.Int32)"/>.
        /// </returns>
        /// <exception cref="T:System.IndexOutOfRangeException">The index passed was outside the range of 0 through <see cref="P:System.Data.IDataRecord.FieldCount"/>. </exception>
        public Type GetFieldType(int i)
        {
            return typeof (string);
        }

        /// <summary>
        /// Gets the fixed-position numeric value of the specified field.
        /// </summary>
        /// <param name="i">The index of the field to find.</param>
        /// <returns>
        /// The fixed-position numeric value of the specified field.
        /// </returns>
        /// <exception cref="T:System.IndexOutOfRangeException">The index passed was outside the range of 0 through <see cref="P:System.Data.IDataRecord.FieldCount"/>. </exception>
        public decimal GetDecimal(int i)
        {
            return Decimal.Parse(_record[i], NumberStyles.Currency);
        }

        /// <summary>
        /// Gets all the attribute fields in the collection for the current record.
        /// </summary>
        /// <param name="values">An array of <see cref="T:System.Object"/> to copy the attribute fields into.</param>
        /// <returns>
        /// The number of instances of <see cref="T:System.Object"/> in the array.
        /// </returns>
        public int GetValues(object[] values)
        {
            int len = (_record.Count > values.Length) ? values.Length : _record.Count;
            var saTemp = new string[_record.Count];

            _record.CopyTo(saTemp, 0);
            Array.Copy(saTemp, 0, values, 0, len);

            return len;
        }

        /// <summary>
        /// Gets the name for the field to find.
        /// </summary>
        /// <param name="i">The index of the field to find.</param>
        /// <returns>
        /// The name of the field or the empty string (""), if there is no value to return.
        /// </returns>
        /// <exception cref="T:System.IndexOutOfRangeException">The index passed was outside the range of 0 through <see cref="P:System.Data.IDataRecord.FieldCount"/>. </exception>
        public string GetName(int i)
        {
            return null;
        }

        /// <summary>
        /// Gets the number of columns in the current row.
        /// </summary>
        /// <value></value>
        /// <returns>When not positioned in a valid recordset, 0; otherwise, the number of columns in the current record. The default is -1.</returns>
        public int FieldCount
        {
            get { return _record.Count; }
        }

        /// <summary>
        /// Gets the 64-bit signed integer value of the specified field.
        /// </summary>
        /// <param name="i">The index of the field to find.</param>
        /// <returns>
        /// The 64-bit signed integer value of the specified field.
        /// </returns>
        /// <exception cref="T:System.IndexOutOfRangeException">The index passed was outside the range of 0 through <see cref="P:System.Data.IDataRecord.FieldCount"/>. </exception>
        public long GetInt64(int i)
        {
            return Int64.Parse(_record[i], INT_STYLE);
        }

        /// <summary>
        /// Gets the double-precision floating point number of the specified field.
        /// </summary>
        /// <param name="i">The index of the field to find.</param>
        /// <returns>
        /// The double-precision floating point number of the specified field.
        /// </returns>
        /// <exception cref="T:System.IndexOutOfRangeException">The index passed was outside the range of 0 through <see cref="P:System.Data.IDataRecord.FieldCount"/>. </exception>
        public double GetDouble(int i)
        {
            return Double.Parse(_record[i], NumberStyles.Any);
        }

        /// <summary>
        /// Gets the value of the specified column as a Boolean.
        /// </summary>
        /// <param name="i">The zero-based column ordinal.</param>
        /// <returns>The value of the column.</returns>
        /// <exception cref="T:System.IndexOutOfRangeException">The index passed was outside the range of 0 through <see cref="P:System.Data.IDataRecord.FieldCount"/>. </exception>
        public bool GetBoolean(int i)
        {
            return Boolean.Parse(_record[i]);
        }

        /// <summary>
        /// Returns the GUID value of the specified field.
        /// </summary>
        /// <param name="i">The index of the field to find.</param>
        /// <returns>The GUID value of the specified field.</returns>
        /// <exception cref="T:System.IndexOutOfRangeException">The index passed was outside the range of 0 through <see cref="P:System.Data.IDataRecord.FieldCount"/>. </exception>
        public Guid GetGuid(int i)
        {
            return new Guid(_record[i]);
        }

        /// <summary>
        /// Gets the date and time data value of the specified field.
        /// </summary>
        /// <param name="i">The index of the field to find.</param>
        /// <returns>
        /// The date and time data value of the specified field.
        /// </returns>
        /// <exception cref="T:System.IndexOutOfRangeException">The index passed was outside the range of 0 through <see cref="P:System.Data.IDataRecord.FieldCount"/>. </exception>
        public DateTime GetDateTime(int i)
        {
            return DateTime.Parse(_record[i]);
        }

        /// <summary>
        /// Return the index of the named field.
        /// </summary>
        /// <param name="name">The name of the field to find.</param>
        /// <returns>The index of the named field.</returns>
        public int GetOrdinal(string name)
        {
            return -1;
        }

        /// <summary>
        /// Gets the data type information for the specified field.
        /// </summary>
        /// <param name="i">The index of the field to find.</param>
        /// <returns>
        /// The data type information for the specified field.
        /// </returns>
        /// <exception cref="T:System.IndexOutOfRangeException">The index passed was outside the range of 0 through <see cref="P:System.Data.IDataRecord.FieldCount"/>. </exception>
        public string GetDataTypeName(int i)
        {
            return "String";
        }

        /// <summary>
        /// Gets the single-precision floating point number of the specified field.
        /// </summary>
        /// <param name="i">The index of the field to find.</param>
        /// <returns>
        /// The single-precision floating point number of the specified field.
        /// </returns>
        /// <exception cref="T:System.IndexOutOfRangeException">The index passed was outside the range of 0 through <see cref="P:System.Data.IDataRecord.FieldCount"/>. </exception>
        public float GetFloat(int i)
        {
            return float.Parse(_record[i], NumberStyles.Any);
        }

        /// <summary>
        /// Returns an <see cref="T:System.Data.IDataReader"/> for the specified column ordinal.
        /// </summary>
        /// <param name="i">The index of the field to find.</param>
        /// <returns>
        /// An <see cref="T:System.Data.IDataReader"/>.
        /// </returns>
        /// <exception cref="T:System.IndexOutOfRangeException">The index passed was outside the range of 0 through <see cref="P:System.Data.IDataRecord.FieldCount"/>. </exception>
        public IDataReader GetData(int i)
        {
            return null;
        }

        /// <summary>
        /// Reads a stream of characters from the specified column offset into the buffer as an array, starting at the given buffer offset.
        /// </summary>
        /// <param name="i">The zero-based column ordinal.</param>
        /// <param name="fieldoffset">The index within the row from which to start the read operation.</param>
        /// <param name="buffer">The buffer into which to read the stream of bytes.</param>
        /// <param name="bufferoffset">The index for <paramref name="buffer"/> to start the read operation.</param>
        /// <param name="length">The number of bytes to read.</param>
        /// <returns>The actual number of characters read.</returns>
        /// <exception cref="T:System.IndexOutOfRangeException">The index passed was outside the range of 0 through <see cref="P:System.Data.IDataRecord.FieldCount"/>. </exception>
        public long GetChars(int i, long fieldoffset, char[] buffer, int bufferoffset, int length)
        {
            char[] ch = _record[i].ToCharArray();
            int len = (ch.Length > length) ? length : ch.Length;

            Array.Copy(ch, (int)fieldoffset, buffer, bufferoffset, len);
            return len;
        }

        /// <summary>
        /// Gets the string value of the specified field.
        /// </summary>
        /// <param name="i">The index of the field to find.</param>
        /// <returns>The string value of the specified field.</returns>
        /// <exception cref="T:System.IndexOutOfRangeException">The index passed was outside the range of 0 through <see cref="P:System.Data.IDataRecord.FieldCount"/>. </exception>
        public string GetString(int i)
        {
            return _record[i];
        }

        /// <summary>
        /// Gets the string.
        /// </summary>
        /// <param name="index">The index.</param>
        /// <param name="maxLength">Length of the max.</param>
        /// <returns></returns>
        public string GetString(int index, int maxLength)
        {
            return (maxLength < _record[index].Length) 
                ? _record[index].Substring(0, maxLength) 
                : _record[index];
        }

        /// <summary>
        /// Gets the character value of the specified column.
        /// </summary>
        /// <param name="i">The zero-based column ordinal.</param>
        /// <returns>
        /// The character value of the specified column.
        /// </returns>
        /// <exception cref="T:System.IndexOutOfRangeException">The index passed was outside the range of 0 through <see cref="P:System.Data.IDataRecord.FieldCount"/>. </exception>
        public char GetChar(int i)
        {
            return _record[i][0];
        }

        /// <summary>
        /// Gets the 16-bit signed integer value of the specified field.
        /// </summary>
        /// <param name="i">The index of the field to find.</param>
        /// <returns>
        /// The 16-bit signed integer value of the specified field.
        /// </returns>
        /// <exception cref="T:System.IndexOutOfRangeException">The index passed was outside the range of 0 through <see cref="P:System.Data.IDataRecord.FieldCount"/>. </exception>
        public short GetInt16(int i)
        {
            return Int16.Parse(_record[i], INT_STYLE);
        }

        #endregion
    }
}