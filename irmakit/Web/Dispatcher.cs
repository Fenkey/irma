using System;
using System.Text;
using System.Reflection;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using IRMACore.Lower;
using IRMAKit.Log;
using IRMAKit.Configure;

namespace IRMAKit.Web
{
	internal sealed class Dispatcher : IDispatcher
	{
		private class Actor
		{
			public string MAllow;
			public byte Methods;
			public IHandler Handler;
			public string ClassName;
			public CrossOriginAllowAttribute Coa;
			public List<ReqCheckAttribute> CheckList;
			public ReqRefAttribute Ref;
			public ReqProxyAttribute Proxy;
			public List<ReqEndAttribute> EndList;
			public bool FuseCheck;
			public bool Pf;
			public bool CacheAllow;

			public Actor(string mAllow, byte methods, IHandler handler, string className, bool pf)
			{
				this.MAllow = mAllow;
				this.Methods = methods;
				this.Handler = handler;
				this.ClassName = className;
				this.CheckList = new List<ReqCheckAttribute>();
				this.Ref = null;
				this.Proxy = null;
				this.EndList = new List<ReqEndAttribute>();
				this.FuseCheck = false;
				this.Pf = pf;
				this.CacheAllow = true;
			}
		}
		private Config config;
		private IHandlePerformance performance;
		private Dictionary<string, IHandler> handlers = new Dictionary<string, IHandler>();
		private Dictionary<string, Actor> valueMapping = new Dictionary<string, Actor>();
		private Dictionary<string, Actor> regexCache = new Dictionary<string, Actor>();
		private List<KeyValuePair<Regex, Actor>> regexMapping = new List<KeyValuePair<Regex, Actor>>();

		private void ParseAttribute(string className, ref Actor actor, Attribute[] attributes)
		{
			foreach (Attribute a in attributes) {
				Type t = a.GetType();
				if (!t.IsSubclassOf(typeof(IrmaAttribute)))
					continue;

				if (t.IsSubclassOf(typeof(ReqCheckAttribute))) {
					if (t.Equals(typeof(CrossOriginAllowAttribute))) {
						actor.Methods |= (byte)Method.OPTIONS;
						actor.Coa = (CrossOriginAllowAttribute)a;
					}

					ReqCheckAttribute rca = (ReqCheckAttribute)a;
					if (!rca.Init(config, className, actor.Methods))
						continue;
					actor.CheckList.Add(rca);
					/*
					 * Don't use cache (RestApi[Cache=false])
					 * if the parameters are various and dymanic.
					 */
					if (!rca.Cache)
						actor.CacheAllow = false;
				} else if (t.IsSubclassOf(typeof(ReqRefAttribute))) {
					ReqRefAttribute rfa = (ReqRefAttribute)a;
					if (!rfa.Init(config, className, actor.Methods))
						continue;
					actor.Ref = rfa;
					if (actor.Ref.Retrieve("Kit - Ref found: " + actor.ClassName + ", ") <= 0) {
						actor.Ref = null;
						Logger.WARN("Kit - No any valid Ref on {0}", actor.ClassName);
					}
				} else if (t.IsSubclassOf(typeof(ReqProxyAttribute))) {
					ReqProxyAttribute rpa = (ReqProxyAttribute)a;
					if (!rpa.Init(config, className, actor.Methods))
						continue;
					actor.Proxy = rpa;
					if (actor.Proxy.Retrieve("Kit - Proxy found: " + actor.ClassName + ", ") <= 0) {
						actor.Proxy = null;
						Logger.WARN("Kit - No any valid Proxy on {0}", actor.ClassName);
					}
				} else if (t.IsSubclassOf(typeof(ReqEndAttribute))) {
					ReqEndAttribute rea = (ReqEndAttribute)a;
					if (!rea.Init(config, className, actor.Methods))
						continue;
					actor.EndList.Add(rea);
				} else if (t.Equals(typeof(FuseCheckAttribute))) {
					actor.FuseCheck = true;
				}
			}
			actor.EndList.Sort(delegate(ReqEndAttribute L, ReqEndAttribute R) { return L.OrderNum.CompareTo(R.OrderNum); });
			actor.CheckList.Sort(delegate(ReqCheckAttribute L, ReqCheckAttribute R) { return L.OrderNum.CompareTo(R.OrderNum); });
		}

		private Type FindType(string className)
		{
			Assembly[] assemblies = AppDomain.CurrentDomain.GetAssemblies();
			foreach (Assembly a in assemblies) {
				Type[] types = a.GetTypes();
				foreach (Type t in types) {
					if (t.FullName.Equals(className))
						return t;
				}
			}
			return null;
		}

