using System;
using System.Data;
using System.Data.Common;
using System.Threading.Tasks;
using System.Xml;

namespace Zonkey
{
    /// <summary>
    /// Provides methods for the interaction of a <see cref="System.Xml.XmlDocument"/> with a database.
    /// </summary>
    public class XmlAdapter : AdapterBase
    {
        /// <summary>
        /// Preferred constructor
        /// </summary>
        /// <param name="connection">Database Connection to be used by XmlAdapter</param>
        public XmlAdapter(DbConnection connection)
        {
            Connection = connection;
        }

        /// <summary>
        /// Default Constructor
        /// </summary>
        public XmlAdapter()
        {
        }

        /// <summary>
        /// Gets the XML document.
        /// </summary>
        /// <param name="rootName">Name of the root.</param>
        /// <param name="nodeName">Name of the node.</param>
        /// <param name="sqlText">The SQL text.</param>
        /// <param name="isProc"><c>true</c> if using stored procedure to populate XML; otherwise <c>false</c>.</param>
        /// <param name="parameters">The parameters.</param>
        /// <returns></returns>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1059:MembersShouldNotExposeCertainConcreteTypes", MessageId = "System.Xml.XmlNode")]
        public async Task<XmlDocument> GetXmlDocument(string rootName, string nodeName, string sqlText, bool isProc, params object[] parameters)
        {
            XmlDocument xDoc = new XmlDocument();
            XmlElement xRoot = xDoc.CreateElement(rootName);
            xDoc.AppendChild(xRoot);

            await FillXmlNode(xRoot, nodeName, sqlText, isProc, parameters);
            return xDoc;
        }

        /// <summary>
        /// Fills the XML node.
        /// </summary>
        /// <param name="rootNode">The root node.</param>
        /// <param name="nodeName">Name of the node.</param>
        /// <param name="sqlText">The SQL text.</param>
        /// <param name="isProc">if set to <c>true</c> [is proc].</param>
        /// <param name="parameters">The parameters.</param>
        /// <returns></returns>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1059:MembersShouldNotExposeCertainConcreteTypes", MessageId = "System.Xml.XmlNode")]
        public async Task<int> FillXmlNode(XmlNode rootNode, string nodeName, string sqlText, bool isProc, params object[] parameters)
        {
            if (rootNode == null) throw new ArgumentNullException(nameof(rootNode));

            DbCommand command = PrepareCommand(sqlText, isProc, parameters);
            using (DbDataReader reader = await command.ExecuteReaderAsync(CommandBehavior.SingleResult))
            {
                int count = 0;
                while (await reader.ReadAsync())
                {
                    XmlElement xRecord = rootNode.OwnerDocument.CreateElement(nodeName);
                    rootNode.AppendChild(xRecord);

                    for (int i = 0; i < reader.VisibleFieldCount; i++)
                    {
                        XmlElement xField = rootNode.OwnerDocument.CreateElement(reader.GetName(i));

                        if (reader.GetFieldType(i) == typeof (byte[]))
                            xField.InnerText = Convert.ToBase64String((byte[])reader[i]);
                        else
                            xField.InnerText = reader[i].ToString();

                        xRecord.AppendChild(xField);
                    }

                    count++;
                }

                return count;
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
            if (sql == null)
                throw new ArgumentNullException(nameof(sql));

            if (Connection == null)
                throw new InvalidOperationException("must set connection before calling");

            DbCommand command = Connection.CreateCommand();
            command.CommandText = sql;
            command.CommandType = (isProc) ? CommandType.StoredProcedure : CommandType.Text;
            if (parameters != null)
                DataManager.AddParamsToCommand(command, SqlDialect, parameters, ParameterPrefix);

            return command;
        }
    }
}