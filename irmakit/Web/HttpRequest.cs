using System;
using System.IO;
using System.Web;
using System.Text;
using System.Collections.Generic;
using System.Collections.Specialized;
using Newtonsoft.Json.Linq;
using IRMACore.Net;
using IRMAKit.Configure;

namespace IRMAKit.Web
{
	internal sealed class PostFile : IPostFile
	{
		private string name;
		public string Name { get { return this.name; } }

		private string fileName;
		public string FileName { get { return this.fileName; } }

		private string contentType;
		public string ContentType { get { return this.contentType; } }

		private byte[] content;
		public byte[] Content { get { return this.content; } }

		public PostFile(string name, string fileName, string contentType, byte[] content)
		{
			this.name = name;
			this.fileName = fileName;
			this.contentType = contentType;
			this.content = content;
		}
	}

	internal sealed class HttpRequest : IRequest
	{
		private bool disposed = false;

		private Config config;

		private Encoding encoding;
		public Encoding Encoding
		{
			get { return this.encoding; }
		}

		public string SessionCookieName
		{
			get { return this.config.SessionCookieName; }
		}

		public string SessionCookiePath
		{
			get { return this.config.SessionCookiePath; }
		}

		public bool IsMock
		{
			get { return Http.RequestIsMock(); }
		}

		private string serverName;
		public string ServerName
		{
			get {
				if (this.serverName == null)
					this.serverName = Http.GetRequestParam("SERVER_NAME");
				return this.serverName;
			}
		}

		private string serverAddr;
		public string ServerAddr
		{
			get {
				if (this.serverAddr == null)
					this.serverAddr = Http.GetRequestParam("SERVER_ADDR");
				return this.serverAddr;
			}
		}

		private string serverProtocol;
		public string ServerProtocol
		{
			get {
				if (this.serverProtocol == null)
					this.serverProtocol = Http.GetRequestParam("SERVER_PROTOCOL");
				return this.serverProtocol;
			}
		}

		private int serverPort = -1;
		public int ServerPort
		{
			get {
				if (this.serverPort < 0)
					int.TryParse(Http.GetRequestParam("SERVER_PORT"), out this.serverPort);
				return this.serverPort;
			}
		}

		private string remoteAddr;
		public string RemoteAddr
		{
			get {
				if (this.remoteAddr == null)
					this.remoteAddr = Http.GetRequestParam("REMOTE_ADDR");
				return this.remoteAddr;
			}
		}

		private int remotePort = -1;
		public int RemotePort
		{
			get {
				if (this.remotePort < 0)
					int.TryParse(Http.GetRequestParam("REMOTE_PORT"), out this.remotePort);
				return this.remotePort;
			}
		}

		private string host;
		public string Host
		{
			get {
				if (this.host == null)
					this.host = Http.GetRequestParam("HTTP_HOST");
				return this.host;
			}
		}

		private string domain;
		public string Domain
		{
			get {
				if (this.domain == null) {
					int i = this.Host.IndexOf(':');
					if (i > 0)
						this.domain = this.Host.Substring(0, i);
					else
						this.domain = this.Host;
				}
				return this.domain;
			}
		}

		private string documentRoot;
		public string DocumentRoot
		{
			get {
				if (this.documentRoot == null)
					this.documentRoot = Http.GetRequestParam("DOCUMENT_ROOT");
				return this.documentRoot;
			}
		}

		private string scriptFileName;
		public string ScriptFileName
		{
			get {
				if (this.scriptFileName == null)
					this.scriptFileName = Http.GetRequestParam("SCRIPT_FILENAME");
				return this.scriptFileName;
			}
		}

		private string forwardedFor;
		public string ForwardedFor
		{
			get {
				if (this.forwardedFor == null)
					this.forwardedFor = Http.GetRequestParam("HTTP_X_FORWARDED_FOR");
				return this.forwardedFor;
			}
		}

		private string clientIp;
		public string ClientIp
		{
			get {
				if (this.clientIp == null)
					this.clientIp = Http.GetRequestParam("HTTP_X_CLIENT_IP");
				return this.clientIp;
			}
		}

		private string connection;
		public string Connection
		{
			get {
				if (this.connection == null)
					this.connection = Http.GetRequestParam("HTTP_CONNECTION");
				return this.connection;
			}
		}

