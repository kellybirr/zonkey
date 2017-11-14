using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;

namespace ZonkeyCodeGen.Utilities
{
	/// <summary>
	/// This class provides methods for logging debug output from the application, based on the set Debug Level
	/// </summary>
	static class DebugLog
	{
		private static object _writeLockObj = new object();
		private static object _exceptLockObj = new object();

        /// <summary>
        /// Returns the true windows threadID for the current managed thread, 
        /// this is more helpful when debugging services than a managed threadID
        /// </summary>
        /// <returns></returns>
        [DllImport("kernel32.dll", EntryPoint = "GetCurrentThreadId")]
        private static extern int GetOSThreadId();

		/// <summary>
		/// Write an exception to the debug log with great detail.
		/// </summary>
		/// <param name="exceptionToLog">The exception to log</param>
		public static void WriteException(Exception exceptionToLog)
		{
			lock ( _exceptLockObj )
			{
				WriteLine(DebugLevel.Error, "EXCEPTION INFO: TYPE={0}", exceptionToLog.GetType().FullName);

				Exception exInner = exceptionToLog;
				while (exInner != null)
				{
					WriteLine(DebugLevel.Error, "\t SRC={0}\t MSG={1}", exInner.Source, exInner.Message);	
					exInner = exInner.InnerException;
				}

				WriteLine(DebugLevel.Error, "\t STACK TRACE: {0}", exceptionToLog.StackTrace.Replace("\r", "").Replace("\n", " | "));
			}
		}

		/// <summary>
		/// Writes output to the application debug log
		/// </summary>
		/// <param name="messageLevel">The debug level that this message reports at</param>
		/// <param name="format">The format of the message to be written [see String.Format()]</param>
		/// <param name="args">The arguments for the message to be written [see String.Format()]</param>
		[System.Diagnostics.DebuggerStepThrough()]
		public static void WriteLine(DebugLevel messageLevel, string format, params object[] args)
		{
			WriteLine(messageLevel, string.Format(format, args));
		}

		/// <summary>
		/// Writes output to the application debug log
		/// </summary>
		/// <param name="messageLevel">The debug level that this message reports at</param>
		/// <param name="message">The exact text of the message to be written</param>
		[System.Diagnostics.DebuggerStepThrough()]
		public static void WriteLine(DebugLevel messageLevel, string message)
		{
			// exit if not debugging at this level
			if (AppConfig.DebugLevel == DebugLevel.None) return;
			if (messageLevel > AppConfig.DebugLevel) return;
			
			lock ( _writeLockObj )
			{
				StreamWriter swOut = null;
				try
				{
					// write output to file
					swOut = new StreamWriter(DebugFilePath, true, Encoding.ASCII);
					
					string szOutputLine = DateTime.Now.ToString("MM/dd/yyyy hh:mm:ss").PadRight(21) 
						+ GetOSThreadId().ToString().PadRight(7) + message;			

					System.Diagnostics.Debug.WriteLine(szOutputLine);
					swOut.WriteLine(szOutputLine);
					
					swOut.Flush();
				}
				catch (Exception)
				{ 
					// Ignore exceptions here. What would we do with it?
				}
				finally
				{
					if (swOut != null)
						swOut.Close();
				}
			}
		}

        /// <summary>
        /// Gets the debug file path.
        /// </summary>
        /// <value>The debug file path.</value>
        public static string DebugFilePath
        {
            get 
            { 
                if (_debugFilePath == null)
                {
                    // Get Path to current assembly
                    string sAppPath = typeof(DebugLog).Assembly.Location;
                    sAppPath = Path.GetDirectoryName(sAppPath);

                    _debugFilePath = Path.Combine(sAppPath, "debug.log");
                }

                return _debugFilePath; 
            }
        }
        private static string _debugFilePath;
	}

	/// <summary>
	/// The debugging level that messages reports at, each level build upon the previous
	/// </summary>
	public enum DebugLevel
	{
		/// <summary>
		/// No Debug Logging
		/// </summary>
		None		= 0,

		/// <summary>
		/// Log Errors Only
		/// </summary>
		Error		= 1,

		/// <summary>
		/// Log Errors and Warnings
		/// </summary>
		Warning		= 2,

		/// <summary>
		/// Log Errors, Warnings and Info Messages
		/// </summary>
		Info		= 3,

		/// <summary>
		/// Log everything, too much information.
		/// </summary>
		Verbose		= 4
	}
}
