using System;

namespace Zonkey.ConnectionManagers.Specialized
{
	/// <summary>
	/// A Thread-safe connection manager for worker processes (not web-safe)
	/// </summary>
	public class ThreadedConnectionManager: BaseConnectionManager
	{
		[ThreadStatic]
		private static ConnectionManagerContext _context;

		protected override ConnectionManagerContext Context
		{
			get { return (_context ?? (_context = new ConnectionManagerContext())); }
		}
	}
}
