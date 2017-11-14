using System;
using System.Data;
using System.Data.Common;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Zonkey.Utility
{
    /// <summary>
    /// Provides methods to read and execute SQL script files.
    /// </summary>
    public class SqlScriptProcessor
    {
        private readonly DbConnection m_cnxn;
        private bool m_UseTrxn;
        private string[] m_SqlArray;

        /// <summary>
        /// Initializes a new instance of the <see cref="Zonkey.Utility.SqlScriptProcessor"/> class.
        /// </summary>
        /// <param name="connection">The connection.</param>
        public SqlScriptProcessor(DbConnection connection)
        {
            m_cnxn = connection;
        }

        /// <summary>
        /// Executes the SQL script.
        /// </summary>
        /// <param name="sqlFile">The path to the SQL script file.</param>
        /// <param name="failOnError">if set to <c>true</c>, then throws an error if execution fails.</param>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1031:DoNotCatchGeneralExceptionTypes")]
        public async Task ExecuteScript(string sqlFile, bool failOnError)
        {
            if (string.IsNullOrEmpty(sqlFile))
                throw new ArgumentNullException(nameof(sqlFile));

            // set parm values
            m_UseTrxn = true;

            // open sql file or console
            string sSql;

            if (sqlFile.Equals("con", StringComparison.CurrentCultureIgnoreCase))
            {
                using (StreamReader sr = new StreamReader(Console.OpenStandardInput(), Encoding.ASCII))
                    sSql = sr.ReadToEnd();
            }
            else
            {
                sSql = File.ReadAllText(sqlFile);
            }

            // Split sql statements at 'GO'
            m_SqlArray = Regex.Split(sSql, @"\r\nGO(?:\r\n)*", RegexOptions.IgnoreCase);

            try
            {
                // get trx
                DbTransaction trx = null;
                if (m_UseTrxn) trx = m_cnxn.BeginTransaction();

                for (int i = 0; i < m_SqlArray.Length; i++)
                {
                    string q = m_SqlArray[i].Trim();
                    if (q.Length == 0) continue;

                    try
                    {
                        DbCommand cmd = m_cnxn.CreateCommand();
                        cmd.CommandText = q;
                        cmd.Connection = m_cnxn;
                        cmd.Transaction = trx;
                        cmd.CommandTimeout = 120;
                        await cmd.ExecuteNonQueryAsync();
                    }
                    catch (DbException)
                    {
                        if ((m_UseTrxn) && (trx != null))
                        {
                            try     { trx.Rollback(); }
                            catch   { /* Ignore */ }
                        }

                        if (m_UseTrxn || failOnError)
                            throw;
                    }
                }

                trx?.Commit();
            }
            finally
            {
                if (m_cnxn.State == ConnectionState.Open)
                    m_cnxn.Close();
            }
        }
    }
}