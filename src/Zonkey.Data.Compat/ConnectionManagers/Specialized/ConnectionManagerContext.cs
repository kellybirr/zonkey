using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Linq;

namespace Zonkey.ConnectionManagers.Specialized
{
	/// <summary>
	/// The context store for a base connection manger.
	/// </summary>
	public sealed class ConnectionManagerContext : IDisposable
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="ConnectionManagerContext"/> class.
		/// </summary>
		public ConnectionManagerContext()
		{
			Connections = new Dictionary<string, DbConnection>();
			Replacements = new Dictionary<string, string[]>();
		}

		/// <summary>
		/// Gets or sets the connections.
		/// </summary>
		/// <value>The connections.</value>
		public IDictionary<string, DbConnection> Connections { get; private set; }

		/// <summary>
		/// Gets or sets the replacements.
		/// </summary>
		/// <value>The replacements.</value>
		public IDictionary<string, string[]> Replacements { get; private set; }

		/// <summary>
		/// Gets or sets the transaction.
		/// </summary>
		/// <value>The transaction.</value>
		public TransactionContext Transaction { get; set; }

		/// <summary>
		/// Gets the connection.
		/// </summary>
		/// <param name="name">The name.</param>
		/// <returns></returns>
		public DbConnection GetConnection(string name)
		{
			DbConnection cnxn;
			Connections.TryGetValue(name, out cnxn);

			return cnxn;
		}

		/// <summary>
		/// Gets the replacements.
		/// </summary>
		/// <param name="connectionName">Name of the connection.</param>
		/// <returns></returns>
		public string[] GetReplacements(string connectionName)
		{
			string[] result;
			Replacements.TryGetValue(connectionName, out result);

			return result;
		}

		/// <summary>
		/// Closes the connection.
		/// </summary>
		/// <param name="name">The name.</param>
		public void CloseConnection(string name)
		{
			// retrieve connection
			var cnxn = GetConnection(name);
			if (cnxn == null) return;

			// check transaction
			var trx = DbTransactionRegistry.RetrieveTransaction(cnxn);
			if (trx != null)
			{
				trx.Rollback();
				DbTransactionRegistry.RemoveTransaction(cnxn);
			}

			// close connection
			if (cnxn.State == ConnectionState.Open)
				cnxn.Close();

			// remove connection
			Connections.Remove(name);
		}

		/// <summary>
		/// Closes the connections.
		/// </summary>
		public void CloseConnections()
		{
			FinishTransaction(false);

			// avoid collection modified exceptions
			string[] connections = Connections.Keys.ToArray();
			foreach (string sKey in connections)
				CloseConnection(sKey);
		}

		/// <summary>
		/// Gets the transaction.
		/// </summary>
		/// <param name="level">The level.</param>
		/// <returns></returns>
		public TransactionContext GetTransaction(IsolationLevel level)
		{
			if (Transaction == null)
			{
				Transaction = new TransactionContext();
				foreach (DbConnection cnxn in Connections.Values)
				{
					if (cnxn == null) continue;
					DbTransactionRegistry.RegisterNewTransaction(cnxn, level);
				}
			}

			return Transaction;
		}

		/// <summary>
		/// Finishes the transaction.
		/// </summary>
		/// <param name="commit">if set to <c>true</c> [commit].</param>
		public void FinishTransaction(bool commit)
		{

			var trxn = Transaction;
			if (trxn == null) return;

			foreach (var cxPair in Connections)
			{
				var cnxn = cxPair.Value;
				if (cnxn == null) continue;

				var dbTrx = DbTransactionRegistry.RetrieveTransaction(cnxn);
				if (dbTrx == null) continue;

				if (cnxn.State == ConnectionState.Open)
				{
					if (commit)
						dbTrx.Commit();
					else
						dbTrx.Rollback();
				}

				DbTransactionRegistry.RemoveTransaction(cnxn);
			}

			Transaction = null;
		}

		/// <summary>
		/// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
		/// </summary>
		public void Dispose()
		{
			lock (this)
			{
				// close all connections on dispose
				CloseConnections();

				// empty dictionaries
				Connections.Clear();
				Replacements.Clear();
			}

		}
	}
}
