using System;
using IRMAKit.Web;
using IRMAKit.Log;

namespace ${appName}.Web
{
	[Proxy("/auto_proxy_r/restapi/->/restapi/->_, /auto_proxy_r/jsonapi->/jsonapi->_", ProxyMode.REPLACE)]
	public class AutoProxyRHandler : IHandler
	{
		public void Do(IContext context)
		{
			context.Response.Echo("AutoProxyR normal doing here ...");
            Logger.DEBUG("AutoProxyR handle success.");
		}
	}
}
