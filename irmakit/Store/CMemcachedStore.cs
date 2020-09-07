using System;
using System.IO;
using System.Text;
using System.Collections.Generic;
using IRMACore.Lower;
using IRMAKit.Web;

namespace IRMAKit.Store
{
	public sealed class CMemcachedStore : IKeyValueStore
	{
		private long L;

		/*
		 * NOTE: .net自带的系列化/反系列化（即Service.CurrentContext.Serialize / Deserialize）效率不高
		 * 且数据冗余size大，不利于缓存考虑。故CMemcachedStore的设计建议应用层可考虑采用Newtonsoft库将对象
		 * 首先转换为json string后进行Serizlize缓存，CMemcachedStore也将按此优先考虑string情况；非string情
		 * 况，再退求其次按.net自带方式处理；反之，Deserizlize也一样考虑，优先尝试反系列化为string
		 *
		 * FIX：CMemcachedStore内不能直接采用Newtonsoft进行object系列化处理，将会因此失去准确的原始class信
		 * 息而导致最终无法准确Deserizlize为原始class对象
		 *
		 * Newtonsoft参考：
		 * using Newtonsoft.Json;
		 *
		 * object o = 99;
		 * string s = JsonConvert.SerializeObject(o);
		 * cm.Set(key1, s);
		 * s = cm.Get(key1);
		 * o = JsonConvert.DeserializeObject<object>(s);
		 *
		 * byte[] bytes = { 99, 88, 77 };
		 * s = JsonConvert.SerializeObject(bytes);
		 * cm.Set(key2, s);
		 * s = cm.Get(key2);
		 * bytes = JsonConvert.DeserializeObject<byte[]>(s);
		 *
		 * s = JsonConvert.SerializeObject(new C());
		 * cm.Set(key3, s);
		 * s = cm.Get(key3);
		 * C c = JsonConvert.DeserializeObject<C>(s);
		 *
		 * Dictionary<string, object> d = new Dictionary<string, object>() { ... };
		 * s = JsonConvert.SerializeObject(d);
		 * rs[key4] = s;
		 * s = rs[key4];
		 * d = JsonConvert.DeserializeObject<Dictionary<string, object>>(s);
		 *
		 * 本身是string数据，既可考虑Json系列化、也可直接处理：
		 * cm.Set(key5, "ABC");
		 * s = cm.Get(key5);
		 */
		private byte[] Serialize(object obj)
		{
			try {
				if (obj.GetType() == typeof(string))
					return Encoding.UTF8.GetBytes((string)obj);
				return Service.CurrentContext.Serialize(obj);
			} catch {
				return null;
			}
		}

		private object Deserialize(byte[] bytes)
		{
			try {
				string s = Encoding.UTF8.GetString(bytes);
				if (s.IndexOf("mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken") > 0)
					return Service.CurrentContext.Deserialize(bytes);
				return s;
			} catch {
				return Service.CurrentContext.Deserialize(bytes);
			}
		}

		public CMemcachedStore(string serverStr, string instance, long zipMin)
		{
			string serverStrSave = serverStr;
			if (!string.IsNullOrEmpty(serverStr)) {
				string[] s = serverStr.Split(new char[] {',', ';', '|'}, StringSplitOptions.None|StringSplitOptions.RemoveEmptyEntries);
				for (int i = 0; i < s.Length; i++)
					s[i] = "--SERVER=" + s[i];
				serverStr = string.Join(" ", s);
			}
			if (string.IsNullOrEmpty(instance))
				instance = "irma";
			if ((this.L = ICall.MemcachedNew(serverStr, instance, zipMin)) <= 0)
				throw new KVStoreInvalidException(string.Format("(C)Memcached server invalid: {0}. Make sure it is running", serverStrSave));
		}

		public CMemcachedStore(string server, int port, string instance, long zipMin)
		{
			string serverStr = null;
			if (!string.IsNullOrEmpty(serverStr) && port > 0)
				serverStr = string.Format("--SERVER={0}:{1}", server, port);
			if (string.IsNullOrEmpty(instance))
				instance = "irma";
			if ((this.L = ICall.MemcachedNew(serverStr, instance, zipMin)) <= 0)
				throw new KVStoreInvalidException(string.Format("(C)Memcached server invalid: {0}:{1}. Make sure it is running", server, port));
		}

		public CMemcachedStore(string server, string instance) : this(server, instance, 0L)
		{
		}

		public CMemcachedStore(string server, long zipMin) : this(server, null, zipMin)
		{
		}

		public CMemcachedStore(long zipMin=0L) : this(null, null, zipMin)
		{
		}

		public CMemcachedStore(string server, int port, string instance) : this(server, port, instance, 0L)
		{
		}

		public CMemcachedStore(string server, int port) : this(server, port, null, 0L)
		{
		}

		public CMemcachedStore(string instance) : this(null, instance, 0L)
		{
		}

		public bool Add(string key, byte[] value, TimeSpan expire)
		{
			return Add(key, value, (long)expire.TotalSeconds);
		}

		public bool Add(string key, object value, TimeSpan expire)
		{
			return Add(key, value, (long)expire.TotalSeconds);
		}