		private Actor FindActor(Context context)
		{
			Actor actor = null;
			Method method = context.Request.Method;
			// Don't use OriAppLocation !
			string path = context.Request.AppLocation;

			if (valueMapping.ContainsKey(path))
				actor = valueMapping[path];
			else if (regexCache.ContainsKey(path))
				actor = regexCache[path];
			else {
				foreach (KeyValuePair<Regex, Actor> r in regexMapping) {
					if (r.Key.IsMatch(path) == true) {
						actor = r.Value;
						if (actor.CacheAllow)
							regexCache[path] = actor;
						break;
					}
				}
			}

			if (actor == null) {
				context.HandlerName = null;
				throw new AccessNotFoundException(context.Request.OriAppLocation);
			}
			context.HandlerName = actor.ClassName;
			if ((actor.Methods & (byte)method) <= 0)
				throw new MethodNotAllowedException(method.ToString(), actor.MAllow);
			else if (config.StrictPostPut && (method == Method.POST || method == Method.PUT) && context.Request.TotalBytes <= 0)
				throw new BodyIsEmptyException(method.ToString());
			return actor;
		}

		public Dispatcher(Config config)
		{
			if (config.Routers == null)
				throw new DispatcherInitFailException("Routers is null");
			foreach (KeyValuePair<string, string> r in config.Routers) {
				string[] s = r.Value.Split(new char[] {'`'});
				if (s.Length != 3)
					continue;
				string className = s[0];
				string methods = s[1];
				bool pf;
				bool.TryParse(s[2], out pf);
				Register(methods, r.Key, className, pf);
			}
			// It is not useful any more.
			config.Routers = null;
			this.config = config;

			if (this.valueMapping.Count <= 0 && this.regexMapping.Count <= 0)
				throw new DispatcherInitFailException("No any valid router");

			if (!string.IsNullOrEmpty(config.Performance)) {
				Type clazz = FindType(config.Performance);
				if (clazz != null)
					this.performance = (IHandlePerformance)Activator.CreateInstance(clazz);
			}
		}

		private bool Register(string mAllow, byte methods, string path, IHandler handler, string className, bool pf)
		{
			if (string.IsNullOrEmpty(path) || handler == null)
				return false;
			Attribute[] attributes = Attribute.GetCustomAttributes(handler.GetType());
			Actor actor = new Actor(mAllow, methods, handler, className, pf);
			ParseAttribute(className, ref actor, attributes);

			if (path[0] == '@') {
				path = path.Substring(1);
				if (string.IsNullOrEmpty(path)) {
					Logger.WARN("Kit - Invalid router: {0}", path);
					return false;
				}
				valueMapping[path] = actor;
			} else {
				try {
					Regex regex = new Regex(path, RegexOptions.Compiled);
					regexMapping.Add(new KeyValuePair<Regex, Actor>(regex, actor));
				} catch {
					Logger.WARN("Kit - Invalid router: {0}", path);
					return false;
				}
			}
			return true;
		}

		public bool Register(string mAllow, byte methods, string path, string className, bool pf)
		{
			Type clazz = FindType(className);
			if (clazz == null) {
				Logger.WARN("Kit - Handler of router('{0}') is not found: {1}", path, className);
				return false;
			}
			try {
				if (!handlers.ContainsKey(className))
					handlers[className] = (IHandler)Activator.CreateInstance(clazz);
				return Register(mAllow, methods, path, handlers[className], className, pf);
			} catch (Exception e) {
				Logger.WARN("Kit - Register router('{0}') wrong: {1}", path, e.Message);
				return false;
			}
		}

		public bool Register(string mAllow, Method method, string path, string className, bool pf)
		{
			return Register(mAllow, (byte)method, path, className, pf);
		}

		public bool Register(string methods, string path, string className, bool pf)
		{
			string[] ms = methods.Split(new char[] {',', ';', '|'}, StringSplitOptions.None|StringSplitOptions.RemoveEmptyEntries);
			byte _methods = 0;
			try {
				foreach (string p in ms) {
					if (p.Equals("*"))
						_methods |= (byte)Method.ALL;
					else {
						Method m = (Method)Enum.Parse(typeof(Method), p);
						_methods |= (byte)m;
					}
				}
				return Register(string.Join(",", ms), _methods, path, className, pf);
			} catch {
				return false;
			}
		}

