using System;
using IRMAKit.Log;
using IRMAKit.Web;

namespace ${appName}.Web
{
	[OnePermissionCheck(OrderNum=0)]
	public class BaseHandler : IHandler
	{
		public virtual void Do(IContext context)
		{
		}
	}

	[TwoPermissionCheck(Owner="Fenkey")]
	[ThreePermissionCheck]
	public class PermissionCheckHandler2 : BaseHandler
	{
		public override void Do(IContext context)
		{
			context.Response.Echo("Permission check 2 ok");
			Logger.DEBUG("Permission check handle2 success.");
		}
	}
}
