using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;

namespace Zonkey
{
	/// <summary>
	/// Manages leightweight dbtransactions based on their parent connection
	/// </summary>
	public static class DbTransactionRegistry
	{
		private static readonly Dictionary<DbConnection, DbTransaction> _transactions;

		static DbTransactionRegistry()
		{
			_transactions = new Dictionary<DbConnection, DbTransaction>();
		}

		/// <summary>
		/// Begins and Registers a new transaction.
		/// </summary>
		/// <param name="connection">The connection.</param>
		public static void RegisterNewTransaction(DbConnection connection)
		{
			if (connection == null)
				throw new ArgumentNullException(nameof(connection));

			lock (_transactions)
				_transactions[connection] = connection.BeginTransaction();
		}

		/// <summary>
		/// Begins and Registers a new transaction.
		/// </summary>
		/// <param name="connection">The connection.</param>
		/// <param name="level">The level.</param>
		public static void RegisterNewTransaction(DbConnection connection, IsolationLevel level)
		{
			if (connection == null)
				throw new ArgumentNullException(nameof(connection));

			lock (_transactions)
				_transactions[connection] = connection.BeginTransaction(level);
		}

		/// <summary>
		/// Registers the transaction with the connection.
		/// </summary>
		/// <param name="connection">The connection</param>
		/// <param name="transaction">The transaction</param>
		public static void RegisterTransaction(DbConnection connection, DbTransaction transaction)
		{
			if (connection == null)
				throw new ArgumentNullException(nameof(connection));

			if (transaction == null)
				throw new ArgumentNullException(nameof(transaction));

			lock (_transactions)
				_transactions[connection] = transaction;
		}

		/// <summary>
		/// Retrieves the transaction.
		/// </summary>
		/// <param name="connection">The connection.</param>
		/// <returns></returns>
		public static DbTransaction RetrieveTransaction(DbConnection connection)
		{
			if (connection == null)
				throw new ArgumentNullException(nameof(connection));

			lock (_transactions)
			{
				DbTransaction trx;
				return (_transactions.TryGetValue(connection, out trx)) ? trx : null;
			}
		}

		/// <summary>
		/// Removes the transaction.
		/// </summary>
		/// <param name="connection">The connection.</param>
		public static void RemoveTransaction(DbConnection connection)
		{
			if (connection == null)
				throw new ArgumentNullException(nameof(connection));

			lock (_transactions)
			{
				if (_transactions.ContainsKey(connection))
					_transactions.Remove(connection);
			}
		}

		/// <summary>
		/// Sets the transaction for a command based on it's associated connection.
		/// </summary>
		/// <param name="command">The command.</param>
		public static void SetCommandTransaction(DbCommand command)
		{
			if (command == null)
				throw new ArgumentNullException(nameof(command));

			lock (_transactions)
			lock (command)
			{
				DbTransaction trx;
				if (_transactions.TryGetValue(command.Connection, out trx))
					command.Transaction = trx;
			}
		}

		/// <summary>
		/// Clears this instance.
		/// </summary>
		public static void Clear()
		{
			lock (_transactions)
				_transactions.Clear();
		}
	}
}
