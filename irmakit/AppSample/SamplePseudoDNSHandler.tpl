using System;
using IRMAKit.Log;
using IRMAKit.Utils;
using IRMAKit.Web;

namespace ${appName}.Web
{
	public class PseudoDNSHandler : IHandler
	{
		private IDrum drum;

		public PseudoDNSHandler()
		{
			drum = new Drum();

			string key = "www.beyhui.com";
			drum.Add(key, "192.168.3.10");
			drum.Add(key, "192.168.3.11");
			drum.Add(key, "192.168.3.12");
			drum.Add(key, "192.168.3.13");
			drum.Add(key, "192.168.3.14");
			drum.Add(key, "192.168.3.15");

			key = "www.beyhui.cn";
			drum.Add(key, "192.168.5.98");
			drum.Add(key, "192.168.5.99");
			drum.Add(key, "192.168.5.100");
		}

		public void Do(IContext context)
		{
			string key = context.Request.Params.Get("key");
			if (key == null)
				key = "www.beyhui.com";
			context.Response.Echo(key + ": " + (string)drum[key] + " (RoundEnd: " + drum.RoundEnd(key) + ")");

			Logger.DEBUG("PseudoDNS handle success.");
		}
	}
}
