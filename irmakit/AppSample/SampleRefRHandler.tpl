using System;
using IRMAKit.Web;
using IRMAKit.Log;

namespace ${appName}.Web
{
	[OnePermissionCheck]
	[Ref("/refr/restapi/->/restapi/->AAA, /refr/jsonapi->/jsonapi", RefMode.REPLACE)]
	public class RefRHandler : IHandler
	{
		public void Do(IContext context)
		{
			context.Response.Echo("RefR normal doing here ...");
            Logger.DEBUG("RefR handle success.");
		}
	}
}
