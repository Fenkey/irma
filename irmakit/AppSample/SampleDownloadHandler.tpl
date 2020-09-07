using System;
using System.Text;
using IRMAKit.Log;
using IRMAKit.Web;

namespace ${appName}.Web
{
	public class DownloadHandler : IHandler
	{
		public void Do(IContext context)
		{
			IResponse res = context.Response;

			string confFile = context.AppPath + "/../../conf/${appName}.conf";
			res.ContentType = "text/plain";
			res.Download(confFile);

			Logger.DEBUG("Download handle success.");
		}
	}
}
