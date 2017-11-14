using System;
using System.Data;
using System.Data.Common;
using Zonkey.ObjectModel;

namespace Zonkey
{
	/// <summary>
	/// Event Args passed to FillProgress and FillAsyncComplete events.
	/// </summary>
	public class FillStatusEventArgs : System.EventArgs
	{
		/// <summary>
		/// Standard Constructor
		/// </summary>
		/// <param name="count">the current record count</param>
		/// <param name="complete">is the process complete</param>
		public FillStatusEventArgs(int count, bool complete)
		{
			RecordCount = count;
			IsComplete = complete;
		}

		/// <summary>
		/// The number of records in the collection at the time the event was fired
		/// </summary>
		public int RecordCount { get; private set; }

		/// <summary>
		/// True if the fill operation has completed, otherwise False.
		/// Will always be true for a FillAsyncComplete event.
		/// </summary>
		public bool IsComplete { get; private set; }

		/// <summary>
		/// Set this value to True to cancel the current fill operation.
		/// This parameter is only used for FillProgress events. 
		/// It has no effect in a FillAsyncComplete event.
		/// </summary>
		public bool Cancel { get; set; }
	} 

	/// <summary>
	/// EventArgs passed to BeforeExecuteCommand Events
	/// </summary>
	public class CommandExecuteEventArgs : EventArgs
	{
		/// <summary>
		/// Default Constructor
		/// </summary>
		/// <param name="command">The command to be executed</param>
		public CommandExecuteEventArgs(DbCommand command)
		{
			Command = command;
		}

		/// <summary>
		/// The command to be executed
		/// </summary>
		public DbCommand Command { get; private set; }

		/// <summary>
		/// Set this value to true to cancel the execution of the command
		/// This will cause a OperationCanceledException to be thrown in the DataClassAdapter method.
		/// </summary>
		public bool Cancel { get; set; }
	}

	/// <summary>
	/// Event Args passed to BeforeSave Event
	/// </summary>
	public class BeforeSaveEventArgs : EventArgs
	{
		/// <summary>
		/// Gets or sets the type of the save.
		/// </summary>
		/// <value>The type of the save.</value>
		public SaveType SaveType { get; set; }

		/// <summary>
		/// Gets or sets the data object.
		/// </summary>
		/// <value>The data object.</value>
		public ISavable DataObject { get; set; }

		/// <summary>
		/// Gets or sets the data map.
		/// </summary>
		/// <value>The data map.</value>
		public DataMap DataMap { get; set; }

		/// <summary>
		/// Gets or sets a value indicating whether this <see cref="BeforeSaveEventArgs"/> is cancel.
		/// </summary>
		/// <value><c>true</c> if cancel; otherwise, <c>false</c>.</value>
		public bool Cancel { get; set; }
	}

}