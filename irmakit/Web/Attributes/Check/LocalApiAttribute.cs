using System;
using IRMAKit.Log;

namespace IRMAKit.Web
{
	[AttributeUsage(AttributeTargets.Class, Inherited=true, AllowMultiple=true)]
	public class LocalApiAttribute : ReqCheckAttribute
	{
		private string serverAddr = "127.0.0.1";
		public string ServerAddr
		{
			set { this.serverAddr = value; }
			get { return this.serverAddr; }
		}

		private string remoteAddr = "127.0.0.1";
		public string RemoteAddr
		{
			set { this.remoteAddr = value; }
			get { return this.remoteAddr; }
		}

		protected override bool Check(IContext context)
		{
			IRequest req = context.Request;
			return req.ServerAddr == serverAddr && req.RemoteAddr == remoteAddr;
		}
	}
}
