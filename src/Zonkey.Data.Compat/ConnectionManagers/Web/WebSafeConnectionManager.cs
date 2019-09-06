using System;
using Zonkey.ConnectionManagers.Specialized;

#if (NETSTANDARD2_0)
using Microsoft.AspNetCore.Http;
#else
using System.Web;
#endif

namespace Zonkey.ConnectionManagers.Web
{
	/// <summary>
	/// A Thread-safe and Web-Safe connection manager
	/// </summary>
	public class WebSafeConnectionManager: BaseConnectionManager
	{
		[ThreadStatic]
		private static ConnectionManagerContext _threadContext;

		private const string CTX_ITEM_NAME = "Zonkey.ConnectionManager.Context";

#if (! NETSTANDARD2_0)
		public WebSafeConnectionManager()
		{
			ContextAccessor = () => HttpContext.Current;
		}
#endif

		public Func<HttpContext> ContextAccessor { get; set; }

		protected override ConnectionManagerContext Context
		{
			get
			{
				HttpContext http = ContextAccessor?.Invoke();
				if (http == null)
					return (_threadContext ?? (_threadContext = new ConnectionManagerContext()));

				var ctx = http.Items[CTX_ITEM_NAME] as ConnectionManagerContext;
				if (ctx == null)
				{
					ctx = new ConnectionManagerContext();
					http.Items[CTX_ITEM_NAME] = ctx;
				}

				return ctx;
			}
		}

		protected override void OnPrepareConnection()
		{
			// check if transaction needed
			HttpContext http = ContextAccessor?.Invoke();
			if (http != null)
			{
				switch ((string)http.Request.Headers["X-Zonkey-Transaction"])
				{
					case "required":
					case "auto-rollback":
						GetTransaction();
						break;
				}
			}
		}
	}
}
