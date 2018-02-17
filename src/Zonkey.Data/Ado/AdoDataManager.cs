using System;
using System.Data;
using System.Data.Common;
using System.Globalization;
using System.Threading.Tasks;

namespace Zonkey.Ado
{
    public class AdoDataManager : DataManager
    {
        public AdoDataManager(DbConnection connection) : base(connection)
        { }

        /// <summary>
        /// Default Constructor
        /// </summary>
        public AdoDataManager()
        { }

        /// <summary>
        /// Allow ad-hoc queries or stored procedures to return a populated dataset object.
        /// </summary>
        /// <param name="sql">The SQL code or stored procedure name to execute</param>
        /// <param name="commandType">Type of command to execute</param>
        /// <param name="parameters">Parameter list required by the statement or stored procedure</param>
        /// <returns>A Populated DataSet</returns>
        public async Task<DataSet> GetDataSet(string sql, CommandType commandType, params object[] parameters)
        {
            using (DbDataReader reader = await GetDataReader(sql, commandType, parameters).ConfigureAwait(false))
            {
                DataSet ds = new DataSet { Locale = CultureInfo.InvariantCulture };

                int i = 0;
                while (!reader.IsClosed)
                {
                    DataTable dt = new DataTable($"Table{++i}") { Locale = CultureInfo.InvariantCulture };
                    dt.Load(reader, LoadOption.OverwriteChanges);
                    ds.Tables.Add(dt);
                }

                return ds;
            }
        }


        /// <summary>
        /// Allow ad-hoc queries or stored procedures to fill a data table.
        /// Automatically overwrites changes in the current data table rows.
        /// </summary>
        /// <param name="dt">The DataTable object to fill</param>
        /// <param name="sql">The SQL code or stored procedure name to execute</param>
        /// <param name="commandType">Type of command to execute</param>
        /// <param name="parameters">Parameter list required by the statement or stored procedure</param>
        public async Task FillDataTable(DataTable dt, string sql, CommandType commandType, params object[] parameters)
        {
            if (dt == null)
                throw new ArgumentNullException(nameof(dt));

            using (DbDataReader reader = await GetDataReader(sql, commandType, parameters).ConfigureAwait(false))
                dt.Load(reader, LoadOption.OverwriteChanges);
        }


        /// <summary>
        /// Gets the child relation.
        /// </summary>
        /// <param name="dataItem">The data item.</param>
        /// <param name="relation">The relation.</param>
        /// <returns>A <see cref="System.Data.DataView"/> for the child <see cref="System.Data.DataTable"/>.</returns>
        public static DataView GetChildRelation(object dataItem, string relation)
        {
            DataRowView drv = dataItem as DataRowView;
            return drv?.CreateChildView(relation);
        }

    }
}
