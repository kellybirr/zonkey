using System;
using System.Data;
using System.Data.Common;
using System.Data.SqlClient;

namespace Zonkey.Ado
{
    /// <summary>
    /// Provides methods for the interaction of a <see cref="System.Data.DataTable"/> with a database.
    /// </summary>
    public class DataTableAdapter : AdapterBase2
    {
        //private bool _cancelFill;
        private DbProviderFactory _providerFactory;

        /// <summary>
        /// Preferred constructor
        /// </summary>
        /// <param name="connection">Database Connection to be used by the adapter</param>
        public DataTableAdapter(DbConnection connection) : this()
        {
            Connection = connection;
        }

        /// <summary>
        /// Default constructor - must assign connection before use
        /// </summary>
        public DataTableAdapter()
        {
            // init default command builder creator
            CreateCommandBuilder = CreateCommandBuilderInternal;
        }

        /// <summary>
        /// Occurs when [before save changes].
        /// </summary>
        public event EventHandler<TableSaveEventArgs> BeforeSaveChanges;

        /// <summary>
        /// Called when Database connection Changes.
        /// </summary>
        protected override void OnConnectionChanged()
        {
            _providerFactory = DbConnectionFactory.GetFactory(Connection.GetType().Namespace);
        }

        /// <summary>
        /// Selects all rows from a table, without filtering
        /// </summary>
        public void FillAll(DataTable dataTable)
        {
            if (dataTable == null)
                throw new ArgumentNullException(nameof(dataTable));

            DbDataAdapter dataAdapter = _providerFactory.CreateDataAdapter();
            DataTableCommandBuilder builder = CreateCommandBuilder(dataTable, Connection);

            dataAdapter.SelectCommand = builder.SelectAllCommand;
            EnrollInTransaction(dataAdapter.SelectCommand);

            dataAdapter.TableMappings.Add("Table", dataTable.TableName);
            dataAdapter.Fill(dataTable);
        }

        /// <summary>
        /// Fills DataTable with simple text filter
        /// </summary>
        public void Fill(DataTable dataTable, string filter)
        {
            if (dataTable == null)
                throw new ArgumentNullException(nameof(dataTable));

            DbDataAdapter dataAdapter = _providerFactory.CreateDataAdapter();
            DataTableCommandBuilder builder = CreateCommandBuilder(dataTable, Connection);

            dataAdapter.SelectCommand = builder.GetSelectCommand(filter);
            EnrollInTransaction(dataAdapter.SelectCommand);

            dataAdapter.TableMappings.Add("Table", dataTable.TableName);
            dataAdapter.Fill(dataTable);
        }

        /// <summary>
        /// Fills DataTable with filtered data. Parameters are passed
        /// using SqlParamater objects.
        /// </summary>
        public void Fill(DataTable dataTable, string filter, params object[] parameters)
        {
            if (dataTable == null)
                throw new ArgumentNullException(nameof(dataTable));

            DbDataAdapter dataAdapter = _providerFactory.CreateDataAdapter();
            DataTableCommandBuilder builder = CreateCommandBuilder(dataTable, Connection);

            dataAdapter.SelectCommand = builder.GetSelectCommand(filter);
            EnrollInTransaction(dataAdapter.SelectCommand);

            DataManager.AddParamsToCommand(dataAdapter.SelectCommand, SqlDialect, parameters);

            dataAdapter.TableMappings.Add("Table", dataTable.TableName);
            dataAdapter.Fill(dataTable);
        }

        /// <summary>
        /// Fills DataTable with filtered data. Parameters are passed
        /// using SqlParamater objects.
        /// </summary>
        public void Fill(DataTable dataTable, params SqlFilter[] filters)
        {
            if (dataTable == null)
                throw new ArgumentNullException(nameof(dataTable));

            DbDataAdapter dataAdapter = _providerFactory.CreateDataAdapter();
            DataTableCommandBuilder builder = CreateCommandBuilder(dataTable, Connection);
            dataAdapter.SelectCommand = builder.GetSelectCommand(filters);
            EnrollInTransaction(dataAdapter.SelectCommand);

            dataAdapter.TableMappings.Add("Table", dataTable.TableName);
            dataAdapter.Fill(dataTable);
        }

