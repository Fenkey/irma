using System;
using System.Collections.Generic;
using IRMAKit.Configure;

namespace IRMAKit.Web
{
	public interface IContext
	{
		/// <summary>
		/// OS (i.e. linux / windows)
		/// </summary>
		string OS { get; }

		/// <summary>
		/// IsW0 (Is the first launched worker ?)
		/// </summary>
		bool IsW0 { get; }

		/// <summary>
		/// AppPath
		/// </summary>
		string AppPath { get; }

		/// <summary>
		/// UnixTime
		/// </summary>
		long UnixTime { get; }

		/// <summary>
		/// Config
		/// </summary>
		IConfig Config { get; }

		/// <summary>
		/// Request
		/// </summary>
		IRequest Request { get; }

		/// <summary>
		/// Response
		/// </summary>
		IResponse Response { get; }

		/// <summary>
		/// HandlerName
		/// </summary>
		string HandlerName{ get; }

		/// <summary>
		/// Session
		/// </summary>
		ISession Session { get; }

		/// <summary>
		/// Fresh memory (be valid in period of once handle)
		/// </summary>
		Dictionary<string, object> FM { get; }

		/// <summary>
		/// []
		/// </summary>
		object this[string key] { get; set; }

		/// <summary>
		/// RemoveSessionCookie
		/// </summary>
		void RemoveSessionCookie();

		/// <summary>
		/// Serialize
		/// </summary>
		byte[] Serialize(object obj);

		/// <summary>
		/// Deserialize
		/// </summary>
		object Deserialize(byte[] bytes);
	}
}
