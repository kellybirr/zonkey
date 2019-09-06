using System;

namespace Zonkey.ConnectionManagers.Specialized
{
	/// <summary>
	/// A Single instance connection manager that is not thread safe or web safe,
	/// usually used for unit testing projects
	/// </summary>
	public class SingleConnectionManager : BaseConnectionManager
	{
		private readonly Lazy<ConnectionManagerContext> _context =
			new Lazy<ConnectionManagerContext>( () => new ConnectionManagerContext() );

		protected override ConnectionManagerContext Context
		{
			get { return _context.Value; }
		}
	}
}
