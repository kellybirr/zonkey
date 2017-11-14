using System;
using System.Data;
using System.Data.Common;

namespace Zonkey.Ado
{
	/// <summary>
	/// EventArgs passes to BeforeExecuteCommand Events
	/// </summary>
	public class TableSaveEventArgs : EventArgs
	{
		/// <summary>
		/// Default Constructor
		/// </summary>
		/// <param name="dbAdapter"></param>
		/// <param name="dt"></param>
		public TableSaveEventArgs(DbDataAdapter dbAdapter, DataTable dt)
		{
			DbAdapter = dbAdapter;
			Table = dt;
		}

		/// <summary>
		/// The adapter doing the saving
		/// </summary>
		public DbDataAdapter DbAdapter { get; private set; }

		/// <summary>
		/// Gets or sets the table.
		/// </summary>
		/// <value>The table.</value>
		public DataTable Table { get; private set; }

		/// <summary>
		/// Set this value to true to cancel the execution of the command
		/// This will cause a OperationCanceledException to be thrown in the DataClassAdapter method.
		/// </summary>
		public bool Cancel { get; set; }
	}

}