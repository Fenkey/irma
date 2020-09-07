using System;
using System.IO;
using System.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Collections.Generic;
using IRMACore.Lower;
using IRMAKit.Utils;
using IRMAKit.Log;
using IRMAKit.Store;

namespace IRMAKit.Configure
{
	public delegate string LoadRoutersJson(IConfig config);

	public delegate string LoadUserJson(IConfig config);

	internal sealed class Config : IConfig
	{
		private IUnit u = new Unit();

		private string spiritUrl;

		private string appName;
		public string AppName
		{
			get { return this.appName; }
		}

		private string version;
		public string Version
		{
			get { return this.version; }
		}

		private string releaseInfo;
		public string ReleaseInfo
		{
			get { return this.releaseInfo; }
		}

		private string appCharset;
		public string AppCharset
		{
			get {
				if (this.appCharset == null)
					this.appCharset = "UTF-8";
				return this.appCharset;
			}
		}

		private long bodyMax = 0L;

		private bool strictPostPut = false;
		public bool StrictPostPut
		{
			get { return this.strictPostPut; }
		}

		private string sessionEngine;
		public string SessionEngine
		{
			get { return this.sessionEngine; }
		}

		private string sessionServers;
		public string SessionServers
		{
			get { return this.sessionServers; }
		}

		private string sessionInstance;
		public string SessionInstance
		{
			get { return this.sessionInstance; }
		}

		private long sessionExpire = 86400L;
		public long SessionExpire
		{
			get { return this.sessionExpire; }
		}

		private IKeyValueStore sessionStore;
		public IKeyValueStore SessionStore
		{
			get { return this.sessionStore; }
		}

		private string sessionCookieName = "IRMASessionID";
		public string SessionCookieName
		{
			get { return this.sessionCookieName; }
		}

		private string sessionCookiePath;
		public string SessionCookiePath
		{
			get { return this.sessionCookiePath; }
		}

		private string sessionCookieDomain;
		public string SessionCookieDomain
		{
			get { return this.sessionCookieDomain; }
		}

		private long sessionCookieExpire = 31536000L;
		public long SessionCookieExpire
		{
			get { return this.sessionCookieExpire; }
		}

		private string performance;
		public string Performance
		{
			get { return this.performance; }
		}

		private Dictionary<string, string> routers;
		public Dictionary<string, string> Routers
		{
			get { return this.routers; }
			set { this.routers = value; }
		}

		private JObject user;
		public JObject User
		{
			get { return this.user; }
		}

		private JArray LoadRouters(string json)
		{
			JArray ja = null;
			if (!string.IsNullOrEmpty(json)) {
				try {
					ja = JArray.Parse(json);
				} catch {
					ja = null;
					Logger.WARN("Kit - Invalid routers' JSON which is loaded by LoadRoters()");
				}
			}
			return ja;
		}

		private JObject LoadUser(string json)
		{
			JObject jo = null;
			if (!string.IsNullOrEmpty(json)) {
				try {
					jo = JObject.Parse(json);
				} catch {
					jo = null;
					Logger.WARN("Kit - Invalid user's JSON which is loaded by LoadUser()");
				}
			}
			return jo;
		}

		private void ParseRouters(JArray ja)
		{
			routers = new Dictionary<string, string>();
			for (int i = 0; i < ja.Count; i++) {
				JObject jo = ja.Value<JObject>(i);

				string path = jo.Value<string>("path");
				if (string.IsNullOrEmpty(path))
					continue;
				path = path.Trim();

				string handler = jo.Value<string>("handler");
				if (string.IsNullOrEmpty(handler) || handler.IndexOf('`') >= 0)
					continue;
				handler = handler.Trim();

				string methods = jo.Value<string>("methods");
				if (string.IsNullOrEmpty(methods) || methods.IndexOf('`') >= 0)
					methods = "GET";

				bool pf = jo.Value<bool>("pf");

				routers[path] = handler + "`" + methods + "`" + pf.ToString();
			}
			if (routers.Count <= 0)
				throw new ConfigParamInvalidException("system.routers");
		}

		private void ParseSessionServer(JObject jo)
		{
			sessionServers = jo.Value<string>("servers");
			if (string.IsNullOrEmpty(sessionServers))
				throw new ConfigParamNotFoundException("system.session.server.servers");

			sessionInstance = jo.Value<string>("instance");
			if (string.IsNullOrEmpty(sessionInstance))
				throw new ConfigParamNotFoundException("system.session.server.instance");

			/*
			Nullable<long> expire = jo.Value<long>("expire");
			if (expire != null && expire.HasValue && expire.Value > 0)
				sessionExpire = expire.Value;
			*/
			long se = sessionExpire;
			try {
				se = jo.Value<long>("expire");
			} catch {
				se = u.ParseSeconds(jo.Value<string>("expire"));
			}
			if (se >= 0) //0 means never expired.
				sessionExpire = se;

			sessionEngine = jo.Value<string>("engine");
			if (string.IsNullOrEmpty(sessionEngine))
				sessionEngine = "MEMCACHED";
			sessionEngine = sessionEngine.Trim().ToUpper();

			if (sessionEngine == "MEMCACHED")
				sessionStore = new MemcachedStore(sessionServers, appName + "/" + sessionInstance);
			else if (sessionEngine == "CMEMCACHED")
				sessionStore = new CMemcachedStore(sessionServers, appName + "/" + sessionInstance, 5120L);
			else if (sessionEngine == "MEMCACHEDWRAPPER")
				sessionStore = new MemcachedStoreWrapper(sessionServers, appName + "/" + sessionInstance, 5120L);
			else if (sessionEngine == "CREDIS")
				sessionStore = new CRedisStore(sessionServers, appName + "/" + sessionInstance);
			else
				throw new ConfigParamInvalidException("system.session.server.engine");
		}

