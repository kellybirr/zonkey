using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using Zonkey.ConnectionManagers;

namespace Zonkey.ObjectModel
{
	/// <summary>
	/// A quick adapter pattern for DCs
	/// </summary>
	/// <typeparam name="T"></typeparam>
	public abstract class DCAdapterBase<T> where T : class, new()
	{
		protected DataClassAdapter<T> BaseAdapter { get; set; }

		/// <summary>
		/// Initializes a new instance of the <see cref="DCAdapterBase&lt;T&gt;"/> class.
		/// </summary>
		/// <param name="connectionName">Name of the connection.</param>
		protected DCAdapterBase(string connectionName)
		{
			BaseAdapter = new DataClassAdapter<T>(ConnectionManager.GetConnection(connectionName));
		}

		protected DCAdapterBase()
		{ }

		/// <summary>
		/// Fills the specified collection.
		/// </summary>
		/// <param name="collection">The collection.</param>
		/// <param name="filterExpression">The filter expression.</param>
		/// <returns></returns>
		public virtual int Fill(ICollection<T> collection, Expression<Func<T, bool>> filterExpression)
			=> BaseAdapter.Fill(collection, filterExpression).ConfigureAwait(false).GetAwaiter().GetResult();

		/// <summary>
		/// Gets the single item.
		/// </summary>
		/// <param name="filterExpression">The filter expression.</param>
		/// <returns></returns>
		public virtual T GetSingleItem(Expression<Func<T, bool>> filterExpression)
			=> BaseAdapter.GetOne(filterExpression).ConfigureAwait(false).GetAwaiter().GetResult();

		/// <summary>
		/// Gets the single item.
		/// </summary>
		/// <param name="filterExpression">The filter expression.</param>
		/// <returns></returns>
		public virtual int GetCount(Expression<Func<T, bool>> filterExpression)
			=> BaseAdapter.GetCount(filterExpression).ConfigureAwait(false).GetAwaiter().GetResult();

		/// <summary>
		/// Gets the single item.
		/// </summary>
		/// <param name="filterExpression">The filter expression.</param>
		/// <returns></returns>
		public virtual bool Exists(Expression<Func<T, bool>> filterExpression)
			=> BaseAdapter.Exists(filterExpression).ConfigureAwait(false).GetAwaiter().GetResult();

		/// <summary>
		/// Saves the specified obj.
		/// </summary>
		/// <param name="obj">The obj.</param>
		/// <returns></returns>
		public virtual bool Save(T obj)
			=> BaseAdapter.Save(obj).ConfigureAwait(false).GetAwaiter().GetResult();

		/// <summary>
		/// Deletes the specified filter expression.
		/// </summary>
		/// <param name="filterExpression">The filter expression.</param>
		/// <returns></returns>
		public virtual int Delete(Expression<Func<T, bool>> filterExpression)
			=> BaseAdapter.Delete(filterExpression).ConfigureAwait(false).GetAwaiter().GetResult();

		/// <summary>
		/// Opens the reader.
		/// </summary>
		/// <param name="filterExpression">The filter expression.</param>
		/// <returns></returns>
		public virtual DataClassReader<T> OpenReader(Expression<Func<T, bool>> filterExpression)
			=> BaseAdapter.OpenReader(filterExpression).ConfigureAwait(false).GetAwaiter().GetResult();
	}

	/// <summary>
	/// A quick adapter pattern for DCs that has audit events
	/// </summary>
	/// <typeparam name="T"></typeparam>
	public class AuditingDCAdapter<T> : DCAdapterBase<T>, IDisposable
		where T : class, new()
	{
		protected SaveAuditor Auditor;
		protected bool disposed;

		/// <summary>
		/// Occurs when a save need to be audited.
		/// </summary>
		public event EventHandler<DCSaveAuditEventArgs> AuditSave;

		/// <summary>
		/// Initializes a new instance of the <see cref="AuditingDCAdapter&lt;T&gt;"/> class.
		/// </summary>
		/// <param name="connectionName">Name of the connection.</param>
		protected AuditingDCAdapter(string connectionName)
		{
			BaseAdapter = new DataClassAdapter<T>(ConnectionManager.GetConnection(connectionName));
			Auditor = new SaveAuditor(BaseAdapter, OnAuditSave);
		}

		~AuditingDCAdapter()
		{
			Dispose(false);
		}

		public void Dispose()
		{
			Dispose(true);
		}

		protected virtual void Dispose(bool disposing)
		{
			if (disposed) return;

			if (Auditor != null)
			{
				Auditor.Dispose();
				Auditor = null;
			}

			if (disposing)
			{
				GC.SuppressFinalize(this);
			}
		}

		/// <summary>
		/// Called when a save audit occurs.
		/// </summary>
		/// <param name="audit">The audit.</param>
		protected virtual void OnAuditSave(SaveAudit audit)
		{
			AuditSave?.Invoke(this, new DCSaveAuditEventArgs(audit));
		}
	}

	/// <summary>
	/// Args for DC Save audit events
	/// </summary>
	public class DCSaveAuditEventArgs : EventArgs
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="DCSaveAuditEventArgs"/> class.
		/// </summary>
		/// <param name="audit">The audit.</param>
		public DCSaveAuditEventArgs(SaveAudit audit)
		{
			Audit = audit;
		}

		/// <summary>
		/// Gets or sets the audit.
		/// </summary>
		/// <value>The audit.</value>
		public SaveAudit Audit { get; set; }
	}
}
