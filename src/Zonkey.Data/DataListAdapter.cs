using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Zonkey
{
    /// <summary>
    /// Provides methods for the interaction of a DataList (array) with a database.
    /// </summary>
    public class DataListAdapter : AdapterBase
    {
        /// <summary>
        /// Preferred constructor
        /// </summary>
        /// <param name="connection">Database Connection to be used by DataListAdapter</param>
        public DataListAdapter(DbConnection connection)
        {
            Connection = connection;
        }

        /// <summary>
        /// Default Constructor
        /// </summary>
        public DataListAdapter()
        {
        }

        /// <summary>
        /// Gets an Array of T from an SQL statement
        /// Always uses column 0 only
        /// </summary>
        /// <param name="sql">SQL SELECT Statement</param>
        /// <returns>An Array of T</returns>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1004:GenericMethodsShouldProvideTypeParameter")]
        public Task<T[]> GetArray<T>(string sql)
        {
            return GetArray<T>(sql, false, null);
        }

        /// <summary>
        /// Gets an Array of T from an SQL statement
        /// Always uses column 0 only
        /// </summary>
        /// <param name="sql">SQL SELECT Statement</param>
        /// <param name="parameters">Array of Parameters</param>
        /// <returns>An Array of T</returns>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1004:GenericMethodsShouldProvideTypeParameter")]
        public Task<T[]> GetArray<T>(string sql, params object[] parameters)
        {
            return GetArray<T>(sql, false, parameters);
        }

        /// <summary>
        /// Gets an Array of T from an SQL statement
        /// Always uses column 0 only
        /// </summary>
        /// <param name="sql">SQL SELECT Statement</param>
        /// <param name="isProc">Is Stored Procedure</param>
        /// <param name="parameters">Array of Parameters</param>
        /// <returns>An Array of T</returns>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1004:GenericMethodsShouldProvideTypeParameter")]
        public async Task<T[]> GetArray<T>(string sql, bool isProc, params object[] parameters)
        {
            DbCommand command = PrepareCommand(sql, isProc, parameters);

            var list = new List<T>();
            TypeInfo typeInfo = typeof(T).GetTypeInfo();
            using (DbDataReader reader = await command.ExecuteReaderAsync(CommandBehavior.SingleResult).ConfigureAwait(false))
            {
                while (await reader.ReadAsync())
                {
                    if (typeInfo.IsAssignableFrom(reader[0].GetType()))
                        list.Add((T)reader[0]);
                    else
                        list.Add((T)Convert.ChangeType(reader[0], typeof (T)));
                }
            }

            return list.ToArray();
        }

        /// <summary>
        /// Gets an Array of strings from an SQL statement
        /// Always uses column 0 only
        /// </summary>
        /// <param name="sql">SQL SELECT Statement</param>
        /// <param name="isProc">Is Stored Procedure</param>
        /// <param name="prependEmpty">should the first item be an empty string?</param>
        /// <param name="parameters">Array of Parameters</param>
        /// <returns>An Array of strings</returns>
        public async Task<string[]> GetStringArray(string sql, bool isProc, bool prependEmpty, params object[] parameters)
        {
            var command = PrepareCommand(sql, isProc, parameters);

            var list = new List<string>();
            if (prependEmpty) list.Add(string.Empty);
            using (var reader = await command.ExecuteReaderAsync(CommandBehavior.SingleResult).ConfigureAwait(false))
            {
                while (await reader.ReadAsync())
                {
                    string sValue = reader[0].ToString();
                    bool isEmpty = string.IsNullOrEmpty(sValue);

                    if (! (isEmpty && prependEmpty))
                        list.Add(sValue);
                }
            }

            return list.ToArray();
        }


        /// <summary>
        /// Gets a delimited list of string values from an SQL statement
        /// Always uses column 0 only
        /// </summary>
        /// <param name="sql">SQL SELECT Statement</param>
        /// <param name="delimiter">The delimiter for the string</param>
        /// <param name="isProc">Is Stored Procedure</param>
        /// <param name="parameters">Array of Parameters</param>
        /// <returns>a string</returns>
        public Task<string> GetDelimitedString(string sql, string delimiter, bool isProc, params object[] parameters)
        {
            return GetDelimitedString(sql, delimiter, null, isProc, parameters);
        }

        /// <summary>
        /// Gets a delimited list of string values from an SQL statement
        /// Always uses column 0 only
        /// </summary>
        /// <param name="sql">SQL SELECT Statement</param>
        /// <param name="delimiter">The delimiter for the string</param>
        /// <param name="textQualifier">A string to put before and after each item in the list (often a single quote)</param>
        /// <param name="isProc">Is Stored Procedure</param>
        /// <param name="parameters">Array of Parameters</param>
        /// <returns>a string</returns>
        public async Task<string> GetDelimitedString(string sql, string delimiter, string textQualifier, bool isProc, params object[] parameters)
        {
            if (delimiter == null) throw new ArgumentNullException(nameof(delimiter));
            bool isQualified = (! string.IsNullOrEmpty(textQualifier));

            var command = PrepareCommand(sql, isProc, parameters);

            var sbList = new StringBuilder();
            using (var reader = await command.ExecuteReaderAsync(CommandBehavior.SingleResult).ConfigureAwait(false))
            {
                while (await reader.ReadAsync())
                {
                    if (sbList.Length > 0)
                        sbList.Append(delimiter);

                    if (isQualified)
                    {
                        sbList.Append(textQualifier);
                        sbList.Append(reader[0].ToString());
                        sbList.Append(textQualifier);
                    }
                    else
                        sbList.Append(textQualifier);
                }
            }

            return sbList.ToString();
        }

        /// <summary>
        /// Gets an Array of DataListItems from an SQL statement
        /// Column 0 is 'ID', Column 1 is 'Label'
        /// </summary>
        /// <param name="sql">SQL SELECT Statement</param>
        /// <returns>A <see cref="Zonkey.DataListItem"/> array.</returns>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2223:MembersShouldDifferByMoreThanReturnType"), System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2223:MembersShouldDifferByMoreThanReturnType"), System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2223:MembersShouldDifferByMoreThanReturnType"), System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2223:MembersShouldDifferByMoreThanReturnType"), System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2223:MembersShouldDifferByMoreThanReturnType")]
        public Task<DataListItem[]> GetDataList(string sql)
        {
            return GetDataList(sql, false, null);
        }

        /// <summary>
        /// Gets an Array of DataListItems from an SQL statement
        /// Column 0 is 'ID', Column 1 is 'Label'
        /// </summary>
        /// <param name="sql">SQL SELECT Statement</param>
        /// <param name="parameters">Array of Parameters</param>
        /// <returns>A <see cref="Zonkey.DataListItem"/> array.</returns>
        public Task<DataListItem[]> GetDataList(string sql, params object[] parameters)
        {
            return GetDataList(sql, false, parameters);
        }

        /// <summary>
        /// Gets an Array of DataListItems from an SQL statement
        /// Column 0 is 'ID', Column 1 is 'Label'
        /// </summary>
        /// <param name="sql">SQL SELECT Statement</param>
        /// <param name="isProc">Is Stored Procedure</param>
        /// <param name="parameters">Array of Parameters</param>
        /// <returns>A <see cref="Zonkey.DataListItem"/> array.</returns>
        public async Task<DataListItem[]> GetDataList(string sql, bool isProc, params object[] parameters)
        {
            var command = PrepareCommand(sql, isProc, parameters);
            var list = new List<DataListItem>();

            using (var reader = await command.ExecuteReaderAsync(CommandBehavior.SingleResult).ConfigureAwait(false))
            {
                while (await reader.ReadAsync())
                    list.Add(new DataListItem(reader[0], reader.GetString(1)));
            }

            return list.ToArray();
        }

        /// <summary>
        /// Gets an Array of DataListItems from an SQL statement
        /// Column 0 is 'ID', Column 1 is 'Label'
        /// </summary>
        /// <typeparam name="TKey">The type of the ID (select column 0)</typeparam>
        /// <param name="sql">SQL SELECT Statement</param>
        /// <returns>A <see cref="Zonkey.DataListItem&lt;TKey&gt;"/> array.</returns>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1715:IdentifiersShouldHaveCorrectPrefix", MessageId = "T")]
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1004:GenericMethodsShouldProvideTypeParameter")]
        public Task<DataListItem<TKey>[]> GetDataList<TKey>(string sql)
        {
            return GetDataList<TKey>(sql, false, null);
        }

        /// <summary>
        /// Gets an Array of DataListItems from an SQL statement
        /// Column 0 is 'ID', Column 1 is 'Label'
        /// </summary>
        /// <typeparam name="TKey">The type of the ID (select column 0)</typeparam>
        /// <param name="sql">SQL SELECT Statement</param>
        /// <param name="parameters">Array of Parameters</param>
        /// <returns>>A <see cref="Zonkey.DataListItem&lt;TKey&gt;"/> array.</returns>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1715:IdentifiersShouldHaveCorrectPrefix", MessageId = "T")]
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", MessageId = "TKey")]
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1004:GenericMethodsShouldProvideTypeParameter")]
        public Task<DataListItem<TKey>[]> GetDataList<TKey>(string sql, params object[] parameters)
        {
            return GetDataList<TKey>(sql, false, parameters);
        }

        /// <summary>
        /// Gets an Array of DataListItems from an SQL statement
        /// Column 0 is 'ID', Column 1 is 'Label'
        /// </summary>
        /// <typeparam name="TKey">The type of the ID (select column 0)</typeparam>
        /// <param name="sql">SQL SELECT Statement</param>
        /// <param name="isProc">Is Stored Procedure</param>
        /// <param name="parameters">Array of Parameters</param>
        /// <returns>A <see cref="Zonkey.DataListItem&lt;TKey&gt;"/> array.</returns>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1715:IdentifiersShouldHaveCorrectPrefix", MessageId = "T")]
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", MessageId = "TKey")]
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1004:GenericMethodsShouldProvideTypeParameter")]
        public async Task<DataListItem<TKey>[]> GetDataList<TKey>(string sql, bool isProc, params object[] parameters)
        {
            var command = PrepareCommand(sql, isProc, parameters);
            var list = new List<DataListItem<TKey>>();

            using (var reader = await command.ExecuteReaderAsync(CommandBehavior.SingleResult).ConfigureAwait(false))
            {
                while (await reader.ReadAsync())
                    list.Add(new DataListItem<TKey>((TKey)reader[0], reader.GetString(1)));
            }

            return list.ToArray();
        }

        /// <summary>
        /// Fills list of IDataListItems from an SQL statement
        /// Column 0 is 'Key, Column 1 is 'Value'
        /// </summary>
        /// <typeparam name="T">The type of object contained in the list</typeparam>
        /// <param name="list">The List object to fill</param>
        /// <param name="sql">SQL SELECT Statement</param>
        public Task FillDataList<T>(ICollection<T> list, string sql)
            where T : IDataListItem, new()
        {
            return FillDataList(list, sql, false, null);
        }

        /// <summary>
        /// Fills list of IDataListItems from an SQL statement
        /// Column 0 is 'Key, Column 1 is 'Value'
        /// </summary>
        /// <typeparam name="T">The type of object contained in the list</typeparam>
        /// <param name="list">The List object to fill</param>
        /// <param name="sql">SQL SELECT Statement</param>
        /// <param name="parameters">Array of Parameters</param>
        public Task FillDataList<T>(ICollection<T> list, string sql, params object[] parameters)
            where T : IDataListItem, new()
        {
            return FillDataList(list, sql, false, parameters);
        }

        /// <summary>
        /// Fills list of IDataListItems from an SQL statement
        /// Column 0 is 'Key, Column 1 is 'Value'
        /// </summary>
        /// <typeparam name="T">The type of object contained in the list</typeparam>
        /// <param name="list">The List object to fill</param>
        /// <param name="sql">SQL SELECT Statement</param>
        /// <param name="isProc">Is Stored Procedure</param>
        /// <param name="parameters">Array of Parameters</param>
        public async Task FillDataList<T>(ICollection<T> list, string sql, bool isProc, params object[] parameters)
            where T : IDataListItem, new()
        {
            if (list == null) throw new ArgumentNullException(nameof(list));

            var command = PrepareCommand(sql, isProc, parameters);
            using (var reader = await command.ExecuteReaderAsync(CommandBehavior.SingleResult).ConfigureAwait(false))
            {
                while (await reader.ReadAsync())
                    list.Add( new T { Id = reader[0], Label = reader[1].ToString() } );
            }
        }

        /// <summary>
        /// Fills a collection of T from an SQL statement
        /// Always uses column 0 only
        /// </summary>
        /// <typeparam name="T">The type of object contained in the list</typeparam>
        /// <param name="list">The List object to fill</param>
        /// <param name="sql">SQL SELECT Statement</param>
        public Task FillCollection<T>(ICollection<T> list, string sql)
        {
            return FillCollection(list, sql, false, null);
        }

        /// <summary>
        /// Fills a collection of T from an SQL statement
        /// Always uses column 0 only
        /// </summary>
        /// <typeparam name="T">The type of object contained in the list</typeparam>
        /// <param name="list">The List object to fill</param>
        /// <param name="sql">SQL SELECT Statement</param>
        /// <param name="parameters">Array of Parameters</param>
        public Task FillCollection<T>(ICollection<T> list, string sql, params object[] parameters)
        {
            return FillCollection(list, sql, false, parameters);
        }

        /// <summary>
        /// Fills a collection of T from an SQL statement
        /// Always uses column 0 only
        /// </summary>
        /// <typeparam name="T">The type of object contained in the list</typeparam>
        /// <param name="list">The List object to fill</param>
        /// <param name="sql">SQL SELECT Statement</param>
        /// <param name="isProc">Is Stored Procedure</param>
        /// <param name="parameters">Array of Parameters</param>
        public async Task FillCollection<T>(ICollection<T> list, string sql, bool isProc, params object[] parameters)
        {
            if (list == null) throw new ArgumentNullException(nameof(list));

            TypeInfo typeInfo = typeof(T).GetTypeInfo();
            var command = PrepareCommand(sql, isProc, parameters);
            using (var reader = await command.ExecuteReaderAsync(CommandBehavior.SingleResult).ConfigureAwait(false))
            {
                while (await reader.ReadAsync())
                {
                    if (typeInfo.IsAssignableFrom(reader[0].GetType()))
                        list.Add((T)reader[0]);
                    else
                        list.Add((T)Convert.ChangeType(reader[0], typeof (T)));
                }
            }
        }

        /// <summary>
        /// Fills a dictionary from an SQL statement
        /// Column 0 is 'Key, Column 1 is 'Value'
        /// </summary>
        /// <param name="dictionary">The IDictionary object to fill</param>
        /// <param name="sql">SQL SELECT Statement</param>
        public Task FillDictionary(IDictionary dictionary, string sql)
        {
            return FillDictionary(dictionary, sql, false, null);
        }

        /// <summary>
        /// Fills a dictionary from an SQL statement
        /// Column 0 is 'Key, Column 1 is 'Value'
        /// </summary>
        /// <param name="dictionary">The IDictionary object to fill</param>
        /// <param name="sql">SQL SELECT Statement</param>
        /// <param name="parameters">Array of Parameters</param>
        public Task FillDictionary(IDictionary dictionary, string sql, params object[] parameters)
        {
            return FillDictionary(dictionary, sql, false, parameters);
        }

        /// <summary>
        /// Fills a dictionary from an SQL statement
        /// Column 0 is 'Key, Column 1 is 'Value'
        /// </summary>
        /// <param name="dictionary">The IDictionary object to fill</param>
        /// <param name="sql">SQL SELECT Statement</param>
        /// <param name="isProc">Is Stored Procedure</param>
        /// <param name="parameters">Array of Parameters</param>
        public async Task FillDictionary(IDictionary dictionary, string sql, bool isProc, params object[] parameters)
        {
            if (dictionary == null) throw new ArgumentNullException(nameof(dictionary));

            var command = PrepareCommand(sql, isProc, parameters);
            using (var reader = await command.ExecuteReaderAsync(CommandBehavior.SingleResult).ConfigureAwait(false))
            {
                while (await reader.ReadAsync())
                    dictionary.Add(reader[0], reader[1]);
            }
        }

        /// <summary>
        /// Preps a new Db command for a GetListFromSql method
        /// </summary>
        /// <param name="sql">SQL SELECT Statement</param>
        /// <param name="isProc">Is Stored Procedure</param>
        /// <param name="parameters">Array of Parameters</param>
        /// <returns>An instance of a DbCommand</returns>
        private DbCommand PrepareCommand(string sql, bool isProc, object[] parameters)
        {
            if (string.IsNullOrWhiteSpace(sql))
                throw new ArgumentNullException(nameof(sql));

            if (Connection == null)
                throw new InvalidOperationException("must set connection before calling");

            var command = Connection.CreateCommand();
            command.CommandText = sql;
            command.CommandType = (isProc) ? CommandType.StoredProcedure : CommandType.Text;

            if (parameters != null)
            {
                if ( (parameters is SqlFilter[] filters) && (! isProc) && (! sql.ToLower().Contains(" where ")) )
                {
                    var sbFilter = new StringBuilder(" WHERE ");
                    for (var i = 0; i < filters.Length; i++)
                    {
                        if (i > 0) sbFilter.Append(" AND ");
                        sbFilter.Append(filters[i].ToString(SqlDialect, i));
                        filters[i].AddToCommandParams(command, SqlDialect, i);
                    }

                    command.CommandText += sbFilter.ToString();
                }
                else
                    DataManager.AddParamsToCommand(command, SqlDialect, parameters, ParameterPrefix);
            }

            return command;
        }
    }
}