using System;
using System.IO;
using System.Web;
using System.Text;
using System.Collections.Generic;
using System.Collections.Specialized;
using Newtonsoft.Json.Linq;

namespace IRMAKit.Web
{
	public enum Method
	{
		HEAD	= 1,
		GET		= 2,
		POST	= 4,
		PUT		= 8,
		SET		= 16,
		DELETE	= 32,
		OPTIONS	= 64,
		ALL		= 1|2|4|8|16|32|64
	}

	public interface IPostFile
	{
		/// <summary>
		/// Name of parameter
		/// </summary>
		string Name { get; }

		/// <summary>
		/// FileName
		/// </summary>
		string FileName { get; }

		/// <summary>
		/// ContentType
		/// </summary>
		string ContentType { get; }

		/// <summary>
		/// Content
		/// </summary>
		byte[] Content { get; }
	}

	public interface IRequest : IDisposable
	{
		/// <summary>
		/// Encoding
		/// </summary>
		Encoding Encoding { get; }

		/// <summary>
		/// IsMock
		/// </summary>
		bool IsMock { get; }

		/// <summary>
		/// ServerName
		/// </summary>
		string ServerName { get; }

		/// <summary>
		/// ServerAddr
		/// </summary>
		string ServerAddr { get; }

		/// <summary>
		/// ServerProtocol
		/// </summary>
		string ServerProtocol { get; }

		/// <summary>
		/// ServerPort
		/// </summary>
		int ServerPort { get; }

		/// <summary>
		/// RemoteAddr
		/// </summary>
		string RemoteAddr { get; }

		/// <summary>
		/// RemotePort
		/// </summary>
		int RemotePort { get; }

		/// <summary>
		/// Host
		/// </summary>
		string Host { get; }

		/// <summary>
		/// Domain
		/// </summary>
		string Domain { get; }

		/// <summary>
		/// DocumentRoot
		/// </summary>
		string DocumentRoot { get; }

		/// <summary>
		/// ScriptFileName
		/// </summary>
		string ScriptFileName { get; }

		/// <summary>
		/// X-Forwarded-For
		/// </summary>
		string ForwardedFor { get; }

		/// <summary>
		/// ClientIp
		/// </summary>
		string ClientIp { get; }

		/// <summary>
		/// Connection
		/// </summary>
		string Connection { get; }

		/// <summary>
		/// Is https ?
		/// </summary>
		bool Https { get; }

		/// <summary>
		/// URI
		/// </summary>
		Uri Uri { get; }

		/// <summary>
		/// OriAppLocation
		/// </summary>
		string OriAppLocation { get; }

		/// <summary>
		/// AppLocation
		/// </summary>
		string AppLocation { get; }

		/// <summary>
		/// IsRef (if true, it will cause AppLocation to be different from OriAppLocation.)
		/// </summary>
		bool IsRef { get; }

		/// <summary>
		/// RefParam (common)
		/// </summary>
		object RefParam { get; set; }

		/// <summary>
		/// RefToParam (individual)
		/// </summary>
		string RefToParam { get; set; }

		/// <summary>
		/// HttpMethod
		/// </summary>
		string HttpMethod { get; }

		/// <summary>
		/// Method
		/// </summary>
		Method Method { get; }

		/// <summary>
		/// QueryString
		/// </summary>
		string QueryString { get; }

		/// <summary>
		/// ContentType
		/// </summary>
		string ContentType { get; }

		/// <summary>
		/// Accept
		/// </summary>
		string Accept { get; }

		/// <summary>
		/// AcceptLanguage
		/// </summary>
		string AcceptLanguage { get; }

		/// <summary>
		/// AcceptEncoding
		/// </summary>
		string AcceptEncoding { get; }

		/// <summary>
		/// UserAgent
		/// </summary>
		string UserAgent { get; }

		/// <summary>
		/// IfModifiedSince
		/// </summary>
		string IfModifiedSince { get; }

		/// <summary>
		/// IfMatch
		/// </summary>
		string IfMatch { get; }

		/// <summary>
		/// IfNoneMatch
		/// </summary>
		string IfNoneMatch { get; }

		/// <summary>
		/// Referer
		/// </summary>
		string Referer { get; }

		/// <summary>
		/// Origin
		/// </summary>
		string Origin { get; }

		/// <summary>
		/// Authorization
		/// </summary>
		string Authorization { get; }

		/// <summary>
		/// AuthUserName
		/// </summary>
		string AuthUserName { get; }

		/// <summary>
		/// AuthPassword
		/// </summary>
		string AuthPassword { get; }

		/// <summary>
		/// SSLDN, th DN string of client certificate in https. We get it by specifying
		/// 'fastcgi_param HTTP_SSL_DN $ssl_client_s_dn;' in the configuration of nginx.
		/// </summary>
		string SSLDN { get; }

		/// <summary>
		/// IsProxy ? if so, it means the current request is from another handler or
		/// service developed on irmakit by proxying.
		/// </summary>
		bool IsProxy { get; }

		/// <summary>
		/// ProxyParam
		/// </summary>
		string ProxyParam { get; }

		/// <summary>
		/// Headers
		/// </summary>
		Dictionary<string, string> Headers { get; }

		/// <summary>
		/// RestParams
		/// </summary>
		NameValueCollection RestParams { get; set; }

		/// <summary>
		/// GetParams
		/// </summary>
		NameValueCollection GetParams { get; }

		/// <summary>
		/// PostParams
		/// </summary>
		NameValueCollection PostParams { get; }

		/// <summary>
		/// Params, which are the comprehensive parameters include GetParams and PostParams.
		/// </summary>
		NameValueCollection Params { get; }

		/// <summary>
		/// Cookies
		/// </summary>
		HttpCookieCollection Cookies { get; }

		/// <summary>
		/// Files posted
		/// </summary>
		Dictionary<string, IPostFile> Files { get; }

		/// <summary>
		/// Body
		/// </summary>
		byte[] Body { get; }

		/// <summary>
		/// Body
		/// </summary>
		Stream InputStream { get; }

		/// <summary>
		/// Body in the form of text
		/// </summary>
		string InputText { get; }

		/// <summary>
		/// Body in the form of json
		/// </summary>
		JObject InputJson { get; }

		/// <summary>
		/// ContentLength
		/// </summary>
		long ContentLength { get; }

		/// <summary>
		/// TotalBytes
		/// </summary>
		long TotalBytes { get; }

		/// <summary>
		/// ReqDump (The dump of the current request)
		/// </summary>
		byte[] ReqDump { get; }
	}
}