        /// <summary>
        /// Fills data table using stored procedure
        /// </summary>
        public void FillWithSP(DataTable dataTable, string storedProcName, params object[] parameters)
        {
            if (dataTable == null)
                throw new ArgumentNullException(nameof(dataTable));

            DbCommand command = Connection.CreateCommand();
            command.CommandText = storedProcName;
            command.CommandType = CommandType.StoredProcedure;
            DataManager.AddParamsToCommand(command, SqlDialect, parameters);

            DbDataAdapter dataAdapter = _providerFactory.CreateDataAdapter();
            dataAdapter.SelectCommand = command;
            EnrollInTransaction(dataAdapter.SelectCommand);

            dataAdapter.TableMappings.Add("Table", dataTable.TableName);
            dataAdapter.Fill(dataTable);
        }

        /// <summary>
        /// Performs all update operations for changed rows in a table
        /// </summary>
        public int SaveChanges(DataTable dataTable)
        {
            if (dataTable == null)
                throw new ArgumentNullException(nameof(dataTable));

            DataTableCommandBuilder builder = CreateCommandBuilder(dataTable, Connection);
            builder.SequenceName = SequenceName;

            DbDataAdapter dataAdapter = _providerFactory.CreateDataAdapter();
            dataAdapter.UpdateCommand = builder.UpdateCommand;
            dataAdapter.DeleteCommand = builder.DeleteCommand;
            dataAdapter.InsertCommand = builder.InsertCommand;

            // enroll in transactions
            EnrollInTransaction(dataAdapter.UpdateCommand);
            EnrollInTransaction(dataAdapter.DeleteCommand);
            EnrollInTransaction(dataAdapter.InsertCommand);

            if (BeforeSaveChanges != null)
            {
                var args = new TableSaveEventArgs(dataAdapter, dataTable);
                BeforeSaveChanges(this, args);

                if (args.Cancel)
                    throw new OperationCanceledException("Save Changes Canceled");
            }

            return dataAdapter.Update(dataTable);
        }

        /// <summary>
        /// Performs all update operations for changed rows in a table in a <see cref="System.Data.DataSet"/>.
        /// </summary>
        /// <param name="ds">The DataSet.</param>
        /// <param name="tableName">Name of the table in the DataSet.</param>
        /// <returns></returns>
        public int SaveChanges(DataSet ds, string tableName)
        {
            if (ds == null)
                throw new ArgumentNullException(nameof(ds));

            return SaveChanges(ds.Tables[tableName]);
        }

        /// <summary>
        /// Performs all update operations for changed rows in a table in a <see cref="System.Data.DataSet"/>.
        /// </summary>
        /// <param name="ds">The DataSet.</param>
        /// <param name="tableIndex">Index of the table.</param>
        /// <returns></returns>
        public int SaveChanges(DataSet ds, int tableIndex)
        {
            if (ds == null)
                throw new ArgumentNullException(nameof(ds));

            return SaveChanges(ds.Tables[tableIndex]);
        }

        /// <summary>
        /// Gets or sets the name of the sequence for an auto-increment column.
        /// </summary>
        /// <value>The name of the sequence.</value>
        public string SequenceName { get; set; }

        /// <summary>
        /// The dynamically overridable function used to create command builders
        /// </summary>
        public Func<DataTable, DbConnection, DataTableCommandBuilder> CreateCommandBuilder { get; set; }

        private static DataTableCommandBuilder CreateCommandBuilderInternal(DataTable table, DbConnection connection)
        {
            return new DataTableCommandBuilder(table, connection);
        }
    }
}