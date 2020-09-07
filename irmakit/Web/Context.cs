using System;
using System.IO;
using System.Web;
using System.Text;
using System.Collections.Generic;
using System.Runtime.Serialization.Formatters.Binary;
using IRMACore.Net;
using IRMACore.Lower;
using IRMAKit.Configure;

namespace IRMAKit.Web
{
	internal sealed class Context : IContext
	{
		private Dictionary<string, object> flashMemory;

		private Dictionary<string, object> global;

		private string os;
		public string OS
		{
			get {
				if (this.os == null)
					this.os = ICall.GetOS();
				return this.os;
			}
		}

		public bool IsW0
		{
			get { return ICall.GetWorkerIndex() == 0; }
		}

		private string appPath;
		public string AppPath
		{
			get {
				if (this.appPath == null)
					this.appPath = ICall.GetCurrentPath();
				return this.appPath;
			}
		}

		public long UnixTime
		{
			get { return ICall.GetUnixTime(); }
		}

		private Config config;
		public IConfig Config
		{
			get { return this.config; }
		}

		private HttpRequest request;
		public IRequest Request
		{
			get { return this.request; }
		}

		public HttpRequest HttpRequest
		{
			get { return this.request; }
		}

		private HttpResponse response;
		public IResponse Response
		{
			get { return this.response; }
		}

		public HttpResponse HttpResponse
		{
			get { return this.response; }
		}

		private string handlerName;
		public string HandlerName
		{
			set { this.handlerName = value; }
			get { return this.handlerName; }
		}

		private HttpCookie CreateCookie(string sid)
		{
			HttpCookie cookie = new HttpCookie(config.SessionCookieName, sid);
			cookie.Expires = DateTime.Now.AddSeconds(config.SessionCookieExpire);
			//cookie.Path = string.IsNullOrEmpty(config.SessionCookiePath) ? "/" + config.AppName : config.SessionCookiePath;
			if (!string.IsNullOrEmpty(response.CookiePath))
				cookie.Path = response.CookiePath;
			else if (!string.IsNullOrEmpty(config.SessionCookiePath))
				cookie.Path = config.SessionCookiePath;
			if (!string.IsNullOrEmpty(config.SessionCookieDomain))
				cookie.Domain = config.SessionCookieDomain;
			return cookie;
		}

		private ISession session;
		public ISession Session
		{
			get {
				if (this.session == null) {
					HttpCookie cookie = null;
					if (request.Cookies != null)
						cookie = request.Cookies.Get(config.SessionCookieName);

					if (cookie == null || string.IsNullOrEmpty(cookie.Value) || cookie.Value.Length < 32) {
						Guid guid = System.Guid.NewGuid();
						string sid = Convert.ToBase64String(Encoding.UTF8.GetBytes(guid.ToString() + UnixTime));
						this.session = new Session(config, sid);
						response.Cookies.Set(CreateCookie(sid));
					} else {
						this.session = new Session(config, cookie.Value);
					}
				}
				return this.session;
			}
		}

		public Context(Config config)
		{
			this.config = config;
			this.flashMemory = new Dictionary<string, object>();
			this.global = new Dictionary<string, object>();
			this.request = new HttpRequest(config);
			this.response = new HttpResponse(OS, this.request);
		}

		public void Refresh()
		{
			request.Dispose();
			request = null;
			request = new HttpRequest(config);

			response.Dispose();
			response = null;
			response = new HttpResponse(OS, request);

			handlerName = null;

			if (flashMemory.Count > 0) {
				flashMemory = null;
				flashMemory = new Dictionary<string, object>();
			}

			if (session != null)
				session.Dispose();
			session = null;
		}

		public object this[string key]
		{
			get {
				return global.ContainsKey(key) ? global[key] : null;
			}
			set {
				if (value == null)
					global.Remove(key);
				else
					global[key] = value;
			}
		}

		public Dictionary<string, object> FM
		{
			get { return this.flashMemory; }
		}

		public void RemoveSessionCookie()
		{
			HttpCookie cookie = request.Cookies.Get(config.SessionCookieName);
			if (cookie != null) {
				if (!string.IsNullOrEmpty(config.SessionCookiePath))
					cookie.Path = config.SessionCookiePath;
				if (!string.IsNullOrEmpty(config.SessionCookieDomain))
					cookie.Domain = config.SessionCookieDomain;
				cookie.Expires = DateTime.Now.AddDays(-10);
				response.Cookies.Set(cookie);
			}
		}

		public void ConfigAdjust(Dictionary<string, object> opt)
		{
			config.Adjust(opt);
		}

		public byte[] Serialize(object obj)
		{
			MemoryStream ms = new MemoryStream();
			BinaryFormatter bf = new BinaryFormatter();
			byte[] bytes = null;
			try {
				bf.Serialize(ms, obj);
				ms.Seek(0, SeekOrigin.Begin);
				bytes = ms.ToArray();
			} catch {
				bytes = null;
			} finally {
				ms.Close();
			}
			return bytes;
		}

		public object Deserialize(byte[] bytes)
		{
			Stream s = new MemoryStream(bytes);
			BinaryFormatter bf = new BinaryFormatter();
			object obj = null;
			try {
				obj = bf.Deserialize(s);
			} catch {
				obj = null;
			} finally {
				s.Close();
			}
			return obj;
		}

		~Context()
		{
			this.flashMemory = null;
			this.global = null;
			this.config = null;
			this.request = null;
			this.response = null;
			this.session = null;
		}
	}
}
