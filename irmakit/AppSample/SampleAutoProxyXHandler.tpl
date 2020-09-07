using System;
using IRMAKit.Web;
using IRMAKit.Log;

namespace ${appName}.Web
{
	[Proxy(@"/auto_proxy_x/(.*) -> /{0} -> _", ProxyMode.REGULAR)]
	public class AutoProxyXHandler : IHandler
	{
		public void Do(IContext context)
		{
			context.Response.Echo("AutoProxyX normal doing here ...");
            Logger.DEBUG("AutoProxyX handle success.");
		}
	}
}
