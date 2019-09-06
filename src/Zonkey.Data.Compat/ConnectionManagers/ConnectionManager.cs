using System;
using System.Data;
using System.Data.Common;

namespace Zonkey.ConnectionManagers
{
	/// <summary>
	/// A simple and reliable implementation of a pluggable connection manager system
	/// </summary>
	public static class ConnectionManager
	{
		/// <summary>
		/// Initializes the specified instance.
		/// </summary>
		/// <param name="instance">The instance.</param>
		public static void Initialize(IConnectionManager instance)
		{
			Current?.CloseConnections();
			Current = instance;
		}

		/// <summary>
		/// Gets the static instance of the connection manager.
		/// </summary>
		/// <value>The current.</value>
		public static IConnectionManager Current { get; private set; }

		/// <summary>
		/// Gets the IDisposable representing the current context
		/// </summary>
		/// <value>The context disposer.</value>
		public static IDisposable ContextDisposer
		{
			get { return Current.ContextDisposer; }
		}

		/// <summary>
		/// Gets a connection from the context or opens a new one and stores it.
		/// </summary>
		/// <param name="name">The name of the connection.</param>
		/// <returns></returns>
		public static DbConnection GetConnection(string name)
		{
			return Current.GetConnection(name);
		}

		/// <summary>
		/// Creates and opens a new dedicated connection.
		/// </summary>
		/// <param name="name">The name of the connection.</param>
		/// <returns></returns>
		public static DbConnection GetDedicatedConnection(string name)
		{
			return Current.GetDedicatedConnection(name);
		}

		/// <summary>
		/// Sets the connection with this name for the current context.
		/// </summary>
		/// <param name="name">The name of the connection.</param>
		/// <param name="cx">The connection object to store.</param>
		public static void SetConnection(string name, DbConnection cx)
		{
			Current.SetConnection(name, cx);
		}

		/// <summary>
		/// Closes the connection.
		/// </summary>
		/// <param name="name">The name of the connection.</param>
		public static void CloseConnection(string name)
		{
			Current.CloseConnection(name);
		}

		/// <summary>
		/// Closes all open managed (non-dedicated) connections in the current context.
		/// </summary>
		public static void CloseConnections()
		{
			Current.CloseConnections();
		}

		/// <summary>
		/// Gets the active transaction from the current context or starts a new one.
		/// </summary>
		/// <returns></returns>
		public static TransactionContext GetTransaction()
		{
			return Current.GetTransaction();
		}

		/// <summary>
		/// Gets the active transaction from the current context or starts a new one
		/// with the given IsolationLevel
		/// </summary>
		/// <param name="level">The <see cref="System.Data.IsolationLevel"/> level.</param>
		/// <returns></returns>
		public static TransactionContext GetTransaction(IsolationLevel level)
		{
			return Current.GetTransaction(level);
		}

		/// <summary>
		/// Gets a value indicating whether this instance has an active transaction.
		/// </summary>
		/// <value>
		/// 	<c>true</c> if this instance has a transaction; otherwise, <c>false</c>.
		/// </value>
		public static bool HasTransaction
		{
			get { return Current.HasTransaction; }
		}

		/// <summary>
		/// Sets the instance catalog information for federated database systems.
		/// </summary>
		/// <param name="connectionName">Name of the connection.</param>
		/// <param name="dbServer">The database server for this instance in this context.</param>
		/// <param name="dbCatalog">The database catalog for this instance in this context.</param>
		public static void SetInstanceCatalog(string connectionName, string dbServer, string dbCatalog)
		{
			Current.SetReplacementValues(connectionName, new[] {dbServer, dbCatalog});
		}
	};

	/// <summary>
	/// Interface for all connection managers to implement
	/// </summary>
	public interface IConnectionManager
	{
		/// <summary>
		/// Closes the connections.
		/// </summary>
		void CloseConnections();

		/// <summary>
		/// Closes the connection.
		/// </summary>
		/// <param name="name">The name.</param>
		void CloseConnection(string name);

		/// <summary>
		/// Gets the connection.
		/// </summary>
		/// <param name="name">The name.</param>
		/// <returns></returns>
		DbConnection GetConnection(string name);

		/// <summary>
		/// Gets the dedicated connection.
		/// </summary>
		/// <param name="name">The name.</param>
		/// <returns></returns>
		DbConnection GetDedicatedConnection(string name);

		/// <summary>
		/// Sets the connection.
		/// </summary>
		/// <param name="name">The name.</param>
		/// <param name="cx">The cx.</param>
		void SetConnection(string name, DbConnection cx);

		/// <summary>
		/// Sets the replacement values.
		/// </summary>
		/// <param name="connectionName">Name of the connection.</param>
		/// <param name="values">The values.</param>
		void SetReplacementValues(string connectionName, string[] values);

		/// <summary>
		/// Gets the transaction.
		/// </summary>
		/// <returns></returns>
		TransactionContext GetTransaction();

		/// <summary>
		/// Gets the transaction and sets it to the given IsolationLevel
		/// </summary>
		/// <param name="level">The <see cref="System.Data.IsolationLevel"/> level.</param>
		/// <returns></returns>
		TransactionContext GetTransaction(IsolationLevel level);

		/// <summary>
		/// Commits the transaction.
		/// </summary>
		void CommitTransaction();

		/// <summary>
		/// Rollbacks the transaction.
		/// </summary>
		void RollbackTransaction();

		/// <summary>
		/// Gets a value indicating whether this instance has transaction.
		/// </summary>
		/// <value>
		/// 	<c>true</c> if this instance has transaction; otherwise, <c>false</c>.
		/// </value>
		bool HasTransaction { get; }

		/// <summary>
		/// Gets the IDisposable representing the current context
		/// </summary>
		/// <value>The context disposer.</value>
		IDisposable ContextDisposer { get; }
	}
}
