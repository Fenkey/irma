using System;
using System.IO;
using System.Threading;
using System.Collections.Generic;
using BeIT.MemCached;

namespace IRMAKit.Store
{
	public sealed class MemcachedStore : IKeyValueStore
	{
		private static object mutex = new object();

		private MemcachedClient client = null;

		// servers i.e.: localhost:11211,...
		public MemcachedStore(string servers, string instance)
		{
			if (string.IsNullOrEmpty(servers))
				servers = "127.0.0.1:11211";
			if (string.IsNullOrEmpty(instance))
				instance = "irma";
			string thxInstance = instance + "-" + Thread.CurrentThread.ManagedThreadId;

			Exception exp = null;
			try {
				string[] s = servers.Split(",".ToCharArray(), StringSplitOptions.None|StringSplitOptions.RemoveEmptyEntries);
				lock (MemcachedStore.mutex) {
					if (!MemcachedClient.Exists(thxInstance)) {
						MemcachedClient.Setup(thxInstance, s);
						this.client = MemcachedClient.GetInstance(thxInstance);
						if (this.client == null)
							throw new Exception();
						this.client.KeyPrefix = "n-" + instance + "-";
						if (!this.client.Set("test", "ok") || this.client.Get("test") == null)
							throw new Exception("Fail to test");
					} else {
						this.client = MemcachedClient.GetInstance(thxInstance);
					}
				}
			} catch (MemcachedClientException) {
				exp = new KVStoreInvalidException(string.Format("Memcached servers invalid: {0}", servers));
			} catch (Exception e) {
				exp = new Exception(string.Format("Memcached client instance is null: {0}", e.Message));
			}

			if (exp != null) {
				this.client = null;
				throw exp;
			}
		}

		public MemcachedStore(string instance=null) : this(null, instance)
		{
		}

		public bool Add(string key, byte[] value, TimeSpan expire)
		{
			return client.Add(key, value, expire);
		}

		public bool Add(string key, object value, TimeSpan expire)
		{
			return client.Add(key, value, expire);
		}

		public bool Add(string key, byte[] value, long expire=0L)
		{
			return client.Add(key, value, TimeSpan.FromSeconds(expire));
		}

		public bool Add(string key, object value, long expire=0L)
		{
			return client.Add(key, value, TimeSpan.FromSeconds(expire));
		}

		public bool Prepend(string key, byte[] value)
		{
			return client.Prepend(key, value);
		}

		public bool Prepend(string key, object value)
		{
			return client.Prepend(key, value);
		}

		public bool Append(string key, byte[] value)
		{
			return client.Append(key, value);
		}

		public bool Append(string key, object value)
		{
			return client.Append(key, value);
		}

		public bool Set(string key, byte[] value, TimeSpan expire)
		{
			return client.Set(key, value, expire);
		}

		public bool Set(string key, object value, TimeSpan expire)
		{
			return client.Set(key, value, expire);
		}

		public bool Set(string key, byte[] value, long expire=0L)
		{
			return client.Set(key, value, TimeSpan.FromSeconds(expire));
		}

		public bool Set(string key, object value, long expire=0L)
		{
			return client.Set(key, value, TimeSpan.FromSeconds(expire));
		}

		public byte[] GetBytes(string key)
		{
			return null;
		}

		public object Get(string key)
		{
			return client.Get(key);
		}

		public byte[][] MGetBytes(string[] keys)
		{
			return null;
		}

		public Dictionary<string, object> Get(string[] keys)
		{
			Dictionary<string, object> objs = new Dictionary<string, object>();
			object[] objArray = client.Get(keys);
			if (objArray != null) {
				for (int i = 0; i < objArray.Length; i++)
					objs[keys[i]] = objArray[i];
			}
			return objs;
		}

		public bool Replace(string key, byte[] value, TimeSpan expire)
		{
			return client.Replace(key, value, expire);
		}

		public bool Replace(string key, object value, TimeSpan expire)
		{
			return client.Replace(key, value, expire);
		}

		public bool Replace(string key, byte[] value, long expire=0L)
		{
			return client.Replace(key, value, TimeSpan.FromSeconds(expire));
		}

		public bool Replace(string key, object value, long expire=0L)
		{
			return client.Replace(key, value, TimeSpan.FromSeconds(expire));
		}

		public bool Expire(string key, TimeSpan expire)
		{
			return false;
		}

		public bool Expire(string key, long expire=0L)
		{
			return false;
		}

		public bool Exists(string key)
		{
			return Get(key) != null;
		}

		public bool Delete(string key)
		{
			return client.Delete(key);
		}

		public bool SetCounter(string key, long value)
		{
			return client.SetCounter(key, (ulong)value);
		}

		public long GetCounter(string key)
		{
			Nullable<ulong> ret = client.GetCounter(key);
			return (ret == null || !ret.HasValue) ? 0L : (long)ret.Value;
		}

		public long Increment(string key, long value=1)
		{
			Nullable<ulong> ret = client.Increment(key, (ulong)value);
			return (ret == null || !ret.HasValue) ? 0L : (long)ret.Value;
		}

		public long Decrement(string key, long value=1)
		{
			Nullable<ulong> ret = client.Decrement(key, (ulong)value);
			return (ret == null || !ret.HasValue) ? 0L : (long)ret.Value;
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
