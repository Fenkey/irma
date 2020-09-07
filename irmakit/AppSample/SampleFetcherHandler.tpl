using System;
using System.Text;
using System.Collections.Generic;
using IRMAKit.Log;
using IRMAKit.Utils;
using IRMAKit.Web;

namespace ${appName}.Web
{
	public class FetcherHandler : IHandler
	{
		public void Do(IContext context)
		{
			IRequest req = context.Request;
			IResponse res = context.Response;

			// 如果在Windows平台下Mock访问，不建议对网站自身作fetcher，因为Mock方式下为单线程lock
			string url = "http://" + req.Host + "/" + context.Config.AppName + "/request_params?name=Tom&age=100&nation=USA&favorite=dog&kid=4";

			Dictionary<string, string> headers = new Dictionary<string, string>() {
				{"X-FOO", "foo"},
				{"Content-Type", "application/x-www-form-urlencoded"}
			};

			/*
			string bodyStr = "author=Fenkey&year=2020";
			byte[] body = Encoding.Default.GetBytes(bodyStr);
			*/
			Dictionary<string, string> postParams = new Dictionary<string, string>() {
				{"author", "Fenkey"},
				{"year", "2020"}
			};

			IFetcher fetcher = (Fetcher)context["fetcher"];
			int code = fetcher.Post(url, headers, postParams);

			res.BufferAppend("Fetcher time used: " + fetcher.TimeUsed + "<br/>");
			res.BufferAppend("<br/>Fetcher response headers:<br/>");
			foreach (KeyValuePair<string, string> h in fetcher.ResHeaders)
				res.BufferAppend(h.Key + "=" + h.Value + "<br/>");
			res.BufferAppend("<br/>" + fetcher.ResText);

			res.Echo();

			Logger.DEBUG("Fetcher handle success. fetcher response code: " + code);
		}
	}
}
