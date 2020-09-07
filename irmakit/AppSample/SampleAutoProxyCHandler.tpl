using System;
using IRMAKit.Web;
using IRMAKit.Log;

namespace ${appName}.Web
{
	[Proxy(
	"/auto_proxy_c/rq -> /request_params -> 0" +
	", /auto_proxy_c/jsonapi -> /jsonapi -> 1" +
	", /auto_proxy_c/testcase -> /testcase -> _"
	)]
	public class AutoProxyCHandler : IHandler
	{
		public void Do(IContext context)
		{
			context.Response.Echo("AutoProxyC normal doing here ...");
            Logger.DEBUG("AutoProxyC handle success.");
		}
	}
}
