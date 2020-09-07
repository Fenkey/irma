using System;
using IRMAKit.Web;

namespace ${appName}.Web
{
	[FuseCheck]
	public class FuseCheckHandler : IHandler
	{
		public void Do(IContext context)
		{
			context.Response.Echo("I'm working !");
			throw new Exception("My wrong :-(");
		}
	}
}
