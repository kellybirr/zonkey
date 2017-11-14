using System;
using System.Data;
using System.Data.Common;
using System.Data.SqlClient;
using System.Text;
using System.Threading.Tasks;
using System.Xml;

namespace Zonkey.SqlServer
{
    /// <summary>
    /// Provides methods for the interaction of a <see cref="System.Xml.XmlDocument"/> with a database.
    /// This class uses and depends on SqlXml Support
    /// </summary>
    public class SqlXmlAdapter : AdapterBase
    {
        /// <summary>
        /// Preferred constructor
        /// </summary>
        /// <param name="connection">Database Connection to be used by XmlAdapter</param>
        public SqlXmlAdapter(DbConnection connection)
        {
            Connection = connection;
        }

        /// <summary>
        /// Default Constructor
        /// </summary>
        public SqlXmlAdapter()
        {
        }

        /// <summary>
        /// Gets the XML document.
        /// </summary>
        /// <param name="rootName">Name of the root.</param>
        /// <param name="sqlText">The SQL text.</param>
        /// <param name="isProc"><c>true</c> if using stored procedure to populate XML; otherwise <c>false</c>.</param>
        /// <param name="parameters">The parameters.</param>
        /// <returns></returns>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1059:MembersShouldNotExposeCertainConcreteTypes", MessageId = "System.Xml.XmlNode")]
        public async Task<XmlDocument> GetXmlDocument(string rootName, string sqlText, bool isProc, params object[] parameters)
        {
            var xDoc = new XmlDocument();
            XmlElement xRoot = xDoc.CreateElement(rootName);
            xDoc.AppendChild(xRoot);

            await FillXmlNode(xRoot, sqlText, isProc, parameters);
            return xDoc;
        }

        /// <summary>
        /// Fills the XML node.
        /// </summary>
        /// <param name="rootNode">The root node.</param>
        /// <param name="sqlText">The SQL text.</param>
        /// <param name="isProc">if set to <c>true</c> [is proc].</param>
        /// <param name="parameters">The parameters.</param>
        /// <returns></returns>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1059:MembersShouldNotExposeCertainConcreteTypes", MessageId = "System.Xml.XmlNode")]
        public async Task<int> FillXmlNode(XmlNode rootNode, string sqlText, bool isProc, params object[] parameters)
        {
            if (rootNode == null) throw new ArgumentNullException(nameof(rootNode));

            var command = (SqlCommand)PrepareCommand(sqlText, isProc, parameters);
            using (XmlReader reader = await command.ExecuteXmlReaderAsync())
            {
                int count = 0, depth = 0;
                XmlElement lastElement = null;
                var parentElement = (XmlElement)rootNode;
                while (await reader.ReadAsync())
                {
                    reader.MoveToElement();

                    if (reader.NodeType == XmlNodeType.EndElement)
                        continue;

                    if (reader.Depth < depth)
                        parentElement = (XmlElement)parentElement.ParentNode;
                    else if ((reader.Depth > depth) && (lastElement != null))
                        parentElement = lastElement;

                    depth = reader.Depth;

                    XmlElement xElement = rootNode.OwnerDocument.CreateElement(reader.Name);
                    parentElement.AppendChild(xElement);
                    if (depth == 0) count++;

                    if (reader.AttributeCount > 0)
                    {
                        while (reader.MoveToNextAttribute())
                            xElement.SetAttribute(reader.Name, reader.Value);
                    }

                    lastElement = xElement;
                }

                return count;
            }
        }

        /// <summary>
        /// Gets the XML data as a string
        /// </summary>
        /// <param name="sqlText">The SQL text.</param>
        /// <param name="isProc">if set to <c>true</c> [is proc].</param>
        /// <param name="parameters">The parameters.</param>
        /// <returns></returns>
        public async Task<string> GetXmlString(string sqlText, bool isProc, params object[] parameters)
        {
            var command = (SqlCommand)PrepareCommand(sqlText, isProc, parameters);
            using (SqlDataReader reader = await command.ExecuteReaderAsync())
            {
                var sb = new StringBuilder();
                while (await reader.ReadAsync())
                    sb.Append(reader.GetString(0));

                return (sb.Length > 0) ? sb.ToString() : null;
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
