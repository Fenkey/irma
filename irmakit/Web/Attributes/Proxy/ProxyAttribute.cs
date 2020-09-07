using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using IRMAKit.Configure;
using IRMAKit.Log;

namespace IRMAKit.Web
{
	public enum ProxyMode
	{
		COVER = 0,	// AppLocation 完全覆盖/取代模式（优点：效率高；缺点：功能简单）
		REPLACE,	// AppLocation 前缀匹配修改模式（功能及效率折中）
		REGULAR		// AppLocation 正则替换模式（优点：功能强；缺点：效率相对低）
	}

	[AttributeUsage(AttributeTargets.Class, Inherited=false, AllowMultiple=false)]
	public class ProxyAttribute : ReqProxyAttribute
	{
		private List<Regex> xList;
		private List<string> toList;
		private Dictionary<string, string> toDict;
		private Dictionary<string, string> paramDict;
		private string map;
		public string Map
		{
			get { return this.map; }
			set { this.map = value; }
		}

		private ProxyMode mode = ProxyMode.COVER;
		public ProxyMode Mode
		{
			get { return this.mode; }
			set { this.mode = value; }
		}

		public ProxyAttribute()
		{
		}

		public ProxyAttribute(string map)
		{
			this.map = map;
		}

		public ProxyAttribute(string map, ProxyMode mode)
		{
			this.map = map;
			this.mode = mode;
		}

		public override bool Init(IConfig config, string handlerName, byte methods)
		{
			toDict = new Dictionary<string, string>();
			paramDict = new Dictionary<string, string>();
			string[] items = map.Split(new char[] {',', ';'}, StringSplitOptions.None|StringSplitOptions.RemoveEmptyEntries);
			foreach (string item in items) {
				string[] s = item.Split(new string[] {"->"}, StringSplitOptions.None|StringSplitOptions.RemoveEmptyEntries);
				if (s.Length != 3) {
					Logger.WARN("Kit - Invalid Proxy in handler '{0}': {1}", handlerName, item);
					continue;
				}
				string s0 = s[0].Trim();
				string s1 = s[1].Trim();
				string s2 = s[2].Trim();
				if (s0 == s1 || !s0.StartsWith("/") || !s1.StartsWith("/")) {
					Logger.WARN("Kit - Invalid Proxy in handler '{0}': {1}", handlerName, item);
					continue;
				}
				if (mode == ProxyMode.REPLACE && (s0.EndsWith("/") ^ s1.EndsWith("/"))) {
					Logger.WARN("Kit - Invalid Proxy(mode=REPLACE) in handler '{0}': {1}", handlerName, item);
					continue;
				}
				toDict[s0] = s1;
				paramDict[s0] = s2;
			}
			if (toDict.Count <= 0)
				return false;

			if (mode == ProxyMode.COVER)
				return true;

			// key长度优先排序（由长到短）
			List<string> tmpList = new List<string>(toDict.Keys);
			tmpList.Sort(delegate(string L, string R) { return R.Length - L.Length; });
			if (mode == ProxyMode.REPLACE) {
				toList = tmpList;
				return true;
			}

			// ProxyMode.REGULAR
			toList = new List<string>();
			xList = new List<Regex>();
			foreach (string p in tmpList) {
				try {
					Regex regex = new Regex(p, RegexOptions.Compiled);
					xList.Add(regex);
					toList.Add(p);
				} catch {
					Logger.WARN("Kit - Invalid Proxy(mode=REGULAR) left item in handler '{0}': {1}", handlerName, p);
				}
			}
			return toList.Count > 0;
		}

		public override int Retrieve(string preStr)
		{
			/*
			foreach (KeyValuePair<string, string> kv in toDict)
				Logger.DEBUG(preStr + kv.Key + " -> " + kv.Value);
			*/
			return toDict.Count;
		}

		protected override string To(IContext context, ref string param)
		{
			string to = null, path = context.Request.AppLocation;

			switch (mode) {
			case ProxyMode.COVER:
				if (toDict.ContainsKey(path)) {
					to = toDict[path];
					param = paramDict[path];
				}
				break;
			case ProxyMode.REPLACE:
				foreach (string p in toList) {
					if (path.StartsWith(p)) {
						to = toDict[p] + path.Substring(p.Length);
						param = paramDict[p];
						break;
					}
				}
				break;
			case ProxyMode.REGULAR:
				for (int i = 0; i < xList.Count; i++) {
					Match m = xList[i].Match(path);
					if (!m.Success)
						continue;
					if (m.Groups.Count == 1) {
						// 视为与COVER模式相同
						to = toDict[toList[i]];
						param = paramDict[toList[i]];
					} else {
						object []o = new object[m.Groups.Count-1];
						for (int j = 1; j < m.Groups.Count; j++)
							o[j-1] = m.Groups[j].Value;
						try {
							to = string.Format(toDict[toList[i]], o);
							param = paramDict[toList[i]];
							break;
						} catch {
							to = null;
						} finally {
							o = null;
						}
					}
				}
				break;
			}

			return to;
		}
	}
}