		public bool Add(string key, byte[] value, long expire=0L)
		{
			return ICall.KvsAdd(L, key, value, expire);
		}

		public bool Add(string key, object value, long expire=0L)
		{
			byte[] bytes;
			try {
				bytes = Serialize(value);
				return bytes != null ? Add(key, bytes, expire) : false;
			} catch {
				return false;
			} finally {
				bytes = null;
			}
		}

		public bool Prepend(string key, byte[] value)
		{
			return ICall.KvsPrepend(L, key, value);
		}

		public bool Prepend(string key, object value)
		{
			byte[] bytes;
			try {
				bytes = Serialize(value);
				return bytes != null ? Prepend(key, bytes) : false;
			} catch {
				return false;
			} finally {
				bytes = null;
			}
		}

		public bool Append(string key, byte[] value)
		{
			return ICall.KvsAppend(L, key, value);
		}

		public bool Append(string key, object value)
		{
			byte[] bytes;
			try {
				bytes = Serialize(value);
				return bytes != null ? Append(key, bytes) : false;
			} catch {
				return false;
			} finally {
				bytes = null;
			}
		}

		public bool Set(string key, byte[] value, TimeSpan expire)
		{
			return Set(key, value, (long)expire.TotalSeconds);
		}

		public bool Set(string key, object value, TimeSpan expire)
		{
			return Set(key, value, (long)expire.TotalSeconds);
		}

		public bool Set(string key, byte[] value, long expire=0L)
		{
			return ICall.KvsSet(L, key, value, expire);
		}

		public bool Set(string key, object value, long expire=0L)
		{
			byte[] bytes;
			try {
				bytes = Serialize(value);
				return bytes != null ? Set(key, bytes, expire) : false;
			} catch {
				return false;
			} finally {
				bytes = null;
			}
		}

		public byte[] GetBytes(string key)
		{
			byte[] b = ICall.KvsGet(L, key);
			return b;
		}

		public object Get(string key)
		{
			byte[] bytes;
			try {
				bytes = GetBytes(key);
				return (bytes == null || bytes.Length <= 0) ? null : Deserialize(bytes);
			} catch {
				return null;
			} finally {
				bytes = null;
			}
		}

		public byte[][] MGetBytes(string[] keys)
		{
			return ICall.KvsMGet(L, keys);
		}

		public Dictionary<string, object> Get(string[] keys)
		{
			byte[][] aBytes;
			try {
				aBytes = MGetBytes(keys);
				if (aBytes == null || aBytes.Length <= 0)
					return null;
				Dictionary<string, object> objs = new Dictionary<string, object>();
				for (int i = 0; i < aBytes.Length; i++) {
					objs[keys[i]] = (aBytes[i] == null || aBytes[i].Length <= 0) ? null : Deserialize(aBytes[i]);
				}
				return objs;
			} catch {
				return null;
			} finally {
				aBytes = null;
			}
		}

		public bool Replace(string key, byte[] value, TimeSpan expire)
		{
			return Replace(key, value, (long)expire.TotalSeconds);
		}

		public bool Replace(string key, object value, TimeSpan expire)
		{
			return Replace(key, value, (long)expire.TotalSeconds);
		}

		public bool Replace(string key, byte[] value, long expire=0L)
		{
			return ICall.KvsReplace(L, key, value, expire);
		}

		public bool Replace(string key, object value, long expire=0L)
		{
			byte[] bytes;
			try {
				bytes = Serialize(value);
				return bytes != null ? Replace(key, bytes, expire) : false;
			} catch {
				return false;
			} finally {
				bytes = null;
			}
		}

		public bool Expire(string key, TimeSpan expire)
		{
			return Expire(key, (long)expire.TotalSeconds);
		}

		public bool Expire(string key, long expire=0L)
		{
			return ICall.KvsExpire(L, key, expire);
		}

		public bool Exists(string key)
		{
			return ICall.KvsExists(L, key);
		}

		public bool Delete(string key)
		{
			return ICall.KvsDelete(L, key);
		}

		public bool SetCounter(string key, long value)
		{
			return ICall.KvsSetCounter(L, key, value);
		}

		public long GetCounter(string key)
		{
			return ICall.KvsGetCounter(L, key);
		}

		public long Increment(string key, long value=1)
		{
			return ICall.KvsIncr(L, key, value);
		}

		public long Decrement(string key, long value=1)
		{
			return ICall.KvsDecr(L, key, value);
		}

		public long LLen(string key)
		{
			return -1L;
		}

		public long RPush(string key, byte[][] vals)
		{
			return -1L;
		}

		public long RPush(string key, object value)
		{
			return -1L;
		}

		public long RPush(string key, params object[] aVals)
		{
			return -1L;
		}

		public long RPush(string key, List<object> lVals)
		{
			return -1L;
		}

		public byte[] LPopBytes(string key)
		{
			return null;
		}

		public object LPop(string key)
		{
			return null;
		}

		public string this[string key]
		{
			get {
				object value = Get(key);
				return value != null ? value.ToString() : null;
			}
			set {
				Set(key, value);
			}
		}
	}
}