		private string httpsParam;
		private bool https = false;
		public bool Https
		{
			get {
				if (this.httpsParam == null) {
					this.httpsParam = Http.GetRequestParam("HTTPS");
					if (this.httpsParam != null && this.httpsParam.ToLower() == "on")
						this.https = true;
				}
				return this.https;
			}
		}

		private Uri uri;
		public Uri Uri
		{
			get {
				if (this.uri == null) {
					string p = Http.GetRequestUri();
					if (p != null)
						this.uri = new Uri((this.Https ? "https://" : "http://") + this.Host + p);
				}
				return this.uri;
			}
		}

		private int hopCount = 1;
		public int HopCount
		{
			get { return this.hopCount; }
		}

		private bool isRef = false;
		public bool IsRef
		{
			get { return this.isRef; }
		}

		private object refParam = null;
		public object RefParam
		{
			get { return this.refParam; }
			set { this.refParam = value; }
		}

		private string refToParam;
		public string RefToParam
		{
			get { return this.refToParam; }
			set { this.refToParam = value; }
		}

		private string oriAppLocation;
		public string OriAppLocation
		{
			get { return this.oriAppLocation; }
		}

		/*
		 * If the port represents an individual service, you should not set AppName.
		 * Otherwise, you should set it and visit it like: http://<ip>:<port>/<AppName>
		 */
		private string appLocation;
		public string AppLocation
		{
			get {
				if (this.appLocation == null) {
					string p = this.Uri.AbsolutePath;
					if (string.IsNullOrEmpty(this.config.AppName))
						this.appLocation = p;
					else if (p.StartsWith("/" + this.config.AppName + "/"))
						this.appLocation = p.Substring(("/" + this.config.AppName).Length);
					else if (p.Equals("/" + this.config.AppName))
						this.appLocation = "/";
					else {
						/*
						 * Anyway, it's regarded as reasonable. The specical case
						 * such as 'location / {...}' configuration in nginx.
						 */
						this.appLocation = p;
					}
					this.oriAppLocation = this.appLocation;
				}
				return this.appLocation;
			}

			set {
				this.appLocation = value;
				this.isRef = true;
				this.hopCount -= 1;
			}
		}

		private string httpMethod;
		public string HttpMethod
		{
			get {
				if (this.httpMethod == null)
					this.httpMethod = Http.GetRequestMethod();
				return this.httpMethod;
			}
		}

		public Method Method
		{
			get {
				try {
					return (Method)Enum.Parse(typeof(Method), this.HttpMethod);
				} catch {}
				return 0;
			}
		}

		private string queryString;
		public string QueryString
		{
			get {
				if (this.queryString == null)
					this.queryString = Http.GetRequestQueryString();
				return this.queryString;
			}
		}

		private string contentType;
		public string ContentType
		{
			get {
				if (this.contentType == null)
					this.contentType = Http.GetRequestContentType();
				return this.contentType;
			}
		}

		private string accept;
		public string Accept
		{
			get {
				if (this.accept == null)
					this.accept = Http.GetRequestParam("HTTP_ACCEPT");
				return this.accept;
			}
		}

		private string acceptLanguage;
		public string AcceptLanguage
		{
			get {
				if (this.acceptLanguage == null)
					this.acceptLanguage = Http.GetRequestParam("HTTP_ACCEPT_LANGUAGE");
				return this.acceptLanguage;
			}
		}

		private string acceptEncoding;
		public string AcceptEncoding
		{
			get {
				if (this.acceptEncoding == null)
					this.acceptEncoding = Http.GetRequestParam("HTTP_ACCEPT_ENCODING");
				return this.acceptEncoding;
			}
		}

		private string userAgent;
		public string UserAgent
		{
			get {
				if (this.userAgent == null)
					this.userAgent = Http.GetRequestParam("HTTP_USER_AGENT");
				return this.userAgent;
			}
		}

		private string ifModifiedSince;
		public string IfModifiedSince
		{
			get {
				if (this.ifModifiedSince == null)
					this.ifModifiedSince  = Http.GetRequestParam("HTTP_IF_MODIFIED_SINCE");
				return this.ifModifiedSince;
			}
		}

