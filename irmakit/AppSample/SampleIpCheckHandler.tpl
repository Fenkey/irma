using System;
using IRMAKit.Log;
using IRMAKit.Web;

namespace ${appName}.Web
{
	[IpCheck]
	public class IpCheckHandler : IHandler
	{
		public void Do(IContext context)
		{
			context.Response.Echo("Welcome " + context.Request.RemoteAddr + " !");
			Logger.DEBUG("Ip check handle success.");
		}
	}
}
