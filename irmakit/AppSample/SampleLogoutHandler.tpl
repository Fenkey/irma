using System;
using IRMAKit.Log;
using IRMAKit.Web;

namespace ${appName}.Web
{
	public class LogoutHandler : IHandler
	{
		public void Do(IContext context)
		{
			context.Session.Remove("userinfo");
			// Optional 
			context.RemoveSessionCookie();
			context.Response.Echo("Logout success !");
		}
	}
}
