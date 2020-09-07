using System;
using IRMAKit.Web;
using IRMAKit.Log;

namespace ${appName}.Web
{
	[OnePermissionCheck]
	[Ref(@"/refx/(.*) -> /{0} -> BBB", RefMode.REGULAR)]
	public class RefXHandler : IHandler
	{
		public void Do(IContext context)
		{
			context.Response.Echo("RefX normal doing here ...");
            Logger.DEBUG("RefX handle success.");
		}
	}
}
