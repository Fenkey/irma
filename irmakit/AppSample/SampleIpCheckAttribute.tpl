using System;
using IRMAKit.Utils;
using IRMAKit.Log;
using IRMAKit.Web;

namespace ${appName}.Web
{
	public class IpCheckAttribute : ReqCheckAttribute
	{
		protected override bool Check(IContext context)
		{
			string ip = context.Request.RemoteAddr;
			ICidr cidr = (Cidr)context["ip_whitelist"];
			if (!cidr.Hit(ip)) {
				Logger.WARN("Ip check failed: " + ip);
				return false;
			}
			Logger.DEBUG("Ip check successful: " + ip);
			return true;
		}
	}
}
