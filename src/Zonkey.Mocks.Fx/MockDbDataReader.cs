using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Reflection;

namespace Zonkey.Mocks
{
    public class MockDbDataReader : DbDataReader
    {
        private IEnumerator _testData;

        private IDictionary<string, object> _current;
        private string[] _currentNames;
        private object[] _currentValues;

        private readonly DataColumnCollection _columns;

        private bool _firstRead;

        /// <summary>
        /// Initializes a new instance of the <see cref="MockDbDataReader"/> class.
        /// </summary>
        /// <param name="testDataSource">The test data.</param>
        internal MockDbDataReader(object testDataSource)
        {
            var testTable = testDataSource as DataTable;
            if (testTable != null)
            {
                _columns = testTable.Columns;
                _testData = testTable.Rows.GetEnumerator();
            }
            else if (testDataSource is IEnumerable)
            {
                _testData = ((IEnumerable) testDataSource).GetEnumerator();

                ReadInternal(true);
                _firstRead = true;
            }
            else
                throw new ArgumentException("Test Data must be a DataTable or IEnumerable", nameof(testDataSource));
        }

        /// <summary>
        /// Closes the <see cref="T:System.Data.Common.DbDataReader"/> object.
        /// </summary>
        public override void Close()
        {
            _testData = null;
        }

        /// <summary>
        /// Gets a value indicating the depth of nesting for the current row.
        /// </summary>
        /// <value></value>
        /// <returns>The depth of nesting for the current row.</returns>
        public override int Depth
        {
            get { return 0; }
        }

        /// <summary>
        /// Gets the number of columns in the current row.
        /// </summary>
        /// <value></value>
        /// <returns>The number of columns in the current row.</returns>
        /// <exception cref="T:System.NotSupportedException">There is no current connection to an instance of SQL Server. </exception>
        public override int FieldCount
        {
            get
            {
                return (_columns != null) 
                    ? _columns.Count 
                    : _currentNames.Length;
            }
        }

        /// <summary>
        /// Gets the value of the specified column as a Boolean.
        /// </summary>
        /// <param name="ordinal">The zero-based column ordinal.</param>
        /// <returns>The value of the specified column.</returns>
        /// <exception cref="T:System.InvalidCastException">The specified cast is not valid. </exception>
        public override bool GetBoolean(int ordinal)
        {
            CheckCurrent();
            return (Boolean)_currentValues[ordinal];
        }

        /// <summary>
        /// Gets the value of the specified column as a byte.
        /// </summary>
        /// <param name="ordinal">The zero-based column ordinal.</param>
        /// <returns>The value of the specified column.</returns>
        /// <exception cref="T:System.InvalidCastException">The specified cast is not valid. </exception>
        public override byte GetByte(int ordinal)
        {
            CheckCurrent();
            return (Byte)_currentValues[ordinal];
        }

        /// <summary>
        /// Reads a stream of bytes from the specified column, starting at location indicated by <paramref name="dataOffset"/>, into the buffer, starting at the location indicated by <paramref name="bufferOffset"/>.
        /// </summary>
        /// <param name="ordinal">The zero-based column ordinal.</param>
        /// <param name="dataOffset">The index within the row from which to begin the read operation.</param>
        /// <param name="buffer">The buffer into which to copy the data.</param>
        /// <param name="bufferOffset">The index with the buffer to which the data will be copied.</param>
        /// <param name="length">The maximum number of characters to read.</param>
        /// <returns>The actual number of bytes read.</returns>
        /// <exception cref="T:System.InvalidCastException">The specified cast is not valid. </exception>
        public override long GetBytes(int ordinal, long dataOffset, byte[] buffer, int bufferOffset, int length)
        {
            CheckCurrent();
            var bytes = _currentValues[ordinal] as byte[];

            if (bytes == null)
                throw new ArgumentException("Specified ordinal is not a byte array");

            Array.Copy(bytes, (int)dataOffset, buffer, bufferOffset, length);
            return (bytes.Length > length) ? length : bytes.Length;
        }

        /// <summary>
        /// Gets the value of the specified column as a single character.
        /// </summary>
        /// <param name="ordinal">The zero-based column ordinal.</param>
        /// <returns>The value of the specified column.</returns>
        /// <exception cref="T:System.InvalidCastException">The specified cast is not valid. </exception>
        public override char GetChar(int ordinal)
        {
            CheckCurrent();
            return (Char)_currentValues[ordinal];
        }