		private string ifMatch;
		public string IfMatch
		{
			get {
				if (this.ifMatch == null)
					this.ifMatch = Http.GetRequestParam("HTTP_IF_MATCH");
				return this.ifMatch;
			}
		}

		private string ifNoneMatch;
		public string IfNoneMatch
		{
			get {
				if (this.ifNoneMatch == null)
					this.ifNoneMatch = Http.GetRequestParam("HTTP_IF_NONE_MATCH");
				return this.ifNoneMatch;
			}
		}

		private string referer;
		public string Referer
		{
			get {
				if (this.referer == null)
					this.referer = Http.GetRequestParam("HTTP_REFERER");
				return this.referer;
			}
		}

		private string origin;
		public string Origin
		{
			get {
				if (this.origin == null)
					this.origin = Http.GetRequestParam("HTTP_ORIGIN");
				return this.origin;
			}
		}

		private string authorization;
		public string Authorization
		{
			get {
				if (this.authorization == null)
					this.authorization = Http.GetRequestParam("HTTP_AUTHORIZATION");
				return this.authorization;
			}
		}

		public string AuthUserName
		{
			get {
				if (Authorization == null)
					return null;
				if (!Authorization.ToUpper().StartsWith("BASIC "))
					return null;
				try {
					byte[] bytes = Convert.FromBase64String(Authorization.Substring(6));
					string[] s = this.encoding.GetString(bytes).Split(new char[]{':'});
					return s.Length == 2 ? s[0] : null;
				} catch {
					return null;
				}
			}
		}

		public string AuthPassword
		{
			get {
				if (Authorization == null)
					return null;
				if (!Authorization.ToUpper().StartsWith("BASIC "))
					return null;
				try {
					byte[] bytes = Convert.FromBase64String(Authorization.Substring(6));
					string[] s = this.encoding.GetString(bytes).Split(new char[]{':'});
					return s.Length == 2 ? s[1] : null;
				} catch {
					return null;
				}
			}
		}

		private string ssldn;
		public string SSLDN
		{
			get {
				if (this.ssldn == null)
					this.ssldn = Http.GetRequestParam("HTTP_SSL_DN");
				return this.ssldn;
			}
		}

		private string proxyParam;
		public string ProxyParam
		{
			get {
				if (this.proxyParam == null)
					this.proxyParam = Http.GetRequestParam("HTTP_X_IRMAKIT_PROXY");
				return this.proxyParam;
			}
		}

		public bool IsProxy
		{
			get { return !string.IsNullOrEmpty(this.ProxyParam); }
		}

		private Dictionary<string, string> headers;
		public Dictionary<string, string> Headers
		{
			get {
				if (this.headers == null)
					this.headers = Http.GetAllRequestHeaders();
				return this.headers;
			}
		}

		private NameValueCollection restParams;
		public NameValueCollection RestParams
		{
			get { return this.restParams; }
			set { this.restParams = value; }
		}

		private NameValueCollection getParams;
		public NameValueCollection GetParams
		{
			get {
				if (this.getParams == null) {
					this.getParams = new NameValueCollection();
					Dictionary<string, byte[]> ps = Http.GetAllRequestGetParams();
					try {
						foreach (KeyValuePair<string, byte[]> p in ps) {
							string v = this.encoding.GetString(p.Value);
							this.getParams.Add(p.Key, v.Trim());
						}
					} finally {
						ps = null;
					}
				}
				return getParams;
			}
		}

		private NameValueCollection postParams;
		public NameValueCollection PostParams
		{
			get {
				if (this.postParams == null) {
					this.postParams = new NameValueCollection();
					Dictionary<string, byte[]> ps = Http.GetAllRequestPostParams();
					try {
						foreach (KeyValuePair<string, byte[]> p in ps) {
							string v = this.encoding.GetString(p.Value);
							this.postParams.Add(p.Key, v.Trim());
						}
					} finally {
						ps = null;
					}
				}
				return postParams;
			}
		}

		private NameValueCollection _params;
		public NameValueCollection Params
		{
			get {
				if (this._params == null) {
					this._params = new NameValueCollection(this.GetParams);
					foreach (string k in this.PostParams.AllKeys)
						this._params.Set(k, this.PostParams.Get(k));
				}
				return _params;
			}
		}

