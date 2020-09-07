using System;
using System.Text;
using IRMAKit.Log;
using IRMAKit.Web;

namespace ${appName}.Web
{
	[LocalApi]
	public class LocalApiHandler : IHandler
	{
		public void Do(IContext context)
		{
			context.Response.Echo("This's a local api !");
			Logger.DEBUG("LocalApi handle success.");
		}
	}
}