		private void DoWithoutPerformance(Actor actor, Context context)
		{
			foreach (ReqCheckAttribute rc in actor.CheckList) {
				if (!rc.CheckWrapper(context)) {
					context.Response.Forbidden();
					return;
				}
			}
			if (context.HttpResponse.RoutineBreak)
				return;

			if (actor.Ref != null) {
				string to = actor.Ref.ToWrapper(context);
				if (!string.IsNullOrEmpty(to)) {
					if (context.Request.IsRef && context.HttpRequest.HopCount <= 0) {
						throw new RefHopOutException(string.Format("{0} -> ... -> {1} -> {2}",
							context.Request.OriAppLocation, context.Request.AppLocation, to));
					}
					// Try again
					context.HttpRequest.AppLocation = to;
					Dispatch(context);
					return;
				}
			}

			if (actor.Proxy != null) {
				string param = null;
				string to = actor.Proxy.ToWrapper(context, ref param);
				if (!string.IsNullOrEmpty(to)) {
					if (context.Request.IsProxy) {
						throw new ProxyHopOutException(string.Format("{0} -> ... -> {1}",
							context.Request.OriAppLocation, to));
					}
					throw new GoToProxyException(to, param);
				}
			}

			actor.Handler.Do(context);
			if (context.HttpResponse.RoutineBreak)
				return;

			foreach (ReqEndAttribute f in actor.EndList)
				f.EndWrapper(context);
		}

		private void DoWithPerformance(Actor actor, Context context)
		{
			Nullable<DateTime> checkStart, checkStop, handleStart, handleStop, endStart, endStop;
			checkStart = checkStop = handleStart = handleStop = endStart = endStop = null;

			try {
				if (actor.CheckList.Count > 0) {
					checkStart = DateTime.Now;
					try {
						foreach (ReqCheckAttribute rc in actor.CheckList) {
							if (!rc.CheckWrapper(context)) {
								context.Response.Forbidden();
								return;
							}
						}
					} catch (Exception e) {
						throw;
					} finally {
						checkStop = DateTime.Now;
					}
				}
				if (context.HttpResponse.RoutineBreak)
					return;

				if (actor.Ref != null) {
					string to = actor.Ref.ToWrapper(context);
					if (!string.IsNullOrEmpty(to)) {
						if (context.Request.IsRef && context.HttpRequest.HopCount <= 0) {
							throw new RefHopOutException(string.Format("{0} -> ... -> {1} -> {2}",
								context.Request.OriAppLocation, context.Request.AppLocation, to));
						}
						// Try again
						context.HttpRequest.AppLocation = to;
						Dispatch(context);
						return;
					}
				}

				if (actor.Proxy != null) {
					string param = null;
					string to = actor.Proxy.ToWrapper(context, ref param);
					if (!string.IsNullOrEmpty(to)) {
						if (context.Request.IsProxy) {
							throw new ProxyHopOutException(string.Format("{0} -> ... -> {1}",
								context.Request.OriAppLocation, to));
						}
						throw new GoToProxyException(to, param);
					}
				}

				handleStart = DateTime.Now;
				try {
					actor.Handler.Do(context);
				} catch (Exception e) {
					throw;
				} finally {
					handleStop = DateTime.Now;
				}
				if (context.HttpResponse.RoutineBreak)
					return;

				if (actor.EndList.Count > 0) {
					endStart = DateTime.Now;
					try {
						foreach (ReqEndAttribute f in actor.EndList)
							f.EndWrapper(context);
					} catch (Exception e) {
						throw;
					} finally {
						endStop = DateTime.Now;
					}
				}
			} finally {
				try {
					performance.Remark(actor.ClassName, checkStart, checkStop, handleStart, handleStop, endStart, endStop);
				} catch {}
			}
		}

		public void Dispatch(Context context)
		{
			Actor actor = FindActor(context);

			if (actor.FuseCheck && ICall.FuseCheck(actor.ClassName) == false)
				return;

			if (context.Request.Method == Method.OPTIONS && actor.Coa != null && !context.Request.IsRef) {
				actor.Coa.CheckWrapper(context);
				context.Response.SendHttp(200);
				return;
			}

			if (!context.Request.IsRef) {
				// Import cookies to build session aeap.
				object useless = context.Request.Cookies;
			}

			if (performance != null && actor.Pf == true)
				DoWithPerformance(actor, context);
			else
				DoWithoutPerformance(actor, context);
		}

		public void Dispose()
		{
		}
	}
}
