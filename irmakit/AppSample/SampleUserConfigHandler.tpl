using System;
using System.Text;
using Newtonsoft.Json.Linq;
using IRMAKit.Utils;
using IRMAKit.Log;
using IRMAKit.Web;

namespace ${appName}.Web
{
	public class UserConfigHandler : IHandler
	{
		public void Do(IContext context)
		{
			StringBuilder sb = new StringBuilder();
			IUnit unit = (Unit)context["unit"];
			JObject u = context.Config.User.Value<JObject>("unit");

			long number = u.Value<long>("number");
			sb.Append(string.Format("number={0}<br/><br/>", number));

			JArray size = u.Value<JArray>("size");
			for (int i = 0; i < size.Count; i++) {
				long s = -1;
				try {
					s = (long)size[i];
				} catch {
					s = unit.ParseBytes((string)size[i]);
				}
				sb.Append(string.Format("size[{0}]={1} (<span style=\"color:BLUE\">bytes</span>)<br/>", i, s));
			}
			sb.Append("<br/>");
			
			JArray time = u.Value<JArray>("time");
			for (int i = 0; i < time.Count; i++) {
				long t = -1;
				try {
					t = (long)time[i];
				} catch {
					t = unit.ParseSeconds((string)time[i]);
				}
				sb.Append(string.Format("time[{0}]={1} (<span style=\"color:BLUE\">seconds</span>)<br/>", i, t));
			}

			context.Response.Echo(sb.ToString());

			Logger.DEBUG("User config handle success.");
		}
	}
}
