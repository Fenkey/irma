using System;
using System.IO;
using System.Text;
using IRMAKit.Utils;
using IRMAKit.Log;
using IRMAKit.Web;

namespace ${appName}.Web
{
	/// <summary>
	/// Static资源的响应涉及到几个地方：
	/// 1、最好考虑304浏览器缓存支持、以及服务端mc缓存（本案例在此忽略）
	/// 2、自由访问资源、受控访问资源（例如要求登录），后者可参考CStaticHandler
	/// 3、自由访问资源，除应用层管理外，在实际部署中也可考虑交给nginx配置管理
	/// </summary>
	public class StaticHandler : IHandler
	{
		private ITime t;
		protected long expires;
		private long lastModified;
		private string lastModifiedStr;

		public StaticHandler()
		{
			this.t = new Time();
			this.expires = 600;
			long lt = this.t.BuildUnixTime(2018, 11, 2, 20, 34, 30);
			this.lastModified = this.t.UnixTimeToGmTime(lt, ref this.lastModifiedStr);
		}

		private byte[] ReadFile(string os, string file)
		{
			if (!os.Equals("linux"))
				file = file.Replace("/", "\\");
			using (FileStream fs = new FileStream(file, FileMode.Open, FileAccess.Read)) {
				byte[] bytes = new byte[fs.Length];
				fs.Read(bytes, 0, bytes.Length);
				return bytes;
			}
		}

		private void EchoResource(IContext context, string contentType, string file, bool zip=false)
		{
			IRequest req = context.Request;
			IResponse res = context.Response;

			long ex = expires;
			if (req.IsRef && req.RefParam != null) {
				ex = (long)req.RefParam;
				Logger.DEBUG("From ref and using the param as expires: " +  ex);
			}

			// for HTTP 1.0
			string expiresStr = null;
			t.UnixTimeToGmTime(t.GetUnixTime() + ex, ref expiresStr);
			res.Expires = expiresStr;

			// for HTTP 1.1
			res.CacheControl = "public,max-age=" + ex;

			res.LastModified = lastModifiedStr;
			res.ContentType = contentType;
			res.GZipEnable = zip;
			res.Echo(ReadFile(context.OS, file));
		}

		private bool IsModified(IContext context)
		{
			long lt = t.BuildGmTime(context.Request.IfModifiedSince);
			if (lt > lastModified) {
				Logger.ERROR("Invalid client Last-Modified");
				return false;
			} else if (lt < lastModified)
				return true;
			Logger.DEBUG("Static resource cached in client is still valid and un-expired");
			return false;
		}

		public void Do(IContext context)
		{
			if (!IsModified(context)) {
				context.Response.NotModified();
				return;
			}

			// 可在此考虑优先从mc内获取缓存资源...

			// docPath请根据实际应用情况调整。例如Windows平台Mock方式时，AppPath可能为Mock目录路径
			string docPath = context.AppPath + "/../../documents";

			// such as: '/s/n/1.html'
			string fileName = context.Request.AppLocation.Substring(3);

			try {
				if (fileName.EndsWith(".html"))
					EchoResource(context, "text/html", docPath + "/html/" + fileName, true);
				else if (fileName.EndsWith(".js"))
					EchoResource(context, "application/javascript", docPath + "/js/" + fileName, true);
				else if (fileName.EndsWith(".css"))
					EchoResource(context, "text/css", docPath + "/css/" + fileName, true);
				else if (fileName.EndsWith(".jpeg"))
					EchoResource(context, "image/jpeg", docPath + "/image/" + fileName);
				else if (fileName.EndsWith(".png"))
					EchoResource(context, "image/png", docPath + "/image/" + fileName);
				else if (fileName.EndsWith(".gif"))
					EchoResource(context, "image/gif", docPath + "/image/" + fileName);
				else if (fileName.EndsWith(".ico"))
					EchoResource(context, "image/x-icon", docPath + "/image/" + fileName);
				else
					context.Response.NotFound();
			} catch {
				context.Response.NotFound();
			}
		}
	}
}
