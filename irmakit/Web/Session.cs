using System;
using System.Text;
using System.Collections.Generic;
using IRMACore.Lower;
using IRMAKit.Store;
using IRMAKit.Configure;

namespace IRMAKit.Web
{
	internal sealed class Session : ISession
	{
		private bool disposed = false;

		private string keyPrefix0;

		private string keyPrefix1;

		private Config config;

		private Dictionary<string, object> innerStore;

		private string sid;
		public string SID
		{
			get { return this.sid; }
		}

		private bool Save(string key, object value, long expire)
		{
			Dictionary<string, object> d = new Dictionary<string, object>() {
				{"data", value},				// saving data
				{"st", ICall.GetUnixTime()},	// metadata: start time
				{"ex", expire}					// metadata: expiration
			};
			return config.SessionStore.Set(keyPrefix1 + key, d, expire);
		}

		public Session(Config config, string sid)
		{
			this.config = config;
			this.sid = sid;
			this.keyPrefix0 = string.Empty + config.AppName + config.SessionInstance;
			this.keyPrefix1 = this.keyPrefix0 + sid;
			this.innerStore = new Dictionary<string, object>();
		}

		public void Dispose()
		{
			if (disposed)
				return;
			sid = null;
			keyPrefix0 = null;
			keyPrefix1 = null;
			innerStore = null;
			disposed = true;
			GC.SuppressFinalize(this);
		}

		~Session()
		{
			Dispose();
		}

		public object this[string key]
		{
			get {
				if (sid == null || key == null)
					return null;
				if (innerStore.ContainsKey(key))
					return innerStore[key];
				object v = config.SessionStore.Get(keyPrefix1 + key);
				if (v != null) {
					Dictionary<string, object> d = (Dictionary<string, object>)v;
					if (d.ContainsKey("kt")) {
						config.SessionStore.Delete(keyPrefix1 + key);
						throw new SessionKickedOutException((DateTime)d["kt"]);
					}
					v = innerStore[key] = d["data"];
					long st = (long)d["st"];
					long ex = (long)d["ex"];
					/*
					 * Simple renewal on session:
					 * 1. 距离最初存入时间已过2/3，将自动触发续存
					 * 2. 续存时长设置为最初给定的过期周期1/2
					 */
					if (ICall.GetUnixTime() - st >= (ex<<1)/3)
						Save(key, v, config.SessionExpire>>1);
				}
				return v;
			}

			set {
				if (key == null)
					return;
				innerStore.Remove(key);
				if (sid != null && value != null)
					Save(key, value, config.SessionExpire);
			}
		}

		public bool Remove(string key)
		{
			if (key == null)
				return false;
			innerStore.Remove(key);
			return config.SessionStore.Delete(keyPrefix1 + key);
		}

		public bool AttachSid(string attachKey, string sessionKey, bool kickOutOnly=false)
		{
			if (attachKey == null || sessionKey == null || sid == null)
				return false;
			/*
			 * 踢出上一次的旧SID，将导致其失效而重新登录（我们标记而不删除，直至被首次检测到被踢后删除、或过期失效）
			 * FIX: 仅当旧SID与当前SID不一样时，才进行踢出标记。例如用户在其session有效情况下重复login时可避免将其
			 * 自身标记为踢出、并由此引发session值读取时弹出SessionKickedOutException异常
			 */
			bool ok = true;
			object v = config.SessionStore.Get(keyPrefix0 + attachKey);
			if (v != null && (string)v != sid) {
				string oldKeyPrefix1 = keyPrefix0 + (string)v;
				v = config.SessionStore.Get(oldKeyPrefix1 + sessionKey);
				if (v != null) {
					Dictionary<string, object> d = (Dictionary<string, object>)v;
					long ex = (long)d["ex"];
					d["kt"] = DateTime.Now;
					ok = config.SessionStore.Set(oldKeyPrefix1 + sessionKey, v, ex);
				}
			}
			// 设置或覆盖最新的SID，成为attachKey所对应的唯一登录者
			return kickOutOnly ? ok : config.SessionStore.Set(keyPrefix0 + attachKey, sid, 0);
		}

		public bool IsAttached(string attachKey, string sessionKey, ref bool currentIsYourself)
		{
			if (attachKey == null)
				return false;
			currentIsYourself = false;
			object v = config.SessionStore.Get(keyPrefix0 + attachKey);
			if (v == null)
				return false;
			bool b = (sid != null && (string)v == sid);
			string oldKeyPrefix1 = keyPrefix0 + (string)v;
			v = config.SessionStore.Get(oldKeyPrefix1 + sessionKey);
			if (v == null)
				return false;
			Dictionary<string, object> d = (Dictionary<string, object>)v;
			if (d.ContainsKey("kt"))
				return false;
			currentIsYourself = b;
			return true;
		}

		public bool IsAttached(string attachKey, string sessionKey)
		{
			bool currentIsYourself = false;
			return IsAttached(attachKey, sessionKey, ref currentIsYourself);
		}

		public bool Close()
		{
			Dispose();
			return true;
		}
	}
}
