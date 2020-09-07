using System;
using System.Text;
using IRMAKit.Log;
using IRMAKit.Web;

namespace ${appName}.Web
{
	[CrossOriginAllow]
	public class CrossOriginHandler : IHandler
	{
		public void Do(IContext context)
		{
			context.Response.Echo("Cross-Origin access is ok");
			Logger.DEBUG("Cross origin handle success.");
		}
	}
}
