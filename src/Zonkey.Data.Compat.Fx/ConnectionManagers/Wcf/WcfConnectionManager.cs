using System;
using System.Collections.Generic;
using System.ServiceModel;
using Zonkey.ConnectionManagers.Specialized;

namespace Zonkey.ConnectionManagers.Wcf
{
	/// <summary>
	/// A Thread-safe connection manager for worker processes (not web-safe)
	/// </summary>
	public class WcfConnectionManager: BaseConnectionManager
	{
		[ThreadStatic]
		private static ConnectionManagerContext _threadContext;

		private static readonly Dictionary<OperationContext, ConnectionManagerContext> _opLookup =
			new Dictionary<OperationContext, ConnectionManagerContext>();

		protected override ConnectionManagerContext Context
		{
			get
			{
				var opCtx = OperationContext.Current;
				if (opCtx == null)
					return (_threadContext ?? (_threadContext = new ConnectionManagerContext()));

				ConnectionManagerContext ctx;
				if (! _opLookup.TryGetValue(opCtx, out ctx))
				{
					ctx = new ConnectionManagerContext();
					_opLookup.Add(opCtx, ctx);

					opCtx.OperationCompleted += opCtx_OperationCompleted;
				}

				return ctx;
			}
		}

		private static void opCtx_OperationCompleted(object sender, EventArgs e)
		{
			var op = (OperationContext) sender;
			if (!_opLookup.ContainsKey(op)) return;

			_opLookup[op].CloseConnections();
			_opLookup.Remove(op);
		}
	}
}
