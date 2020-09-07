using System;
using IRMAKit.Web;

namespace ${appName}.Web
{
	public class ExceptionHandler : IHandler
	{
		public void Do(IContext context)
		{
			throw new Exception("My wrong :-(");
		}
	}
}
