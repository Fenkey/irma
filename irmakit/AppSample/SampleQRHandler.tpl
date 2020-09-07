using System;
using System.Text;
using IRMAKit.Log;
using IRMAKit.Utils;
using IRMAKit.Web;

namespace ${appName}.Web
{
	public class QRHandler : IHandler
	{
		public void Do(IContext context)
		{
			IQRCoder qrCoder = (QRCoder)context["qrcoder"];
			byte[] bytes = qrCoder.GenQR("http://www.baidu.com", 200, 200, QRType.PNG);
			context.Response.ContentType = "image/png";
			context.Response.Echo(bytes);
			bytes = null;

			Logger.DEBUG("QR handle success.");
		}
	}
}
