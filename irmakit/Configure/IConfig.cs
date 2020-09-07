using System;
using Newtonsoft.Json.Linq;
using IRMAKit.Store;

namespace IRMAKit.Configure
{
	internal class ConfigFileInvalidException : Exception
	{
		public ConfigFileInvalidException(string file) : base(string.Format("Invalid config file: {0}", file)) {}
	}

	internal class ConfigJsonParseFailedException : Exception
	{
		public ConfigJsonParseFailedException(string file, string err) : base(string.Format("Config file('{0}') parse json wrong: {1}", file, err)) {}
	}

	internal class ConfigParamNotFoundException : Exception
	{
		public ConfigParamNotFoundException(string param) : base(string.Format("Config param is not found: {0}", param)) {}
	}

	internal class ConfigParamInvalidException : Exception
	{
		public ConfigParamInvalidException(string param) : base(string.Format("Config param is invalid: {0}", param)) {}
	}

	public interface IConfig
	{
		/// <summary>
		/// Application name
		/// </summary>
		string AppName { get; }

		/// <summary>
		/// Version
		/// </summary>
		string Version { get; }

		/// <summary>
		/// Release information (e.g. date)
		/// </summary>
		string ReleaseInfo { get; }

		/// <summary>
		/// Application charset
		/// </summary>
		string AppCharset { get; }

		/// <summary>
		/// Cookie name for session
		/// </summary>
		string SessionCookieName { get; }

		/// <summary>
		/// Cookie path for session
		/// </summary>
		string SessionCookiePath { get; }

		/// <summary>
		/// Cookie domain for session
		/// </summary>
		string SessionCookieDomain { get; }

		/// <summary>
		/// User's configuration
		/// </summary>
		JObject User { get; }
	}
}
