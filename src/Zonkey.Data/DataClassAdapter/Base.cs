using System;
using System.Data.Common;
using Zonkey.ObjectModel;

namespace Zonkey
{
	/// <summary>
	/// Static class to support static methods on the data class adapter
	/// </summary>
	public abstract class DataClassAdapter : AdapterBase2
	{
		#region Properties

		/// <summary>
		/// Gets or sets the data map.
		/// </summary>
		/// <value>
		/// The data map.
		/// </value>
		protected DataMap DataMap { get; set; }

		/// <summary>
		/// Gets or Sets the CommandTimeout for all DbCommands used internally by the DataClassAdapter
		/// </summary>
		public int? CommandTimeout { get; set; }

		/// <summary>
		/// Gets or Sets a value indicating is the count of updated rows should be ignored on updates
		/// Disabling this can be dangerous, use extreme caution.
		/// </summary>
		public bool IgnoreUpdateRowCount { get; set; }

		#endregion


		#region Static Properties

		/// <summary>
		/// Gets or Sets the CommandTimeout for all DbCommands used internally by the any DataClassAdapter 
		/// that does not set it's CommandTimeout
		/// </summary>
		public static int? DefaultCommandTimeout { get; set; }

		/// <summary>
		/// Gets or Sets the SchemaVersion for all DataClassAdapters in the current process
		/// </summary>
		public static int? DefaultSchemaVersion { get; set; }

		/// <summary>
		/// Gets or sets the default quoted identifiers value.
		/// </summary>
		/// <value>The default quoted identifiers.</value>
		public static bool? DefaultQuotedIdentifier { get; set; }

		#endregion

		#region Events

		/// <summary>
		/// Fire before a command is executed against the database
		/// </summary>
		public event EventHandler<CommandExecuteEventArgs> BeforeExecuteCommand;

		protected void DoBeforeExecuteCommand(DbCommand command)
		{
			if (BeforeExecuteCommand != null)
			{
				var args = new CommandExecuteEventArgs(command);
				BeforeExecuteCommand(this, args);

				if (args.Cancel)
					throw new OperationCanceledException("Command Execution Canceled");
			}
		}


		/// <summary>
		/// Fire before a Save(), Insert() or Update() is committed to the database
		/// </summary>
		public event EventHandler<BeforeSaveEventArgs> BeforeSave;

		protected void DoBeforeSave(SaveType saveType, object obj)
		{
			if (BeforeSave == null) return;
			if (! (obj is ISavable)) return;

			var args = new BeforeSaveEventArgs
			{
				SaveType = saveType,
				DataMap = DataMap,
				DataObject = (ISavable)obj
			};
			BeforeSave(this, args);

			if (args.Cancel)
				throw new OperationCanceledException("Command Execution Canceled");
		}

		#endregion

	}
}