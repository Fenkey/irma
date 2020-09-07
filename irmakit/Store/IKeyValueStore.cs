using System;
using System.Collections.Generic;

namespace IRMAKit.Store
{
	public class KVStoreInvalidException : Exception
	{
		public KVStoreInvalidException() : base("KeyValue Store is invalid") {}

		public KVStoreInvalidException(string msg) : base(msg) {}
	}

	public interface IKeyValueStore
	{
		/// <summary>
		/// Add
		/// </summary>
		/// <param name="key">Key</param>
		/// <param name="value">Value</param>
		/// <param name="expire">Expiration</param>
		/// <returns>Return false if key exists.</returns>
		bool Add(string key, byte[] value, TimeSpan expire);

		/// <summary>
		/// Add
		/// </summary>
		/// <param name="key">Key</param>
		/// <param name="value">Value</param>
		/// <param name="expire">Expiration</param>
		/// <returns>Return false if key exists.</returns>
		bool Add(string key, object value, TimeSpan expire);

		/// <summary>
		/// Add
		/// </summary>
		/// <param name="key">Key</param>
		/// <param name="value">Value</param>
		/// <param name="expire">Expiration</param>
		/// <returns>Return false if key exists.</returns>
		bool Add(string key, byte[] value, long expire=0L);

		/// <summary>
		/// Add
		/// </summary>
		/// <param name="key">Key</param>
		/// <param name="value">Value</param>
		/// <param name="expire">Expiration</param>
		/// <returns>Return false if key exists.</returns>
		bool Add(string key, object value, long expire=0L);

		/// <summary>
		/// Append
		/// </summary>
		/// <param name="key">Key</param>
		/// <param name="value">Value</param>
		/// <returns>Return false if key doesn't exist.</returns>
		bool Append(string key, byte[] value);

		/// <summary>
		/// Append
		/// </summary>
		/// <param name="key">Key</param>
		/// <param name="value">Value</param>
		/// <returns>Return false if key doesn't exist.</returns>
		bool Append(string key, object value);

		/// <summary>
		/// Prepend
		/// </summary>
		/// <param name="key">Key</param>
		/// <param name="value">Value</param>
		/// <returns>Return false if key doesn't exist.</returns>
		bool Prepend(string key, byte[] value);

		/// <summary>
		/// Prepend
		/// </summary>
		/// <param name="key">Key</param>
		/// <param name="value">Value</param>
		/// <returns>Return false if key doesn't exist.</returns>
		bool Prepend(string key, object value);

		/// <summary>
		/// Set
		/// </summary>
		/// <param name="key">Key</param>
		/// <param name="value">Value</param>
		/// <param name="expire">Expiration</param>
		/// <returns>Return true if successful.</returns>
		bool Set(string key, byte[] value, TimeSpan expire);

		/// <summary>
		/// Set
		/// </summary>
		/// <param name="key">Key</param>
		/// <param name="value">Value</param>
		/// <param name="expire">Expiration</param>
		/// <returns>Return true if successful.</returns>
		bool Set(string key, object value, TimeSpan expire);

		/// <summary>
		/// Set
		/// </summary>
		/// <param name="key">Key</param>
		/// <param name="value">Value</param>
		/// <param name="expire">Expiration</param>
		/// <returns>Return true if successful.</returns>
		bool Set(string key, byte[] value, long expire=0L);

		/// <summary>
		/// Set
		/// </summary>
		/// <param name="key">Key</param>
		/// <param name="value">Value</param>
		/// <param name="expire">Expiration</param>
		/// <returns>Return true if successful.</returns>
		bool Set(string key, object value, long expire=0L);

		/// <summary>
		/// GetBytes
		/// </summary>
		/// <param name="key">Key</param>
		/// <returns>Get bytes cached.</returns>
		byte[] GetBytes(string key);

		/// <summary>
		/// Get
		/// </summary>
		/// <param name="key">Key</param>
		/// <returns>Get object cached.</returns>
		object Get(string key);

		/// <summary>
		/// MGetBytes
		/// </summary>
		/// <param name="keys">Key set</param>
		/// <returns>Get bytes array cached.</returns>
		byte[][] MGetBytes(string[] keys);

		/// <summary>
		/// Get
		/// </summary>
		/// <param name="keys">Key set</param>
		/// <returns>Get objects cached.</returns>
		Dictionary<string, object> Get(string[] keys);

		/// <summary>
		/// Replace
		/// </summary>
		/// <param name="key">Key</param>
		/// <param name="value">Value</param>
		/// <param name="expire">Expiration</param>
		/// <returns>Replace value.</returns>
		bool Replace(string key, byte[] value, TimeSpan expire);

