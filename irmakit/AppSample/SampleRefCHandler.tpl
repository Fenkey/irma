using System;
using IRMAKit.Web;
using IRMAKit.Log;

namespace ${appName}.Web
{
	[OnePermissionCheck]
	[Ref(
	"/refc/rq -> /request_params -> 999" +
	", /refc/jsonapi -> /jsonapi" +
	", /refc/testcase -> /testcase"
	, Param="The ref common param")]
	public class RefCHandler : IHandler
	{
		public void Do(IContext context)
		{
			context.Response.Echo("RefC normal doing here ...");
            Logger.DEBUG("RefC handle success.");
		}
	}
}
