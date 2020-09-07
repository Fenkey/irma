using System;
using System.Collections.Generic;
using IRMAKit.Log;
using IRMAKit.Web;

namespace ${appName}.Web
{
	[RestApi(Pattern=@"/[^\/]+/(\w+)/(\d+)", Params="name,age")]
	public class RestApiBaseHandler : IHandler
	{
		protected string name;
		protected int age;

		private void Todo(IContext context)
		{
			this.name = (string)context.Request.RestParams.Get("name");
			this.age = int.Parse(context.Request.RestParams.Get("age"));
			Logger.DEBUG("Todo() finished.");
		}

		protected virtual void Doing(IContext context)
		{
			Logger.DEBUG("Doing() finished.");
		}

		private void Done(IContext context)
		{
			Logger.DEBUG("Done() finished.");
		}

		public void Do(IContext context)
		{
			Todo(context);
			Doing(context);
			Done(context);
			Logger.DEBUG("Rest api handle success.");
		}
	}

	public class RestApiHandler : RestApiBaseHandler
	{
		protected override void Doing(IContext context)
		{
			IRequest req = context.Request;
			IResponse res = context.Response;

			res.BufferAppend(string.Format("name: {0}<br/>age: {1}<br/>nation: {2}<br/>unixtime: {3}", name, age+1, "China", context.UnixTime));
			if (req.IsRef)
				res.BufferAppend(string.Format("<br/><br/>IsRef: {0}<br/>OriAppLocation: {1}<br/>AppLocation: {2}<br/>RefParam: {3}<br/>RefToParam: {4}", req.IsRef, req.OriAppLocation, req.AppLocation, req.RefParam, req.RefToParam));
			res.Echo();

			Logger.DEBUG("Doing() finished.");
		}
	}
}