        /// <summary>
        /// Reads a stream of characters from the specified column, starting at location indicated by <paramref name="dataOffset"/>, into the buffer, starting at the location indicated by <paramref name="bufferOffset"/>.
        /// </summary>
        /// <param name="ordinal">The zero-based column ordinal.</param>
        /// <param name="dataOffset">The index within the row from which to begin the read operation.</param>
        /// <param name="buffer">The buffer into which to copy the data.</param>
        /// <param name="bufferOffset">The index with the buffer to which the data will be copied.</param>
        /// <param name="length">The maximum number of characters to read.</param>
        /// <returns>The actual number of characters read.</returns>
        public override long GetChars(int ordinal, long dataOffset, char[] buffer, int bufferOffset, int length)
        {
            CheckCurrent();
            var chars = _currentValues[ordinal] as char[];

            if (chars == null)
                throw new ArgumentException("Specified ordinal is not a char array");

            Array.Copy(chars, (int)dataOffset, buffer, bufferOffset, length);
            return (chars.Length > length) ? length : chars.Length;
        }

        /// <summary>
        /// Gets name of the data type of the specified column.
        /// </summary>
        /// <param name="ordinal">The zero-based column ordinal.</param>
        /// <returns>
        /// A string representing the name of the data type.
        /// </returns>
        /// <exception cref="T:System.InvalidCastException">The specified cast is not valid. </exception>
        public override string GetDataTypeName(int ordinal)
        {
            return GetFieldType(ordinal).Name;
        }

        /// <summary>
        /// Gets the value of the specified column as a <see cref="T:System.DateTime"/> object.
        /// </summary>
        /// <param name="ordinal">The zero-based column ordinal.</param>
        /// <returns>The value of the specified column.</returns>
        /// <exception cref="T:System.InvalidCastException">The specified cast is not valid. </exception>
        public override DateTime GetDateTime(int ordinal)
        {
            CheckCurrent();
            return (DateTime)_currentValues[ordinal];
        }

        /// <summary>
        /// Gets the value of the specified column as a <see cref="T:System.Decimal"/> object.
        /// </summary>
        /// <param name="ordinal">The zero-based column ordinal.</param>
        /// <returns>The value of the specified column.</returns>
        /// <exception cref="T:System.InvalidCastException">The specified cast is not valid. </exception>
        public override decimal GetDecimal(int ordinal)
        {
            CheckCurrent();
            return (Decimal)_currentValues[ordinal];
        }

        /// <summary>
        /// Gets the value of the specified column as a double-precision floating point number.
        /// </summary>
        /// <param name="ordinal">The zero-based column ordinal.</param>
        /// <returns>The value of the specified column.</returns>
        /// <exception cref="T:System.InvalidCastException">The specified cast is not valid. </exception>
        public override double GetDouble(int ordinal)
        {
            CheckCurrent();
            return (Double)_currentValues[ordinal];
        }

        /// <summary>
        /// Returns an <see cref="T:System.Collections.IEnumerator"/> that can be used to iterate through the rows in the data reader.
        /// </summary>
        /// <returns>
        /// An <see cref="T:System.Collections.IEnumerator"/> that can be used to iterate through the rows in the data reader.
        /// </returns>
        public override IEnumerator GetEnumerator()
        {
            while (Read())
                yield return this;
        }

        /// <summary>
        /// Gets the data type of the specified column.
        /// </summary>
        /// <param name="ordinal">The zero-based column ordinal.</param>
        /// <returns>The data type of the specified column.</returns>
        /// <exception cref="T:System.InvalidCastException">The specified cast is not valid. </exception>
        public override Type GetFieldType(int ordinal)
        {
            return (_columns != null) 
                ? _columns[ordinal].DataType 
                : _currentValues[ordinal].GetType();
        }

        /// <summary>
        /// Gets the value of the specified column as a single-precision floating point number.
        /// </summary>
        /// <param name="ordinal">The zero-based column ordinal.</param>
        /// <returns>The value of the specified column.</returns>
        /// <exception cref="T:System.InvalidCastException">The specified cast is not valid. </exception>
        public override float GetFloat(int ordinal)
        {
            CheckCurrent();
            return (Single)_currentValues[ordinal];
        }

