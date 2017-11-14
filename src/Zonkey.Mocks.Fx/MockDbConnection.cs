using System;
using System.Data;
using System.Data.Common;

namespace Zonkey.Mocks
{
    /// <summary>
    /// A Mock of a DbConnection for unit testing
    /// </summary>
    public class MockDbConnection : DbConnection
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="MockDbConnection"/> class.
        /// </summary>
        /// <param name="connectionString">The connection string.</param>
        public MockDbConnection(string connectionString)
        {
            ConnectionString = connectionString;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="MockDbConnection"/> class.
        /// </summary>
        public MockDbConnection()
        { }

        /// <summary>
        /// Starts a database transaction.
        /// </summary>
        /// <param name="isolationLevel">Specifies the isolation level for the transaction.</param>
        /// <returns>
        /// An object representing the new transaction.
        /// </returns>
        protected override DbTransaction BeginDbTransaction(IsolationLevel isolationLevel)
        {
            if (ActiveTransaction != null)
                throw new InvalidOperationException("There is already an active transaction for this connection");

            ActiveTransaction = new MockDbTransaction(this, IsolationLevel.ReadCommitted);
            return ActiveTransaction;
        }

        /// <summary>
        /// Gets or sets the active transaction.
        /// </summary>
        /// <value>The active transaction.</value>
        internal DbTransaction ActiveTransaction { get; set; }

        /// <summary>
        /// Changes the current database for an open connection.
        /// </summary>
        /// <remarks>NOT IMPLEMENTED</remarks>
        /// <param name="databaseName">Specifies the name of the database for the connection to use.</param>
        public override void ChangeDatabase(string databaseName)
        { }

        /// <summary>
        /// Closes the connection to the database. This is the preferred method of closing any open connection.
        /// </summary>
        /// <exception cref="T:System.Data.Common.DbException">The connection-level error that occurred while opening the connection. </exception>
        public override void Close()
        {
            _state = ConnectionState.Closed;
        }

        /// <summary>
        /// Gets or sets the string used to open the connection.
        /// </summary>
        /// <value></value>
        /// <returns>The connection string used to establish the initial connection. The exact contents of the connection string depend on the specific data source for this connection. The default value is an empty string.</returns>
        public sealed override string ConnectionString { get; set; }

        /// <summary>
        /// Creates and returns a <see cref="T:System.Data.Common.DbCommand"/> object associated with the current connection.
        /// </summary>
        /// <returns>
        /// A <see cref="T:System.Data.Common.DbCommand"/> object.
        /// </returns>
        protected override DbCommand CreateDbCommand()
        {
            MockDbCommand command = (CreateCommandFunc == null)
                ? new MockDbCommand { Connection = this }
                : CreateCommandFunc(this);

            SetupCommandFunc?.Invoke(command);

            return command;
        }

        /// <summary>
        /// Hook this delegate to override command creation (not usually required)
        /// </summary>
        public Func<MockDbConnection, MockDbCommand> CreateCommandFunc;

        /// <summary>
        /// Hook this delegate to control command setup (common)
        /// </summary>
        public Action<MockDbCommand> SetupCommandFunc;

        /// <summary>
        /// Gets the name of the database server to which to connect.
        /// </summary>
        /// <value></value>
        /// <returns>The name of the database server to which to connect. The default value is an empty string.</returns>
        public override string DataSource
        {
            get { return "Zonkey Mock DataSource"; }
        }

        /// <summary>
        /// Gets the name of the current database after a connection is opened, or the database name specified in the connection string before the connection is opened.
        /// </summary>
        /// <value></value>
        /// <returns>The name of the current database or the name of the database to be used after a connection is opened. The default value is an empty string.</returns>
        public override string Database
        {
            get { return "Zonkey Mock Database"; }
        }

        /// <summary>
        /// Opens a database connection with the settings specified by the <see cref="P:System.Data.Common.DbConnection.ConnectionString"/>.
        /// </summary>
        public override void Open()
        {
            _state = ConnectionState.Open;
        }

        /// <summary>
        /// Gets a string that represents the version of the server to which the object is connected.
        /// </summary>
        /// <value></value>
        /// <returns>The version of the database. The format of the string returned depends on the specific type of connection you are using.</returns>
        public override string ServerVersion
        {
            get { return GetType().Assembly.GetName().Version.ToString(); }
        }

        /// <summary>
        /// Gets a string that describes the state of the connection.
        /// </summary>
        /// <value></value>
        /// <returns>The state of the connection. The format of the string returned depends on the specific type of connection you are using.</returns>
        public override ConnectionState State
        {
            get { return _state; }
        }
        private ConnectionState _state = ConnectionState.Closed;
    } 
}