		private void ParseSessionClient(JObject jo)
		{
			string p = jo.Value<string>("cookie_name");
			if (!string.IsNullOrEmpty(p))
				sessionCookieName = p;

			sessionCookiePath = "/" + appName;
			p = jo.Value<string>("cookie_path");
			if (!string.IsNullOrEmpty(p)) {
				if (!p.StartsWith("/"))
					throw new ConfigParamInvalidException("system.session.client.cookie_path");
				sessionCookiePath = p;
			}

			p = jo.Value<string>("cookie_domain");
			if (!string.IsNullOrEmpty(p))
				sessionCookieDomain = p;

			/*
			Nullable<long> expire = jo.Value<long>("cookie_expire");
			if (expire != null && expire.HasValue && expire.Value > 0)
				sessionCookieExpire = expire.Value;
			*/
			long sce = sessionCookieExpire;
			try {
				sce = jo.Value<long>("cookie_expire");
			} catch {
				sce = u.ParseSeconds(jo.Value<string>("cookie_expire"));
			}
			if (sce > 0)
				sessionCookieExpire = sce;
		}

		private void ParseSession(JObject jo)
		{
			JObject jserver = jo.Value<JObject>("server");
			ParseSessionServer(jserver);

			JObject jclient = jo.Value<JObject>("client");
			ParseSessionClient(jclient);
		}

		public Config(string configFile, LoadRoutersJson loadRoutersJson, LoadUserJson loadUserJson, out Dictionary<string, object> opt)
		{
			string content = null;
			try {
				using (StreamReader sr = new StreamReader(configFile)) {
					content = sr.ReadToEnd();
				}
			} catch {
				throw new ConfigFileInvalidException(configFile);
			}
			if (string.IsNullOrEmpty(content))
				throw new ConfigFileInvalidException(configFile);

			JObject root;
			try {
				root = JObject.Parse(content);
			} catch (Exception e) {
				throw new ConfigJsonParseFailedException(configFile, e.Message);
			}

			JObject jsystem = root.Value<JObject>("system");
			if (jsystem == null)
				throw new ConfigParamNotFoundException("system");

			/* 'appName' allowed to be null. For example we might access site via http://<ip>:<port> */
			appName = jsystem.Value<string>("app_name");
			if (!string.IsNullOrEmpty(appName) &&
				(appName.StartsWith("/") || appName.EndsWith("/") || appName.IndexOf(" ") >= 0 || appName.IndexOf("\t") >= 0))
				throw new ConfigParamInvalidException("system.app_name");

			version = jsystem.Value<string>("version");

			releaseInfo = jsystem.Value<string>("release_info");

			appCharset = jsystem.Value<string>("app_charset");

			long bm = bodyMax;
			try {
				bm = jsystem.Value<long>("body_max");
			} catch {
				bm = u.ParseBytes(jsystem.Value<string>("body_max"));
			}
			if (bm > 0)
				bodyMax = bm;

			strictPostPut = jsystem.Value<bool>("strict_post_put");

			JObject jsession = jsystem.Value<JObject>("session");
			if (jsession != null)
				ParseSession(jsession);

			performance = jsystem.Value<string>("performance");

			user = root.Value<JObject>("user");

			// Note the priority of loading
			JArray jrouters = null;
			if (loadRoutersJson != null)
				jrouters = LoadRouters(loadRoutersJson(this));
			if (jrouters == null)
				jrouters = jsystem.Value<JArray>("routers");
			if (jrouters == null || jrouters.Count <= 0)
				throw new ConfigParamNotFoundException("system.routers");
			ParseRouters(jrouters);

			if (loadUserJson != null) {
				JObject jo = LoadUser(loadUserJson(this));
				if (jo != null)
					user = jo;
			}

			spiritUrl = jsystem.Value<string>("spirit_url");

			ICall.LaunchInfo(appName, version, bodyMax, spiritUrl);

			opt = new Dictionary<string, object>() {
				{ "release_info", releaseInfo },
				{ "app_charset", appCharset },
				{ "body_max", bodyMax },
				{ "strict_post_put", strictPostPut },
				{ "session.server.engine", sessionEngine },
				{ "session.server.servers", sessionServers },
				{ "session.server.instance", sessionInstance },
				{ "session.server.expire", sessionExpire },
				{ "session.client.cookie_name", sessionCookieName },
				{ "session.client.cookie_path", sessionCookiePath },
				{ "session.client.cookie_domain", sessionCookieDomain },
				{ "session.client.cookie_expire", sessionCookieExpire }
			};
		}

		public void Adjust(Dictionary<string, object> opt)
		{
			releaseInfo = (string)opt["release_info"];
			appCharset = (string)opt["app_charset"];
			bodyMax = (long)opt["body_max"];
			strictPostPut = (bool)opt["strict_post_put"];
			sessionEngine = (string)opt["session.server.engine"];
			sessionServers = (string)opt["session.server.servers"];
			sessionInstance = (string)opt["session.server.instance"];
			sessionExpire = (long)opt["session.server.expire"];
			sessionCookieName = (string)opt["session.client.cookie_name"];
			sessionCookiePath = (string)opt["session.client.cookie_path"];
			sessionCookieDomain = (string)opt["session.client.cookie_domain"];
			sessionCookieExpire = (long)opt["session.client.cookie_expire"];
		}

		~Config()
		{
			this.routers = null;
		}
	}
}
