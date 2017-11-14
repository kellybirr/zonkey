using System;
using System.Data;
using System.Data.Common;

namespace Zonkey.Mocks
{
    /// <summary>
    /// A mock of a DbCommand class for unit testing
    /// </summary>
    public class MockDbCommand : DbCommand
    {
        protected internal MockDbCommand()
        {        
        }

        /// <summary>
        /// Attempts to cancels the execution of a <see cref="T:System.Data.Common.DbCommand"/>.
        /// </summary>
        public override void Cancel()
        {
            DoCancel?.Invoke(this);
        }

        /// <summary>
        /// Hook this delegate for testing when Cancel() is called.
        /// </summary>
        public Action<MockDbCommand> DoCancel;

        /// <summary>
        /// Gets or sets the text command to run against the data source.
        /// </summary>
        /// <value></value>
        /// <returns>The text command to execute. The default value is an empty string ("").</returns>
        public override string CommandText { get; set; }

        /// <summary>
        /// Gets or sets the wait time before terminating the attempt to execute a command and generating an error.
        /// </summary>
        /// <value></value>
        /// <returns>The time in seconds to wait for the command to execute.</returns>
        public override int CommandTimeout { get; set; }

        /// <summary>
        /// Indicates or specifies how the <see cref="P:System.Data.Common.DbCommand.CommandText"/> property is interpreted.
        /// </summary>
        /// <value></value>
        /// <returns>One of the <see cref="T:System.Data.CommandType"/> values. The default is Text.</returns>
        public override CommandType CommandType { get; set; }

        /// <summary>
        /// Creates a new instance of a <see cref="T:System.Data.Common.DbParameter"/> object.
        /// </summary>
        /// <returns>
        /// A <see cref="T:System.Data.Common.DbParameter"/> object.
        /// </returns>
        protected override DbParameter CreateDbParameter()
        {
            return new MockDbParameter();
        }

        /// <summary>
        /// Gets or sets the <see cref="T:System.Data.Common.DbConnection"/> used by this <see cref="T:System.Data.Common.DbCommand"/>.
        /// </summary>
        /// <value></value>
        /// <returns>The connection to the data source.</returns>
        protected override DbConnection DbConnection { get; set; }

        /// <summary>
        /// Gets the collection of <see cref="T:System.Data.Common.DbParameter"/> objects.
        /// </summary>
        /// <value></value>
        /// <remarks>NOT IMPLEMENTED</remarks>
        /// <returns>The parameters of the SQL statement or stored procedure.</returns>
        protected override DbParameterCollection DbParameterCollection
        {
            get { return _parameters; }
        }        
        private readonly MockDbParameterCollection _parameters = new MockDbParameterCollection();

        /// <summary>
        /// Gets or sets the <see cref="P:System.Data.Common.DbCommand.DbTransaction"/> within which this <see cref="T:System.Data.Common.DbCommand"/> object executes.
        /// </summary>
        /// <value></value>
        /// <returns>The transaction within which a Command object of a .NET Framework data provider executes. The default value is a null reference (Nothing in Visual Basic).</returns>
        protected override DbTransaction DbTransaction { get; set; }
        
        /// <summary>
        /// Gets or sets a value indicating whether the command object should be visible in a customized interface control.
        /// </summary>
        /// <value></value>
        /// <returns>true, if the command object should be visible in a control; otherwise false. The default is true.</returns>
        public override bool DesignTimeVisible { get; set; }

        /// <summary>
        /// Executes the command text against the connection.
        /// </summary>
        /// <param name="behavior">An instance of <see cref="T:System.Data.CommandBehavior"/>.</param>
        /// <returns>
        /// A <see cref="T:System.Data.Common.DbDataReader"/>.
        /// </returns>
        protected override DbDataReader ExecuteDbDataReader(CommandBehavior behavior)
        {
            return new MockDbDataReader(DoExecuteReader(this));
        }

        public Func<MockDbCommand, object> DoExecuteReader;

        /// <summary>
        /// Executes a SQL statement against a connection object.
        /// </summary>
        /// <returns>The number of rows affected.</returns>
        public override int ExecuteNonQuery()
        {
            return DoExecuteNonQuery(this);
        }

        /// <summary>
        /// Hook this delegate for testing when ExecuteNonQuery() is called
        /// </summary>
        public Func<MockDbCommand, int> DoExecuteNonQuery;

        /// <summary>
        /// Executes the query and returns the first column of the first row in the result set returned by the query. All other columns and rows are ignored.
        /// </summary>
        /// <returns>
        /// The first column of the first row in the result set.
        /// </returns>
        public override object ExecuteScalar()
        {
            return DoExecuteScalar(this);
        }

        /// <summary>
        /// Hook this delegate for testing when ExecuteScalar() is called
        /// </summary>
        public Func<MockDbCommand, object> DoExecuteScalar;

        /// <summary>
        /// Creates a prepared (or compiled) version of the command on the data source.
        /// </summary>
        public override void Prepare()
        { }

        /// <summary>
        /// Gets or sets how command results are applied to the <see cref="T:System.Data.DataRow"/> when used by the Update method of a <see cref="T:System.Data.Common.DbDataAdapter"/>.
        /// </summary>
        /// <value></value>
        /// <returns>One of the <see cref="T:System.Data.UpdateRowSource"/> values. The default is Both unless the command is automatically generated. Then the default is None.</returns>
        public override UpdateRowSource UpdatedRowSource { get; set; }
    }
}
