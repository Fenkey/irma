using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using IRMAKit.Configure;
using IRMAKit.Log;

namespace IRMAKit.Web
{
	public enum RefMode
	{
		COVER = 0,	// AppLocation 完全覆盖/取代模式（优点：效率高；缺点：功能简单）
		REPLACE,	// AppLocation 前缀匹配修改模式（功能及效率折中）
		REGULAR		// AppLocation 正则替换模式（优点：功能强；缺点：效率相对低）
	}

	[AttributeUsage(AttributeTargets.Class, Inherited=false, AllowMultiple=false)]
	public class RefAttribute : ReqRefAttribute
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

		private RefMode mode = RefMode.COVER;
		public RefMode Mode
		{
			get { return this.mode; }
			set { this.mode = value; }
		}

		private object param;
		public object Param
		{
			get { return this.param; }
			set { this.param = value; }
		}

		public RefAttribute()
		{
		}

		public RefAttribute(string map)
		{
			this.map = map;
		}

		public RefAttribute(string map, RefMode mode)
		{
			this.map = map;
			this.mode = mode;
		}

		public RefAttribute(string map, object param)
		{
			this.map = map;
			this.param = param;
		}

		public RefAttribute(string map, RefMode mode, object param)
		{
			this.map = map;
			this.mode = mode;
			this.param = param;
		}

		public override bool Init(IConfig config, string handlerName, byte methods)
		{
			if (string.IsNullOrEmpty(map))
				return false;
			toDict = new Dictionary<string, string>();
			string[] items = map.Split(new char[] {',', ';'}, StringSplitOptions.None|StringSplitOptions.RemoveEmptyEntries);
			foreach (string item in items) {
				string[] s = item.Split(new string[] {"->"}, StringSplitOptions.None|StringSplitOptions.RemoveEmptyEntries);
				if (s.Length != 2 && s.Length != 3) {
					Logger.WARN("Kit - Invalid Ref in handler '{0}': {1}", handlerName, item);
					continue;
				}
				string s0 = s[0].Trim();
				string s1 = s[1].Trim();
				if (s0 == s1 || !s0.StartsWith("/") || !s1.StartsWith("/")) {
					Logger.WARN("Kit - Invalid Ref in handler '{0}': {1}", handlerName, item);
					continue;
				}
				if (mode == RefMode.REPLACE && (s0.EndsWith("/") ^ s1.EndsWith("/"))) {
					Logger.WARN("Kit - Invalid Ref(mode=REPLACE) in handler '{0}': {1}", handlerName, item);
					continue;
				}
				toDict[s0] = s1;
				if (s.Length == 3 && !string.IsNullOrEmpty(s[2])) {
					string s2 = s[2].Trim();
					if (paramDict == null)
						paramDict = new Dictionary<string, string>();
					paramDict[s0] = s2;
				}
			}
			if (toDict.Count <= 0)
				return false;

			if (mode == RefMode.COVER)
				return true;

			// key长度优先排序（由长到短）
			List<string> tmpList = new List<string>(toDict.Keys);
			tmpList.Sort(delegate(string L, string R) { return R.Length - L.Length; });
			if (mode == RefMode.REPLACE) {
				toList = tmpList;
				return true;
			}

			// RefMode.REGULAR
			toList = new List<string>();
			xList = new List<Regex>();
			foreach (string p in tmpList) {
				try {
					Regex regex = new Regex(p, RegexOptions.Compiled);
					xList.Add(regex);
					toList.Add(p);
				} catch {
					Logger.WARN("Kit - Invalid Ref(mode=REGULAR) left item in handler '{0}': {1}", handlerName, p);
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

		protected override string To(IContext context)
		{
			string to = null, toParam = null, path = context.Request.AppLocation;

			switch (mode) {
			case RefMode.COVER:
				if (toDict.ContainsKey(path)) {
					to = toDict[path];
					if (paramDict != null)
						paramDict.TryGetValue(path, out toParam);
				}
				break;

			case RefMode.REPLACE:
				foreach (string p in toList) {
					if (path.StartsWith(p)) {
						to = toDict[p] + path.Substring(p.Length);
						if (paramDict != null)
							paramDict.TryGetValue(p, out toParam);
						break;
					}
				}
				break;

			case RefMode.REGULAR:
				for (int i = 0; i < xList.Count; i++) {
					Match m = xList[i].Match(path);
					if (!m.Success)
						continue;
					if (m.Groups.Count == 1) {
						// 视为与COVER模式相同
						to = toDict[toList[i]];
						if (paramDict != null)
							paramDict.TryGetValue(toList[i], out toParam);
						break;
					}
					object []o = new object[m.Groups.Count-1];
					for (int j = 1; j < m.Groups.Count; j++)
						o[j-1] = m.Groups[j].Value;
					try {
						to = string.Format(toDict[toList[i]], o);
						if (paramDict != null)
							paramDict.TryGetValue(toList[i], out toParam);
						break;
					} catch {
						to = null;
						toParam = null;
					} finally {
						o = null;
					}
				}
				break;
			}

			if (to != null) {
				context.Request.RefParam = param;
				context.Request.RefToParam = toParam;
			}
			return to;
		}
	}
}
