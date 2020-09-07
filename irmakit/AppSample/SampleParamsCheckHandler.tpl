using System;
using System.Collections.Generic;
using IRMAKit.Log;
using IRMAKit.Web;

namespace ${appName}.Web
{
	[ParamsCheck(GetMust="name,age,nation")]
	public class ParamsCheckHandler : IHandler
	{
		public void Do(IContext context)
		{
			IRequest req = context.Request;
			string name = req.GetParams.Get("name");
			string age = req.GetParams.Get("age");
			string nation = req.GetParams.Get("nation");

			context.Response.Echo("name: {0}<br/>age: {1}<br/>nation: {2}", name, age, nation);

			Logger.DEBUG("ParamsCheck handle success.");
		}
	}
}