        /// <summary>
        /// Gets the value of the specified column as a globally-unique identifier (GUID).
        /// </summary>
        /// <param name="ordinal">The zero-based column ordinal.</param>
        /// <returns>The value of the specified column.</returns>
        /// <exception cref="T:System.InvalidCastException">The specified cast is not valid. </exception>
        public override Guid GetGuid(int ordinal)
        {
            CheckCurrent();
            return (Guid)_currentValues[ordinal]; 
        }

        /// <summary>
        /// Gets the value of the specified column as a 16-bit signed integer.
        /// </summary>
        /// <param name="ordinal">The zero-based column ordinal.</param>
        /// <returns>The value of the specified column.</returns>
        /// <exception cref="T:System.InvalidCastException">The specified cast is not valid. </exception>
        public override short GetInt16(int ordinal)
        {
            CheckCurrent();
            return (Int16)_currentValues[ordinal];
        }

        /// <summary>
        /// Gets the value of the specified column as a 32-bit signed integer.
        /// </summary>
        /// <param name="ordinal">The zero-based column ordinal.</param>
        /// <returns>The value of the specified column.</returns>
        /// <exception cref="T:System.InvalidCastException">The specified cast is not valid. </exception>
        public override int GetInt32(int ordinal)
        {
            CheckCurrent();
            return (Int32)_currentValues[ordinal];
        }

        /// <summary>
        /// Gets the value of the specified column as a 64-bit signed integer.
        /// </summary>
        /// <param name="ordinal">The zero-based column ordinal.</param>
        /// <returns>The value of the specified column.</returns>
        /// <exception cref="T:System.InvalidCastException">The specified cast is not valid. </exception>
        public override long GetInt64(int ordinal)
        {
            CheckCurrent();
            return (Int64)_currentValues[ordinal];
        }

        /// <summary>
        /// Gets the name of the column, given the zero-based column ordinal.
        /// </summary>
        /// <param name="ordinal">The zero-based column ordinal.</param>
        /// <returns>The name of the specified column.</returns>
        public override string GetName(int ordinal)
        {
            return (_columns != null)
                ? _columns[ordinal].ColumnName
                : _currentNames[ordinal];
        }

        /// <summary>
        /// Gets the column ordinal given the name of the column.
        /// </summary>
        /// <param name="name">The name of the column.</param>
        /// <returns>The zero-based column ordinal.</returns>
        /// <exception cref="T:System.IndexOutOfRangeException">The name specified is not a valid column name.</exception>
        public override int GetOrdinal(string name)
        {
            if (_columns != null)
            {
                int index = _columns.IndexOf(name);
                if (index >= 0) return index;
            }
            else
            {
                for (int i = 0; i < _currentNames.Length; i++)
                {
                    if (_currentNames[i] == name)
                        return i;
                }                
            }

            throw new IndexOutOfRangeException("The name specified is not a valid column name.");
        }

        /// <summary>
        /// Returns a <see cref="T:System.Data.DataTable"/> that describes the column metadata of the <see cref="T:System.Data.Common.DbDataReader"/>.
        /// </summary>
        /// <exception cref="NotImplementedException"></exception>
        /// <returns>
        /// A <see cref="T:System.Data.DataTable"/> that describes the column metadata.
        /// </returns>
        /// <exception cref="T:System.InvalidOperationException">The <see cref="T:System.Data.SqlClient.SqlDataReader"/> is closed. </exception>
        public override DataTable GetSchemaTable()
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Gets the value of the specified column as an instance of <see cref="T:System.String"/>.
        /// </summary>
        /// <param name="ordinal">The zero-based column ordinal.</param>
        /// <returns>The value of the specified column.</returns>
        /// <exception cref="T:System.InvalidCastException">The specified cast is not valid. </exception>
        public override string GetString(int ordinal)
        {
            CheckCurrent();
            return (String)_currentValues[ordinal];
        }

        /// <summary>
        /// Gets the value of the specified column as an instance of <see cref="T:System.Object"/>.
        /// </summary>
        /// <param name="ordinal">The zero-based column ordinal.</param>
        /// <returns>The value of the specified column.</returns>
        public override object GetValue(int ordinal)
        {
            CheckCurrent();
            return _currentValues[ordinal];
        }

        /// <summary>
        /// Populates an array of objects with the column values of the current row.
        /// </summary>
        /// <param name="values">An array of <see cref="T:System.Object"/> into which to copy the attribute columns.</param>
        /// <returns>
        /// The number of instances of <see cref="T:System.Object"/> in the array.
        /// </returns>
        public override int GetValues(object[] values)
        {
            CheckCurrent();
            _currentValues.CopyTo(values, 0);
            return _currentValues.Length;
        }

