using System;
using System.Threading;
using System.Collections.Generic;
using IRMAKit.Log;
using IRMAKit.Web;

namespace ${appName}.Web
{
	public class MyEndAttribute : ReqEndAttribute
    {
        protected override void End(IContext context)
        {
			int delay = int.Parse(context.Request.GetParams.Get("delay"));
			Thread.Sleep(delay * 1000);
			Logger.DEBUG("my end: you can do what after your respons here.");
        }
    }

	[MyEnd]
	[ParamsCheck(GetMust="delay")]
	public class NoBlockHandler : IHandler
	{
		public void Do(IContext context)
		{
			context.Response.Echo("ok");
			Logger.DEBUG("Noblock handle success.");
		}
	}
}