		/// <summary>
		/// Replace
		/// </summary>
		/// <param name="key">Key</param>
		/// <param name="value">Value</param>
		/// <param name="expire">Expiration</param>
		/// <returns>Replace value.</returns>
		bool Replace(string key, object value, TimeSpan expire);

		/// <summary>
		/// Replace
		/// </summary>
		/// <param name="key">Key</param>
		/// <param name="value">Value</param>
		/// <param name="expire">Expiration</param>
		/// <returns>Replace value.</returns>
		bool Replace(string key, byte[] value, long expire=0L);

		/// <summary>
		/// Replace
		/// </summary>
		/// <param name="key">Key</param>
		/// <param name="value">Value</param>
		/// <param name="expire">Expiration</param>
		/// <returns>Replace value.</returns>
		bool Replace(string key, object value, long expire=0L);

		/// <summary>
		/// Expire
		/// </summary>
		/// <param name="key">Key</param>
		/// <param name="expire">Expiration</param>
		/// <returns>Set/update key's expiration time.</returns>
		bool Expire(string key, TimeSpan expire);

		/// <summary>
		/// Expire
		/// </summary>
		/// <param name="key">Key</param>
		/// <param name="expire">Expiration</param>
		/// <returns>Set/update key's expiration time.</returns>
		bool Expire(string key, long expire=0L);

		/// <summary>
		/// Exists
		/// </summary>
		/// <param name="key">Key</param>
		/// <returns>Return ture if key exists.</returns>
		bool Exists(string key);

		/// <summary>
		/// Delete
		/// </summary>
		/// <param name="key">Key</param>
		/// <returns>Return false if key doesn't exist.</returns>
		bool Delete(string key);

		/// <summary>
		/// SetCounter
		/// </summary>
		/// <param name="key">Key</param>
		/// <param name="value">Value</param>
		/// <returns>Set key's counter.</returns>
		bool SetCounter(string key, long value);

		/// <summary>
		/// GetCounter
		/// </summary>
		/// <param name="key">Key</param>
		/// <param name="value">Value</param>
		/// <returns>Get key's counter, return -1 if failed and 0 if key doesn't exist.</returns>
		long GetCounter(string key);

		/// <summary>
		/// Increment
		/// </summary>
		/// <param name="key">Key</param>
		/// <param name="value">Value</param>
		/// <returns>Key's counter increases value, return -1 if failed and new counter if successful.</returns>
		long Increment(string key, long value=1);

		/// <summary>
		/// Decrement
		/// </summary>
		/// <param name="key">Key</param>
		/// <param name="value">Value</param>
		/// <returns>Key's counter decreases value, return -1 if failed and new counter if successful.</returns>
		long Decrement(string key, long value=1);

		/// <summary>
		/// LLen
		/// </summary>
		/// <param name="key">Key</param>
		/// <returns>Return -1 if failed and length if successful.</returns>
		long LLen(string key);

		/// <summary>
		/// RPust
		/// </summary>
		/// <param name="key">Key</param>
		/// <returns>Return -1 if failed and new length if successful.</returns>
		long RPush(string key, byte[][] vals);

		/// <summary>
		/// RPust
		/// </summary>
		/// <param name="key">Key</param>
		/// <param name="value">Value</param>
		/// <returns>Return -1 if failed and new length if successful.</returns>
		long RPush(string key, object value);

		/// <summary>
		/// RPust
		/// </summary>
		/// <param name="key">Key</param>
		/// <param name="aVals">Values array</param>
		/// <returns>Return -1 if failed and new length if successful.</returns>
		long RPush(string key, params object[] aVals);

		/// <summary>
		/// RPust
		/// </summary>
		/// <param name="key">Key</param>
		/// <param name="lVals">Values list</param>
		/// <returns>Return -1 if failed and new length if successful.</returns>
		long RPush(string key, List<object> lVals);

		/// <summary>
		/// LPopBytes
		/// </summary>
		/// <param name="key">Key</param>
		/// <returns>Get bytes cached.</returns>
		byte[] LPopBytes(string key);

		/// <summary>
		/// LPop
		/// </summary>
		/// <param name="key">Key</param>
		/// <returns>Get object cached.</returns>
		object LPop(string key);

		/// <summary>
		/// []
		/// </summary>
		/// <param name="key">Key</param>
		/// <returns>Get string cached.</returns>
		string this[string key] { get; set; }
	}
}
