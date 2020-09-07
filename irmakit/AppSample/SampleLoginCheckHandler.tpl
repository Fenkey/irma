using System;
using IRMAKit.Log;
using IRMAKit.Web;

namespace ${appName}.Web
{
	[LoginCheck]
	public class LoginCheckHandler : IHandler
	{
		public void Do(IContext context)
		{
			UserInfo ui = context.Session["userinfo"] as UserInfo;
			context.Response.Echo("Welcome {0} !", new object[] { ui.Name });

			Logger.DEBUG("Login check handle success.");
		}
	}
}
