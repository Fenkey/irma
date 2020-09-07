using System;
using System.IO;
using System.Text;
using System.Collections.Generic;
using IRMAKit.Web;

namespace IRMAKit.Store
{
	public sealed class MemcachedStoreWrapper : IKeyValueStore
	{
		private IKeyValueStore store;

		public MemcachedStoreWrapper(string serverStr, string instance, long zipMin)
		{
			if (Service.CurrentContext.OS == "windows")
				this.store = new MemcachedStore(serverStr, instance);
			else
				this.store = new CMemcachedStore(serverStr, instance, zipMin);
		}

		public MemcachedStoreWrapper(string serverStr, string instance) : this(serverStr, instance, 0L)
		{
		}

		public MemcachedStoreWrapper(string instance, long zipMin) : this(null, instance, zipMin)
		{
		}

		public MemcachedStoreWrapper(string instance) : this(null, instance, 0L)
		{
		}

		public MemcachedStoreWrapper(long zipMin=0L) : this(null, null, zipMin)
		{
		}

		public bool Add(string key, byte[] value, TimeSpan expire)
		{
			return store.Add(key, value, expire);
		}

		public bool Add(string key, object value, TimeSpan expire)
		{
			return store.Add(key, value, expire);
		}

		public bool Add(string key, byte[] value, long expire=0L)
		{
			return store.Add(key, value, expire);
		}

		public bool Add(string key, object value, long expire=0L)
		{
			return store.Add(key, value, expire);
		}

		public bool Prepend(string key, byte[] value)
		{
			return store.Prepend(key, value);
		}

		public bool Prepend(string key, object value)
		{
			return store.Prepend(key, value);
		}

		public bool Append(string key, byte[] value)
		{
			return store.Append(key, value);
		}

		public bool Append(string key, object value)
		{
			return store.Append(key, value);
		}

		public bool Set(string key, byte[] value, TimeSpan expire)
		{
			return store.Set(key, value, expire);
		}

		public bool Set(string key, object value, TimeSpan expire)
		{
			return store.Set(key, value, expire);
		}

		public bool Set(string key, byte[] value, long expire=0L)
		{
			return store.Set(key, value, expire);
		}

		public bool Set(string key, object value, long expire=0L)
		{
			return store.Set(key, value, expire);
		}

		public byte[] GetBytes(string key)
		{
			return store.GetBytes(key);
		}

		public object Get(string key)
		{
			return store.Get(key);
		}

		public byte[][] MGetBytes(string[] keys)
		{
			return store.MGetBytes(keys);
		}

		public Dictionary<string, object> Get(string[] keys)
		{
			return store.Get(keys);
		}

		public bool Replace(string key, byte[] value, TimeSpan expire)
		{
			return store.Replace(key, value, expire);
		}

		public bool Replace(string key, object value, TimeSpan expire)
		{
			return store.Replace(key, value, expire);
		}

		public bool Replace(string key, byte[] value, long expire=0L)
		{
			return store.Replace(key, value, expire);
		}

		public bool Replace(string key, object value, long expire=0L)
		{
			return store.Replace(key, value, expire);
		}

		public bool Expire(string key, TimeSpan expire)
		{
			return store.Expire(key, expire);
		}

		public bool Expire(string key, long expire=0L)
		{
			return store.Expire(key, expire);
		}

		public bool Exists(string key)
		{
			return store.Exists(key);
		}

		public bool Delete(string key)
		{
			return store.Delete(key);
		}

		public bool SetCounter(string key, long value)
		{
			return store.SetCounter(key, value);
		}

		public long GetCounter(string key)
		{
			return store.GetCounter(key);
		}

		public long Increment(string key, long value=1)
		{
			return store.Increment(key, value);
		}

		public long Decrement(string key, long value=1)
		{
			return store.Decrement(key, value);
		}

		public long LLen(string key)
		{
			return store.LLen(key);
		}

		public long RPush(string key, byte[][] vals)
		{
			return store.RPush(key, vals);
		}

		public long RPush(string key, object value)
		{
			return store.RPush(key, value);
		}

		public long RPush(string key, params object[] aVals)
		{
			return store.RPush(key, aVals);
		}

		public long RPush(string key, List<object> lVals)
		{
			return store.RPush(key, lVals);
		}

		public byte[] LPopBytes(string key)
		{
			return store.LPopBytes(key);
		}

		public object LPop(string key)
		{
			return store.LPop(key);
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
