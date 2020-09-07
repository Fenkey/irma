using System;
using IRMAKit.Log;
using IRMAKit.Web;

namespace ${appName}.Web
{
	[Ref(
	"/index.html -> /s/n/index.html" +
	", /index -> /s/n/index.html" +
	", / -> /s/n/index.html"
	, (long)60)]
	public class IndexHandler : IHandler
	{
		public void Do(IContext context)
		{
		}
	}
}