		private HttpCookie ParseCookieItem(string itemStr)
		{
			HttpCookie cookie = null;
			string[] kvs = itemStr.Split(new char[]{'&'});
			if (kvs.Length == 1) {
				string[] kv = itemStr.Split(new char[]{'='}, 2);
				if (kv.Length == 2)
			   		cookie = new HttpCookie(kv[0].Trim(), kv[1].Trim());
			} else if (kvs.Length > 1) {
				for (int i = 0; i < kvs.Length; i++) {
					string[] kv = kvs[i].Split(new char[]{'='}, 2);
					if (kv.Length != 2)
						continue;
					if (i == 0)
						cookie = new HttpCookie(kv[0].Trim(), kv[1].Trim());
					else
						cookie.Values[kv[0].Trim()] = kv[1].Trim();
				}
			}
			kvs = null;
			return cookie;
		}

		private HttpCookieCollection ParseCookie(string cookieStr)
		{
			HttpCookieCollection cookies = new HttpCookieCollection();
			if (!string.IsNullOrEmpty(cookieStr)) {
				string[] items = cookieStr.Split(new char[]{';'});
				foreach (string item in items) {
					HttpCookie cookie = ParseCookieItem(item);
					if (cookie != null)
						cookies.Add(cookie);
				}
			}
			return cookies;
		}

		private HttpCookieCollection cookies;
		public HttpCookieCollection Cookies
		{
			get {
				if (this.cookies == null) {
					//this.cookies = ParseCookie("fooSessionId=sid-12345678; expires=Sat, 02 May 2009 23:38:25 GMT");
					this.cookies = ParseCookie(Http.GetRequestParam("HTTP_COOKIE"));
				}
				return this.cookies;
			}
		}

		private Dictionary<string, IPostFile> files;
		public Dictionary<string, IPostFile> Files
		{
			get {
				if (this.files == null) {
					this.files = new Dictionary<string, IPostFile>();
					Dictionary<string, byte[]> fileParams = Http.GetAllRequestFileParams();
					foreach (KeyValuePair<string, byte[]> f in fileParams) {
						string[] s = f.Key.Split(new string[]{"\r\n"}, StringSplitOptions.None|StringSplitOptions.RemoveEmptyEntries);
						if (s.Length == 3)
							this.files[s[0]] = new PostFile(s[0], s[1], s[2], f.Value);
					}
				}
				return this.files;
			}
		}

		private byte[] body;
		public byte[] Body
		{
			get {
				if (this.body == null)
					this.body = Http.GetRequestBody();
				return this.body;
			}
		}

		private Stream inputStream;
		public Stream InputStream
		{
			get {
				if (this.inputStream == null)
					this.inputStream = new MemoryStream(this.Body);
				return this.inputStream;
			}
		}

		private string inputText;
		public string InputText
		{
			get {
				if (this.inputText == null) {
					try {
						this.inputText = this.encoding.GetString(this.Body);
					} catch {
						this.inputText = null;
					}
				}
				return this.inputText;
			}
		}

		private JObject inputJson;
		public JObject InputJson
		{
			get {
				if (this.inputJson == null) {
					try {
						this.inputJson = JObject.Parse(this.InputText);
					} catch {
						this.inputJson = null;
					}
				}
				return this.inputJson;
			}
		}

		public long ContentLength
		{
			get {
				return Body == null ? 0 : Body.Length;
			}
		}

		public long TotalBytes
		{
			get {
				return Body == null ? 0 : Body.Length;
			}
		}

		private byte[] reqDump;
		public byte[] ReqDump
		{
			get {
				if (this.reqDump == null)
					this.reqDump = Http.RequestDump();
				return this.reqDump;
			}
		}

		public HttpRequest(Config config)
		{
			this.config = config;
			this.encoding = Encoding.GetEncoding(config.AppCharset);
		}

		public void Dispose()
		{
			if (disposed)
				return;

			headers = null;
			restParams = null;
			getParams = null;
			postParams = null;
			_params = null;
			cookies = null;
			files = null;
			body = null;
			inputJson = null;
			refParam = null;
			reqDump = null;
			if (inputStream != null) {
				inputStream.Close();
				inputStream = null;
			}

			disposed = true;
			GC.SuppressFinalize(this);
		}

		~HttpRequest()
		{
			Dispose();
		}
	}
}
