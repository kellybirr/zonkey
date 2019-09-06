using System;

namespace Zonkey.ConnectionManagers
{
	/// <summary>
	/// A simple disposable wrapper for a registered transaction
	/// </summary>
	public class TransactionContext : IDisposable
	{
		private int _status;

		/// <summary>
		/// Initializes a new instance of the <see cref="TransactionContext"/> class.
		/// </summary>
		public TransactionContext()
		{
			_status = 0;
		}

		/// <summary>
		/// Commits this transaction.
		/// </summary>
		public void Commit()
		{
			ConnectionManager.Current.CommitTransaction();
			_status = 1;
		}

		/// <summary>
		/// Rollbacks this transaction.
		/// </summary>
		public void Rollback()
		{
			ConnectionManager.Current.RollbackTransaction();
			_status = -1;
		}

		#region IDisposable Members

		/// <summary>
		/// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
		/// Also rolls-back the transaction if not previously committed
		/// </summary>
		public void Dispose()
		{
			if (_status == 0)
				Rollback();

			GC.SuppressFinalize(this);
		}

		/// <summary>
		/// Releases unmanaged resources and performs other cleanup operations before the
		/// <see cref="TransactionContext"/> is reclaimed by garbage collection.
		/// </summary>
		~TransactionContext()
		{
			Dispose();
		}

		#endregion
	}
}