        /// <summary>
        /// Gets a value that indicates whether this <see cref="T:System.Data.Common.DbDataReader"/> contains one or more rows.
        /// </summary>
        /// <value></value>
        /// <returns>true if the <see cref="T:System.Data.Common.DbDataReader"/> contains one or more rows; otherwise false.</returns>
        public override bool HasRows
        {
            get { return (! IsClosed); }
        }

        /// <summary>
        /// Gets a value indicating whether the <see cref="T:System.Data.Common.DbDataReader"/> is closed.
        /// </summary>
        /// <value></value>
        /// <returns>true if the <see cref="T:System.Data.Common.DbDataReader"/> is closed; otherwise false.</returns>
        /// <exception cref="T:System.InvalidOperationException">The <see cref="T:System.Data.SqlClient.SqlDataReader"/> is closed. </exception>
        public override bool IsClosed
        {
            get { return (_testData == null); }
        }

        /// <summary>
        /// Gets a value that indicates whether the column contains nonexistent or missing values.
        /// </summary>
        /// <param name="ordinal">The zero-based column ordinal.</param>
        /// <returns>
        /// true if the specified column is equivalent to <see cref="T:System.DBNull"/>; otherwise false.
        /// </returns>
        public override bool IsDBNull(int ordinal)
        {
            CheckCurrent();
            return Convert.IsDBNull(_currentValues[ordinal]);
        }

        /// <summary>
        /// Advances the reader to the next result when reading the results of a batch of statements.
        /// </summary>
        /// <returns>
        /// true if there are more result sets; otherwise false.
        /// </returns>
        public override bool NextResult()
        {
            return false;
        }

        /// <summary>
        /// Advances the reader to the next record in a result set.
        /// </summary>
        /// <returns>
        /// true if there are more rows; otherwise false.
        /// </returns>
        public override bool Read()
        {
            return ReadInternal(false);
        }

        private bool ReadInternal(bool schemaOnly)
        {
            if (_firstRead)
                _firstRead = false;
            else if (!_testData.MoveNext())
                return false;
            
            if (_testData.Current == null) 
                return false;

            object oTemp = _testData.Current;
            if (oTemp is IDictionary<string, object>)
                _current = (IDictionary<string, object>)oTemp;
            else
            {
                _current = new Dictionary<string, object>();

                if (oTemp is IDictionary)
                {
                    foreach (DictionaryEntry entry in (IDictionary)oTemp)
                        _current.Add(entry.Key.ToString(), entry.Value);
                }
                else if (oTemp is DataRow)
                {
                    var row = (DataRow) oTemp;
                    foreach (DataColumn column in row.Table.Columns)
                        _current.Add(column.ColumnName, row[column.Ordinal]);
                }
                else
                {                    
                    foreach (PropertyInfo pi in oTemp.GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance))
                        _current.Add(pi.Name, pi.GetValue(oTemp, null));
                }
            }

            _currentNames = new string[_current.Keys.Count];
            _current.Keys.CopyTo(_currentNames, 0);

            _currentValues = new object[_currentNames.Length];
            _current.Values.CopyTo(_currentValues, 0);

            if (schemaOnly)
                _current = null;

            return true;
        }

        /// <summary>
        /// Gets the number of rows changed, inserted, or deleted by execution of the SQL statement.
        /// </summary>
        /// <value></value>
        /// <returns>The number of rows changed, inserted, or deleted. -1 for SELECT statements; 0 if no rows were affected or the statement failed.</returns>
        public override int RecordsAffected
        {
            get { return -1; }
        }

        /// <summary>
        /// Gets the <see cref="System.Object"/> with the specified name.
        /// </summary>
        /// <value></value>
        public override object this[string name]
        {
            get
            {
                CheckCurrent();
                return _current[name];
            }
        }

        /// <summary>
        /// Gets the <see cref="System.Object"/> with the specified ordinal.
        /// </summary>
        /// <value></value>
        public override object this[int ordinal]
        {
            get
            {
                CheckCurrent();
                return _currentValues[ordinal];
            }
        }

        private void CheckCurrent()
        {
            if (_current == null)
                throw new InvalidOperationException("Must Read() before attempting to get values");
        }
    }
}
