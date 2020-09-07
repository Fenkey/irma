using System;

namespace IRMAKit.Web
{
	internal class DispatcherInitFailException : Exception
	{
		public DispatcherInitFailException() : base("Dispatcher init failed") {}

		public DispatcherInitFailException(string err) : base("Dispatcher init failed: " + err) {}
	}

	internal class RefHopOutException : Exception
	{
		public RefHopOutException() : base("Too many times Ref") {}

		public RefHopOutException(string err) : base("Too many times Ref: " + err) {}
	}

	internal class ProxyHopOutException : Exception
	{
		public ProxyHopOutException() : base("Too many times Proxy") {}

		public ProxyHopOutException(string err) : base("Too many times Proxy: " + err) {}
	}

	internal class AccessNotFoundException : Exception
	{
		public AccessNotFoundException() : base("Access not found") {}

		public AccessNotFoundException(string location) : base("Access not found: " + location) {}
	}

	internal class MethodNotAllowedException : Exception
	{
		private string method;
		public string Method { get { return this.method; } }

		private string mAllow;
		public string MAllow { get { return this.mAllow; } }

		public MethodNotAllowedException(string method, string mAllow) : base("Method is not allowd: " + method)
		{
			this.method = method;
			this.mAllow = mAllow;
		}
	}

	internal class GoToProxyException : Exception
	{
		private string location;
		public string Location { get { return this.location; } }

		private string param;
		public string Param { get { return this.param; } }

		public GoToProxyException(string location, string param)
		{
			this.location = location;
			this.param = param;
		}
	}

	internal class BodyIsEmptyException : Exception
	{
		private string method;
		public string Method { get { return this.method; } }

		public BodyIsEmptyException(string method) : base("Request body is empty via method: " + method)
		{
			this.method = method;
		}
	}

	internal interface IDispatcher
	{
		/// <summary>
		/// Dispatch
		/// </summary>
		void Dispatch(Context context);
	}
}
