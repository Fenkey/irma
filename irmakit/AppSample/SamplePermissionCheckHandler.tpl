using System;
using IRMAKit.Log;
using IRMAKit.Web;

namespace ${appName}.Web
{
	[OnePermissionCheck]
	[TwoPermissionCheck(Owner="Fenkey")]
	public class PermissionCheckHandler : IHandler
	{
		public void Do(IContext context)
		{
			context.Response.Echo("Permission check ok");
			Logger.DEBUG("Permission check handle success.");
		}
	}
}
