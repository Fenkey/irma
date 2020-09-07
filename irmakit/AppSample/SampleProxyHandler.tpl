using System;
using System.Text;
using System.Collections.Generic;
using IRMAKit.Log;
using IRMAKit.Utils;
using IRMAKit.Web;

namespace ${appName}.Web
{
	public class ProxyHandler : IHandler
	{
		private IFetcher fetcher = new Fetcher();

		public void Do(IContext context)
		{
			IRequest req = context.Request;
			IResponse res = context.Response;

			// 如果在Windows平台下Mock访问，不建议对网站自身作proxy，因为Mock方式下为单线程lock
			string url = "http://" + req.Host + "/" + context.Config.AppName + "/request_params";
			int code = res.SendProxy(fetcher, url, req, "proxy test");

			Logger.DEBUG("Proxy handle success. response code: " + code);
		}
	}
}
