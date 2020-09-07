using System;
using IRMAKit.Log;
using IRMAKit.Web;

namespace ${appName}.Web
{
	public class LoginCheckAttribute : ReqCheckAttribute
	{
		protected override bool Check(IContext context)
		{
			try {
				UserInfo ui = context.Session["userinfo"] as UserInfo;
				if (ui == null) {
					context.Response.Echo("Sorry, you have to login first !");
					return false;
				}

				if (context.Request.Https && ui.SSLDN == null) {
					context.Response.Echo("You maybe logined site without SSL. Logout it and login again pls !");
					return false;
				}
			} catch (SessionKickedOutException e) {
				context.Response.Echo("Unfortunately ! your current login session have been kicked out by others at: {0}", new object[] { e.KickedTime });
				return false;
			}

			return true;
		}
	}
}
