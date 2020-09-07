using System;
using IRMAKit.Log;
using IRMAKit.Store;
using IRMAKit.Web;

namespace ${appName}.Web
{
	[Serializable]
	internal class UserInfo
	{
		private string name;
		public string Name { get { return this.name; } }

		private string ssldn;
		public string SSLDN { get { return this.ssldn; } }

		private DateTime loginTime;
		public DateTime LoginTime { get { return this.loginTime; } }

		public UserInfo(string name, string ssldn=null)
		{
			this.name = name;
			this.ssldn = ssldn;
			this.loginTime = DateTime.Now;
		}
	}


	public class LoginHandler : IHandler
	{
		public void Do(IContext context)
		{
			IRequest req = context.Request;
			IResponse res = context.Response;

			UserInfo ui;
			try {
				ui = context.Session["userinfo"] as UserInfo;
				if (ui != null) {
					res.Echo("Welcome {0}, you have logined at {1}", ui.Name, ui.LoginTime);
					return;
				}
			} catch (SessionKickedOutException e) {
				// Do nothing.
			}

			if (req.Https) {
				res.Echo("Insecurity login is not allowed !");
				return;
			}

			string name = req.GetParams.Get("name");
			if (!string.IsNullOrEmpty(name)) {
				ui = new UserInfo(name);
				context.Session["userinfo"] = ui;
				res.Echo("Welcome " + name + ", it's your first login !");
				// You can invoke AttachSid() to make sure the session is sole at present moment.
				context.Session.AttachSid(name, "userinfo");
				return;
			}

			res.Echo("Sorry, invalid request: param of 'name' is missing !");
		}
	}
}
