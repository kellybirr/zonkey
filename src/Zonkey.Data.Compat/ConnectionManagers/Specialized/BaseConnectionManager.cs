using System;
using System.Data;
using System.Data.Common;

namespace Zonkey.ConnectionManagers.Specialized
{
	/// <summary>
	/// The base class for a standard connection manager instance
	/// </summary>
	public abstract class BaseConnectionManager : IConnectionManager
	{
		/// <summary>
		/// Gets the connection.
		/// </summary>
		/// <param name="name">The name.</param>
		/// <returns></returns>
		public DbConnection GetConnection(string name)
		{
			// get context
			ConnectionManagerContext ctx = Context;

			// check if already open
			var cnxn = ctx.GetConnection(name);
			if ((cnxn != null) && (cnxn.State == ConnectionState.Open)) return cnxn;

			// prepare connection info
			cnxn = PerpareConnection(name);

			// open connection
			try
			{
				cnxn.Open();

				// enroll in transaction
				if (ctx.Transaction != null)
					DbTransactionRegistry.RegisterNewTransaction(cnxn);

				SetConnection(name, cnxn);

				return cnxn;
			}
			catch (Exception ex)
			{
				throw new DataException("Unable to connect to database", ex);
			}
		}

		/// <summary>
		/// Gets the dedicated connection.
		/// </summary>
		/// <param name="name">The name.</param>
		/// <returns></returns>
		public DbConnection GetDedicatedConnection(string name)
		{
			// prepare connection info
			var cnxn = PerpareConnection(name);

			// open connection
			try
			{
				cnxn.Open();

				// DO NOT enroll in transaction
				return cnxn;
			}
			catch (Exception ex)
			{
				throw new DataException("Unable to connect to database", ex);
			}
		}

		private DbConnection PerpareConnection(string name)
		{
			OnPrepareConnection();

			// get connection from factory
			DbConnection cnxn = DbConnectionFactory.CreateConnection(name);

			// if replacement values set
			string[] replacementValues = Context.GetReplacements(name);
			if ((replacementValues != null) && (!string.IsNullOrEmpty(cnxn.ConnectionString)))
				cnxn.ConnectionString = string.Format(cnxn.ConnectionString, replacementValues);

			// other connection)
			return cnxn;
		}

		/// <summary>
		/// Sets the connection.
		/// </summary>
		/// <param name="name">The name.</param>
		/// <param name="cx">The cx.</param>
		public void SetConnection(string name, DbConnection cx)
		{
			if (cx == null)
				Context.Connections.Remove(name);
			else
				Context.Connections[name] = cx;
		}

		/// <summary>
		/// Closes the connection.
		/// </summary>
		/// <param name="name">The name.</param>
		public void CloseConnection(string name)
		{
			Context.CloseConnection(name);
		}

		/// <summary>
		/// Closes the connections.
		/// </summary>
		public void CloseConnections()
		{
			Context.CloseConnections();
		}

		/// <summary>
		/// Sets the replacement values.
		/// </summary>
		/// <param name="connectionName">Name of the connection.</param>
		/// <param name="values">The values.</param>
		public void SetReplacementValues(string connectionName, string[] values)
		{
			ConnectionManagerContext ctx = Context;

			ctx.CloseConnection(connectionName);
			ctx.Replacements[connectionName] = values;
		}

		/// <summary>
		/// Gets the transaction.
		/// </summary>
		/// <returns></returns>
		public TransactionContext GetTransaction()
		{
			return Context.GetTransaction(IsolationLevel.Unspecified);
		}

		/// <summary>
		/// Gets the transaction, with a specified isolation level.
		/// </summary>
		/// <returns></returns>
		public TransactionContext GetTransaction(IsolationLevel level)
		{
			return Context.GetTransaction(level);
		}

		/// <summary>
		/// Gets a value indicating whether this instance has transaction.
		/// </summary>
		/// <value>
		/// 	<c>true</c> if this instance has transaction; otherwise, <c>false</c>.
		/// </value>
		public bool HasTransaction
		{
			get { return (Context.Transaction != null); }
		}

		/// <summary>
		/// Commits the transaction.
		/// </summary>
		public void CommitTransaction()
		{
			Context.FinishTransaction(true);
		}

		/// <summary>
		/// Rollbacks the transaction.
		/// </summary>
		public void RollbackTransaction()
		{
			Context.FinishTransaction(false);
		}

		/// <summary>
		/// Gets the context.
		/// </summary>
		/// <value>The context.</value>
		protected abstract ConnectionManagerContext Context { get; }

		/// <summary>
		/// Gets the IDisposable representing the current context
		/// </summary>
		/// <value>The context disposer.</value>
		public IDisposable ContextDisposer
		{
			get { return Context; }
		}

		protected virtual void OnPrepareConnection()
		{

		}
	}
}
