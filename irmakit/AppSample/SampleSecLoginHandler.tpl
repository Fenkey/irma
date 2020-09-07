using System;
using IRMAKit.Log;
using IRMAKit.Store;
using IRMAKit.Web;

namespace ${appName}.Web
{
	public class SecLoginHandler : IHandler
	{
		public void Do(IContext context)
		{
			IRequest req = context.Request;
			IResponse res = context.Response;

			if (string.IsNullOrEmpty(req.SSLDN)) {
				res.Echo("Insecurity access !");
				return;
			}

			UserInfo ui = context.Session["userinfo"] as UserInfo;
			if (ui != null) {
				if (ui.SSLDN != null && req.SSLDN.Equals(ui.SSLDN))
					res.Echo("Welcome {0}, you have logined at {1}", ui.Name, ui.LoginTime);
				else
					res.Echo("Insecurity access(such as the fake cookies), login again please !");
				return;
			}

			string name = req.GetParams.Get("name");
			if (!string.IsNullOrEmpty(name)) {
				ui = new UserInfo(name, req.SSLDN);
				context.Session["userinfo"] = ui;
				res.Echo("Welcome " + name + ", it's your first login !");
				return;
			}

			res.Echo("Sorry, invalid request: param of 'name' is missing !");
		}
	}
}
